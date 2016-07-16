//LICENSE: MIT
//Copyright(c) 2012 Felix Geisendörfer(felix @debuggable.com) and contributors 
//Copyright(c) 2013 Andrey Sidorov(sidorares @yandex.ru) and contributors
//MIT, 2015-2016, brezza92, EngineKit and contributors

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

using System;
using System.Collections.Generic;
namespace SharpConnect.MySql.Internal
{
    class QueryParsingConfig
    {
        public bool UseLocalTimeZone;
        public bool DateStrings;
        public string TimeZone;
        public bool SupportBigNumbers;
        public bool BigNumberStrings;
        public bool typeCast;
    }

    delegate void Action<T>(T a);

    enum QueryExecState
    {
        Rest,
        Prepare,
        Exec,
        Closing,
        Closed,
    }

    class Query
    {
        public bool typeCast;
        public bool nestTables;
        CommandParams _cmdParams;
        readonly Connection _conn;
        bool _prepareStatementMode;

        MySqlStreamWrtier _writer;
        SqlStringTemplate _sqlStrTemplate;
        PreparedContext _prepareContext;
        MySqlParserMx _sqlParserMx;
        QueryExecState _execState = QueryExecState.Rest;
        Action<MySqlTableResult> _tableResultListener;

        public Query(Connection conn, string sql, CommandParams cmdParams)
            : this(conn, new SqlStringTemplate(sql), cmdParams)
        {
        }
        public Query(Connection conn, SqlStringTemplate sql, CommandParams cmdParams)
        {
            //*** query use conn resource such as parser,writer
            //so 1 query 1 connection      
            if (conn.IsInUsed)
            {
                //can't use this conn
                throw new Exception("connection is in used");
            }
            //--------------------------------------------------------------
            if (sql == null)
            {
                throw new Exception("Sql command can not null.");
            }
            //--------------------------------------------------------------
            this._conn = conn;
            this._cmdParams = cmdParams;
            //--------------------------------------------------------------
            typeCast = conn.config.typeCast;
            nestTables = false;
            _sqlParserMx = conn.MySqlParserMx;
            _writer = conn.PacketWriter;
            //_receiveBuffer = null;
            _sqlStrTemplate = sql;
        }
        public ErrPacket LoadError { get; private set; }
        public OkPacket OkPacket { get; private set; }
        public void SetResultListener(Action<MySqlTableResult> tableResultListener)
        {
            this._tableResultListener = tableResultListener;
        }


        /// <summary>
        /// +/- blocking
        /// </summary>
        /// <param name="nextAction"></param>
        public void Prepare(Action nextAction = null)
        {
            _execState = QueryExecState.Prepare;
            if (_cmdParams == null)
            {
                throw new Exception("Sql cmdParams can not null.");
            }
            //-------------------
            //blocking method***
            //wait until execute finish 
            //-------------------
            //prepare sql query             
            _sqlParserMx.UsePrepareResponseParser();
            _prepareContext = null;
            //-------------------------------------------------------------
            _writer.Reset();
            ComPrepareStatementPacket.Write(
                _writer,
                _sqlStrTemplate.BindValues(_cmdParams, true));
            //-------------------------------------------------------------
            if (nextAction != null)
            {
                //not block here
                SendAndRecv_A(_writer.ToArray(), nextAction);
            }
            else
            {
                //blocking
                _conn.InitWait();
                SendAndRecv_A(_writer.ToArray(), _conn.UnWait);
                _conn.Wait();
            }
        }

        /// <summary>
        ///+/- blocking 
        /// </summary>
        /// <param name="nextAction"></param>
        public void Execute(Action nextAction = null)
        {
            _execState = QueryExecState.Exec;
            //-------------------
            //blocking method***
            //wait until execute finish 
            //-------------------  
            if (nextAction != null)
            {
                //not block here
                if (_prepareContext != null)
                {
                    ExecutePrepareQuery_A(nextAction);
                }
                else
                {
                    ExecuteNonPrepare_A(nextAction);
                }
            }
            else
            {
                //block
                _conn.InitWait();
                if (_prepareContext != null)
                {
                    ExecutePrepareQuery_A(_conn.UnWait);
                }
                else
                {
                    ExecuteNonPrepare_A(_conn.UnWait);
                }
                _conn.Wait();
            }
        }


        /// <summary>
        ///  +/- blocking
        /// </summary>
        /// <param name="nextAction"></param>
        public void Close(Action nextAction = null)
        {
            //-------------------------------------------------
            switch (_execState)
            {
                case QueryExecState.Closed:
                    if (nextAction != null)
                    {
                        nextAction();
                    }
                    return; //***
                case QueryExecState.Closing:
                    throw new Exception("conn is closing ...");
            }
            //first ***
            _execState = QueryExecState.Closing;
            //-------------------------------------------------
            //blocking
            _sqlParserMx.UseFlushMode(true);
            //wait where   
            //TODO: review here *** tight loop
            while (!_recvComplete) ;
            _sqlParserMx.UseFlushMode(false); //switch back//
            //------------------------------------------------- 
            if (nextAction != null)
            {
                //non blocking 
                Close_A(nextAction);
            }
            else
            {
                //blocking
                _conn.InitWait();
                Close_A(_conn.UnWait);
                _conn.Wait();
            }
        }
        void ExecuteNonPrepare_A(Action nextAction)
        {

            _sqlParserMx.UseResultParser();
            _writer.Reset();
            ComQueryPacket.Write(
                _writer,
                _sqlStrTemplate.BindValues(_cmdParams, false));
            SendAndRecv_A(_writer.ToArray(), nextAction);
        }
        void ExecutePrepareQuery_A(Action nextAction)
        {
            //make sure that when Finished is called
            //when this complete
            if (_prepareContext == null)
            {
                ExecuteNonPrepare_A(nextAction);
                return;
            }
            if (_prepareContext.statementId == 0)
            {
                throw new Exception("exec Prepare() first");
            }
            //------------------------------------------------ 
            ResetPrepareStmt_A(() =>
            {
                //The server will send a OK_Packet if the statement could be reset, a ERR_Packet if not.
                //--------------------------------------------  
                _prepareStatementMode = true;
                _sqlParserMx.UseResultParser(true);
                //-------------------------------------------- 
                _writer.Reset();
                //fill prepared values 
                ComExecPrepareStmtPacket.Write(_writer,
                _prepareContext.statementId,
                _prepareContext.PrepareBoundData(_cmdParams));
                //-------------------------------------------- 
                SendAndRecv_A(_writer.ToArray(), nextAction);
            });
        }
        void Close_A(Action nextAction)
        {
            if (_prepareContext != null)
            {
                _sqlParserMx.UseResultParser();
                _writer.Reset();
                ComStmtClosePacket.Write(_writer, _prepareContext.statementId);
                //when close, not wait for recv
                SendPacket_A(_writer.ToArray(), nextAction);
            }
            else
            {
                _execState = QueryExecState.Closed;
                nextAction();
            }
        }

        void ResetPrepareStmt_A(Action nextAction)
        {
            if (_prepareStatementMode && _prepareContext != null)
            {
                _sqlParserMx.UseResultParser();
                _writer.Reset();
                ComStmtResetPacket.Write(_writer, _prepareContext.statementId);
                SendAndRecv_A(_writer.ToArray(), nextAction);
            }
            else
            {
                nextAction();
            }
        }

        bool _recvComplete = true;
        void RecvComplete()
        {
            _recvComplete = true;
        }
        void RecvPacket_A(Action whenRecv)
        {

            _recvComplete = false;
            _conn.StartReceive(result =>
            {
                if (result == null)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    switch (result.Kind)
                    {
                        default: throw new NotSupportedException();//unknown
                        case MySqlResultKind.Ok:
                            {
                                MySqlOkResult ok = result as MySqlOkResult;
                                OkPacket = ok.okpacket;
                                RecvComplete();
                            }
                            break;
                        case MySqlResultKind.Error:
                            {
                                MySqlErrorResult error = result as MySqlErrorResult;
                                LoadError = error.errPacket;
                                RecvComplete();
                            }
                            break;
                        case MySqlResultKind.TableResult:
                            {
                                MySqlTableResult tableResult = result as MySqlTableResult;
                                //support partial table mode
                                if (!tableResult.IsPartialTable)
                                {
                                    RecvComplete();
                                }
                                //last sub table is not partial table 
                                if (_tableResultListener != null)
                                {
                                    //the _tableResultListener may modifid by other state (Close)
                                    //if don't lock we need to store it to local var
                                    _tableResultListener(tableResult);
                                }
                            }
                            break;
                        case MySqlResultKind.PrepareResponse:
                            {
                                //The server will send a OK_Packet if the statement could be reset, a ERR_Packet if not.
                                //on prepare
                                MySqlPrepareResponseResult response = result as MySqlPrepareResponseResult;
                                _prepareContext = new PreparedContext(
                                    response.okPacket.statement_id,
                                    _sqlStrTemplate,
                                    response.tableHeader);
                                RecvComplete();
                            }
                            break;
                    }
                }
                //-----------------
                whenRecv();
                //-----------------
            });
        }
        void SendPacket_A(byte[] packetBuffer, Action nextAction)
        {
            //send all to 
            _conn.StartSend(packetBuffer, 0, packetBuffer.Length, nextAction);
        }
        void SendAndRecv_A(byte[] packetBuffer, Action onRecv)
        {
            _conn.StartSend(packetBuffer, 0, packetBuffer.Length, () =>
            {
                RecvPacket_A(onRecv);
            });
        }
    }

    class PreparedContext
    {
        public readonly uint statementId;
        TableHeader _tableHeader;
        SqlStringTemplate _sqlStringTemplate;
        MyStructData[] _preparedValues;
        List<SqlBoundSection> _keys;
        public PreparedContext(uint statementId, SqlStringTemplate sqlStringTemplate, TableHeader tableHeader)
        {
            this.statementId = statementId;
            _sqlStringTemplate = sqlStringTemplate;
            _keys = _sqlStringTemplate.GetValueKeys();
            //----------------------------------------------
            _tableHeader = tableHeader;
            int serverFieldCount = tableHeader.ColumnCount;
            //add field information to _keys
            List<FieldPacket> fields = tableHeader.GetFields();
            //----------------------------------------------
            int bindingCount = 0;
            for (int i = 0; i < serverFieldCount; ++i)
            {
                FieldPacket f = fields[i];
                if (f.name == "?") //
                {
                    //this is binding field
                    _keys[bindingCount].fieldInfo = f;
                    bindingCount++;
                }
            }
            //some field from server is not binding field
            //so we select only binding field
            if (bindingCount != _keys.Count)
            {
                throw new Exception("key num not matched!");
            }
            //-------------------------------------------------
            _preparedValues = new MyStructData[bindingCount]; //***
        }

        public MyStructData[] PrepareBoundData(CommandParams cmdParams)
        {
            //1. check proper type and 
            //2. check all values are in its range  
            //extract and arrange 

            int j = _keys.Count;
            for (int i = 0; i < j; ++i)
            {
                SqlBoundSection key = _keys[i];
                if (!cmdParams.TryGetData(key.Text, out _preparedValues[i]))
                {
                    //not found key 
                    throw new Exception("not found " + _keys[i].Text);
                }
                else
                {
                    //-------------------------------
                    //TODO: check here 
                    //all field type is 253 ?
                    //error
                    //------------------------------- 
                    //check
                    //FieldPacket fieldInfo = key.fieldInfo;
                    //switch ((Types)fieldInfo.type)
                    //{
                    //    case Types.VARCHAR:
                    //    case Types.VAR_STRING:
                    //        {
                    //            //check length
                    //            if (_preparedValues[i].myString.Length > fieldInfo.length)
                    //            {
                    //                //TODO: notify user how to handle this data
                    //                //before error
                    //            }
                    //        }
                    //        break;
                    //}



                }
            }

            return _preparedValues;
        }
    }




    class TableHeader
    {
        QueryParsingConfig _config;
        List<FieldPacket> _fields;
        Dictionary<string, int> _fieldNamePosMap;
        public TableHeader()
        {
            this._fields = new List<FieldPacket>();
        }

        public void AddField(FieldPacket field)
        {
            _fields.Add(field);
        }
        public List<FieldPacket> GetFields()
        {
            return _fields;
        }
        public int ColumnCount
        {
            get { return this._fields.Count; }
        }


        public int GetFieldIndex(string fieldName)
        {
            if (_fieldNamePosMap == null)
            {
                ///build map index
                int j = _fields.Count;
                _fieldNamePosMap = new Dictionary<string, int>(j);
                for (int i = 0; i < j; ++i)
                {
                    _fieldNamePosMap.Add(_fields[i].name, i);
                }
            }
            int found;
            if (!_fieldNamePosMap.TryGetValue(fieldName, out found))
            {
                return -1;
            }
            return found;
        }

        public bool TypeCast { get; private set; }
        public bool NestTables { get; set; }
        public QueryParsingConfig ParsingConfig
        {
            get { return _config; }
            set
            {
                _config = value;
                this.TypeCast = _config.typeCast;
            }
        }

    }
}