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
        Action<MySqlTableResult> tableResultArrived;


        public Query(Connection conn, string sql, CommandParams cmdParams)
        {
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
            if (cmdParams == null)
            {
                throw new Exception("Sql cmdParams can not null.");
            }
            //--------------------------------------------------------------
            this._conn = conn;
            this._cmdParams = cmdParams;
            typeCast = conn.config.typeCast;
            nestTables = false;
            //index = 0;
            LoadError = null;
            //*** query use conn resource such as parser,writer
            //so 1 query 1 connection             
            _sqlParserMx = conn.MySqlParserMx;
            _writer = conn.PacketWriter;
            //_receiveBuffer = null;
            _sqlStrTemplate = new SqlStringTemplate(sql);
        }


        public ErrPacket LoadError { get; private set; }
        public OkPacket OkPacket { get; private set; }

        public void SetResultListener(Action<MySqlTableResult> tableResultArrived)
        {
            this.tableResultArrived = tableResultArrived;

        }
        //*** blocking
        public void Prepare()
        {
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
            bool finished = false;
            SendPacket_A(_writer.ToArray(), () =>
            {
                _conn.StartReceive(result =>
                {
                    if (result is MySqlPrepareResponse)
                    {
                        MySqlPrepareResponse response = result as MySqlPrepareResponse;
                        _prepareContext = new PreparedContext(
                            response.okPacket.statement_id,
                            _sqlStrTemplate,
                            response.tableHeader);
                    }
                    else//error
                    {
                        throw new Exception("Prepare Error");
                    }
                    finished = true;
                });
            });
            //-------------------------------------------------------------
            while (!finished) ;//wait *** tight loop
            //-------------------------------------------------------------
        }
        //*** blocking
        public void Execute(Action nextAction = null)
        {
            //-------------------
            //blocking method***
            //wait until execute finish 
            //------------------- 
            //_rowReadIndex = 0;
            //------------------- 
            if (nextAction != null)
            {
                //not block
                if (_prepareContext != null)
                {
                    if (_cmdParams == null)
                    {
                        nextAction();//**
                        return;
                    }
                    ExecutePrepareQuery_A(nextAction);
                }
                else
                {
                    //TODO: review here 
                    _prepareContext = null; //***
                    ExecuteNonPrepare_A(nextAction);
                }
            }
            else
            {
                //block
                bool finished = false;
                if (_prepareContext != null)
                {
                    if (_cmdParams == null)
                    {
                        return;
                    }

                    ExecutePrepareQuery_A(() =>
                    {
                        finished = true;
                    });
                }
                else
                {
                    //TODO: review here 
                    _prepareContext = null; //***
                    ExecuteNonPrepare_A(() =>
                    {
                        finished = true;
                    });
                }
                //-------------------------------------------------------------
                while (!finished) ; //wait *** tight loop 
            }
        }

        void ExecuteNonPrepare_A(Action whenFinish)
        {

            _sqlParserMx.UseResultParser();
            _writer.Reset();
            ComQueryPacket.Write(
                _writer,
                _sqlStrTemplate.BindValues(_cmdParams, false));

            SendPacket_A(_writer.ToArray(), () =>
            {
                //send complete 
                //then recev result
                RecvPacket_A(whenFinish);
            });
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
            //---------------------------------------------------------------------------------
            ResetPrepareStmt_A(() =>
            {
                //The server will send a OK_Packet if the statement could be reset, a ERR_Packet if not.
                //---------------------------------------------------------------------------------
                _prepareStatementMode = true;
                _sqlParserMx.UseResultParser(true);
                _writer.Reset();

                //fill prepared values 
                ComExecPrepareStmtPacket.Write(_writer,
                    _prepareContext.statementId,
                    _prepareContext.PrepareBoundData(_cmdParams));

                SendPacket_A(_writer.ToArray(), () =>
                {
                    RecvPacket_A(nextAction);
                });
            });
        }

        //----------------------------------------------------------------------------------
        void ClosePrepareStmt_A(Action nextAction)
        {
            if (_prepareContext != null)
            {

                _sqlParserMx.UseResultParser();
                _writer.Reset();
                ComStmtClosePacket.Write(_writer, _prepareContext.statementId);
                //TODO: review here
                SendPacket_A(_writer.ToArray(), nextAction);
            }
            else
            {
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
                SendPacket_A(_writer.ToArray(), () =>
                {
                    RecvPacket_A(nextAction);
                });
                //The server will send a OK_Packet if the statement could be reset, a ERR_Packet if not.
            }
            else
            {
                nextAction();
            }
        }

        //*** blocking
        public void Close()
        {
            //-------------------
            //blocking method***
            //wait until execute finish 
            //------------------- 
            bool finish = false;
            ClosePrepareStmt_A(() =>
            {
                finish = true;
            });
            //------------------- 
            while (!finish) ; //** tight loop **
            //-------------------  
        }
        void RecvPacket_A(Action whenFinish)
        {
            bool isPartial = false;
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
                                MySqlOk ok = result as MySqlOk;
                                OkPacket = ok.okpacket;
                            }
                            break;
                        case MySqlResultKind.Error:
                            {
                                MySqlError error = result as MySqlError;
                                LoadError = error.errPacket;
                            }
                            break;
                        case MySqlResultKind.TableResult:
                            {
                                MySqlTableResult tableResult = result as MySqlTableResult;
                                isPartial = tableResult.IsPartialTable;
                                if (tableResultArrived != null)
                                {
                                    tableResultArrived(tableResult);
                                }
                            }
                            break;
                    }
                }
                //-----------------
                whenFinish();
                //-----------------
            });
        }
        void SendPacket_A(byte[] packetBuffer, Action nextAction)
        {
            //send all to 
            _conn.StartSend(packetBuffer, 0, packetBuffer.Length, nextAction);
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
            int fieldCount = tableHeader.ColumnCount;
            _preparedValues = new MyStructData[fieldCount];
            if (_keys.Count != fieldCount)
            {
                throw new Exception("key num not matched!");
            }
            //add field information to _keys

            List<FieldPacket> fields = tableHeader.GetFields();
            for (int i = 0; i < fieldCount; ++i)
            {
                _keys[i].fieldInfo = fields[i];
            }
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