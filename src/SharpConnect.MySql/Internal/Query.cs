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
    class Query
    {
        public bool typeCast;
        public bool nestTables;
        CommandParams _cmdParams;
        readonly Connection _conn;
        TableHeader _tableHeader;
        RowDataPacket _lastRow;
        RowPreparedDataPacket _lastPrepareRow;
        bool _hasSomeRow;
        bool _executePrepared;
        MySqlParserMx _sqlParserMx;
        PacketWriter _writer;
        SqlStringTemplate _sqlStrTemplate;
        PreparedContext _prepareContext;
        public Query(Connection conn, string sql, CommandParams cmdParams)//testing
        {
            if (sql == null)
            {
                throw new Exception("Sql command can not null.");
            }
            this._conn = conn;
            this._cmdParams = cmdParams;
            typeCast = conn.config.typeCast;
            nestTables = false;
            //index = 0;
            LoadError = null;
            //*** query use conn resource such as parser,writer
            //so 1 query 1 connection
            //_parser = conn.PacketParser;
            _sqlParserMx = conn.ParserMx;
            _writer = conn.PacketWriter;
            //_receiveBuffer = null;
            _sqlStrTemplate = new SqlStringTemplate(sql);
        }

        public ErrPacket LoadError { get; private set; }
        public OkPacket OkPacket { get; private set; }

        internal MyStructData[] Cells
        {
            get
            {
                if (_prepareContext != null)
                {
                    return _lastPrepareRow.Cells;
                }
                else
                {
                    return _lastRow.Cells;
                }
            }
        }

        //*** blocking
        public void Prepare()
        {
            //-------------------
            //blocking method***
            //wait until execute finish 
            //-------------------
            //prepare sql query
            _sqlParserMx.CurrentPacketParser = new PrepareResponsePacketParser(_conn.IsProtocol41);
            _prepareContext = null;
            if (_cmdParams == null)
            {
                return;
            }

            _writer.Reset();
            string realSql = _sqlStrTemplate.BindValues(_cmdParams, true);
            ComPrepareStatementPacket preparePacket = new ComPrepareStatementPacket(realSql);
            preparePacket.WritePacket(_writer);
            //-------------------------------------------------------------
            bool finished = false;
            SendPacket_A(_writer.ToArray(), () =>
            {
                _conn.StartReceive(result =>
                {
                    if (result is MySqlPrepareResponse)
                    {
                        MySqlPrepareResponse response = result as MySqlPrepareResponse;
                        _prepareContext = new PreparedContext(response.okPacket.statement_id, _sqlStrTemplate);
                        _tableHeader = response.tableHeader;
                        _prepareContext.Setup(_tableHeader);
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
            _rowReadIndex = 0;
            //------------------- 
            if (nextAction != null)
            {
                //not block
                if (_prepareContext != null)
                {
                    if (_cmdParams == null)
                    {
                        nextAction();
                        return;
                    }
                    ExecutePrepareQuery_A(nextAction);
                }
                else
                {
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
            _sqlParserMx.CurrentPacketParser = new ResultPacketParser(_conn.config, _conn.IsProtocol41);
            _writer.Reset();
            string realSql = _sqlStrTemplate.BindValues(_cmdParams, false);
            var queryPacket = new ComQueryPacket(realSql);
            queryPacket.WritePacket(_writer);
            SendPacket_A(_writer.ToArray(), () =>
            {
                //send complete 
                //then recev result
                ParseRecvPacket_A(whenFinish);
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
                //---------------------------------------------------------------------------------
                _executePrepared = true;
                _writer.Reset();
                _sqlParserMx.CurrentPacketParser = new ResultPacketParser(_conn.config, _conn.IsProtocol41, true);
                //fill prepared values 
                var excute = new ComExecutePrepareStatement(_prepareContext.statementId, _prepareContext.PrepareBoundData(_cmdParams));
                excute.WritePacket(_writer);
                SendPacket_A(_writer.ToArray(), () =>
                {
                    ParseRecvPacket_A(nextAction);
                });
            });
        }

        //----------------------------------------------------------------------------------
        void ClosePrepareStmt_A(Action nextAction)
        {
            if (_prepareContext != null)
            {
                _writer.Reset();
                _sqlParserMx.CurrentPacketParser = new ResultPacketParser(_conn.config, _conn.IsProtocol41);
                ComStmtClose closePrepare = new ComStmtClose(_prepareContext.statementId);
                closePrepare.WritePacket(_writer);
                //TODO: review here
                SendPacket_A(_writer.ToArray(), () => nextAction());
                //SendPacket(_writer.ToArray()); //***
            }
            else
            {
                nextAction();
            }
        }

        void ResetPrepareStmt_A(Action nextAction)
        {
            if (_executePrepared && _prepareContext != null)
            {
                _writer.Reset();
                _sqlParserMx.CurrentPacketParser = new ResultPacketParser(_conn.config, _conn.IsProtocol41);
                ComStmtReset resetPacket = new ComStmtReset(_prepareContext.statementId);
                resetPacket.WritePacket(_writer);
                SendPacket_A(_writer.ToArray(), () =>
                {
                    ParseRecvPacket_A(nextAction);
                });
                //The server will send a OK_Packet if the statement could be reset, a ERR_Packet if not.
            }
            else
            {
                nextAction();
            }
        }

        //*** blocking
        public bool ReadRow()
        {
            //-------------------
            //blocking method***
            //wait until execute finish 
            //-------------------  
            InternalReadRow(); //blocking with tight loop
            //note that load waiting data may not complete in first round       
            //we wait here   
            return _hasSomeRow;
        }
        int _rowReadIndex = 0;
        //*** blocking
        void InternalReadRow()
        {
            _hasSomeRow = false;
            //****
            //return true when we finish
            //**** 
            if (_tableHeader == null)
            {
                return; //ok,finish
            }

            MySqlResult result = _sqlParserMx.ResultPacket;
            if (result == null)
            {
                if (!_sqlParserMx.IsComplete)
                {
                    //---------------------------------------------------
                    //** tight loop**
                    //waiting for parse
                    while (_sqlParserMx.ResultPacket == null)
                    {
                    }
                    //exit loop when has result packet
                    result = _sqlParserMx.ResultPacket;
                    //---------------------------------------------------
                }
                else
                {
                    throw new NotSupportedException("Unexpected Result Type");
                }
            }

            //has some result
            switch (result.Kind)
            {
                case MySqlResultKind.Ok:
                    MySqlOk ok = result as MySqlOk;
                    OkPacket = ok.okpacket;
                    break;
                case MySqlResultKind.Error:
                    MySqlError error = result as MySqlError;
                    LoadError = error.errPacket;
                    break;
                case MySqlResultKind.TableResult:
                    {
                        MySqlTableResult tableResult = _sqlParserMx.ResultPacket as MySqlTableResult;
                        //TODO: review here
                        if (_rowReadIndex >= tableResult.rows.Count)
                        {
                        }
                        else
                        {
                            var lastRow = tableResult.rows[_rowReadIndex];
                            _rowReadIndex++;
                            _lastRow = lastRow;
                            _hasSomeRow = true; //***
                        }
                    }
                    break;
                case MySqlResultKind.PrepareTableResult:
                    {
                        MySqlPrepareTableResult prepareResult = _sqlParserMx.ResultPacket as MySqlPrepareTableResult;
                        //TODO: review here
                        if (_rowReadIndex >= prepareResult.rows.Count)
                        {
                        }
                        else
                        {
                            var lastRow = prepareResult.rows[_rowReadIndex];
                            _rowReadIndex++;
                            _lastPrepareRow = lastRow;
                            _hasSomeRow = true;
                        }
                    }
                    break;
                default:
                    {
                        //unknown result kind
                        throw new NotSupportedException();
                    }
            }
        }

        public int GetColumnIndex(string colName)
        {
            return _tableHeader.GetFieldIndex(colName);
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
            if (_hasSomeRow)
            {
                //TODO : review here ?
                //we use another connection to kill current th
                string realSql = "KILL " + _conn.threadId;
                //sql = "FLUSH QUERY CACHE;";
                Connection killConn = new Connection(_conn.config);
                killConn.Connect();
                var q = new Query(killConn, realSql, null);
                q.Execute();
                _conn.ClearRemainingInputBuffer();
                killConn.Disconnect();
            }
        }

        void ParseRecvPacket_A(Action whenFinish)
        {
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
                                _tableHeader = tableResult.tableHeader;
                            }
                            break;
                        case MySqlResultKind.PrepareTableResult:
                            {
                                MySqlPrepareTableResult prepareResult = result as MySqlPrepareTableResult;
                                _tableHeader = prepareResult.tableHeader;
                            }
                            break;
                    }
                }

                //-----------------
                //recv complete
                whenFinish();
                //-----------------
            });
        }
        void SendPacket_A(byte[] packetBuffer, Action whenSendComplete)
        {
            //send all to 
            _conn.StartSendData(packetBuffer, 0, packetBuffer.Length, whenSendComplete);
        }
    }

    class PreparedContext
    {
        public readonly uint statementId;
        TableHeader _tableHeader;
        SqlStringTemplate _sqlStringTemplate;
        MyStructData[] _preparedValues;
        List<SqlBoundSection> _keys;
        public PreparedContext(uint statementId, SqlStringTemplate sqlStringTemplate)
        {
            this.statementId = statementId;
            _sqlStringTemplate = sqlStringTemplate;
            _keys = _sqlStringTemplate.GetValueKeys();
        }
        public void Setup(TableHeader tableHeader)
        {
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

        public bool TypeCast { get; set; }
        public bool NestTables { get; set; }
        public ConnectionConfig ConnConfig { get; set; }
    }
}