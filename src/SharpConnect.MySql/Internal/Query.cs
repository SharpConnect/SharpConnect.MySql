//LICENSE: MIT
//Copyright(c) 2012 Felix Geisendörfer(felix @debuggable.com) and contributors 
//Copyright(c) 2015 brezza27, EngineKit and contributors

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
using System.Net.Sockets;
using System.Threading;

namespace SharpConnect.MySql.Internal
{
    class Query
    {

        public bool typeCast;
        public bool nestTables;

        CommandParams _cmdParams;
        Connection _conn;
        TableHeader _tableHeader;
        RowDataPacket _lastRow;
        RowPreparedDataPacket _lastPrepareRow;
        bool _hasSomeRow;

        PacketParser _parser;
        PacketWriter _writer;
        SqlStringTemplate _sqlStrTemplate;
        PreparedContext _prepareContext; 
        byte[] _receiveBuffer;


        const int DEFAULT_BUFFER_SIZE = 512;
        const byte ERROR_CODE = 255;
        const byte EOF_CODE = 0xfe;
        const byte OK_CODE = 0;
        const int MAX_PACKET_LENGTH = (1 << 24) - 1;//(int)Math.Pow(2, 24) - 1; 


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
            _parser = conn.PacketParser;
            _writer = conn.PacketWriter;
            _receiveBuffer = null;

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

        public void Execute()
        {
            if (_prepareContext != null)
            {
                ExecutePrepareQuery();
            }
            else
            {
                ExecuteNonPrepare();
            }
        }

        void ExecuteNonPrepare()
        {
            _writer.Reset();

            string realSql = _sqlStrTemplate.BindValues(_cmdParams, false);

            var queryPacket = new ComQueryPacket(realSql);
            queryPacket.WritePacket(_writer);
            SendPacket(_writer.ToArray());

            _prepareContext = null;
            ParseReceivePacket();
        }
        public void Prepare()
        {
            //prepare sql query


            this._prepareContext = null;

            if (_cmdParams == null)
            {
                return;
            }

            _writer.Reset();

            string realSql = _sqlStrTemplate.BindValues(_cmdParams, true);
            ComPrepareStatementPacket preparePacket = new ComPrepareStatementPacket(realSql);

            preparePacket.WritePacket(_writer);
            SendPacket(_writer.ToArray());

            OkPrepareStmtPacket okPreparePacket = new OkPrepareStmtPacket();
            okPreparePacket = ParsePrepareResponse();
            if (okPreparePacket != null)
            {
                _prepareContext = new PreparedContext(okPreparePacket.statement_id, _sqlStrTemplate);

                if (okPreparePacket.num_params > 0)
                {
                    var tableHeader = new TableHeader();
                    tableHeader.TypeCast = typeCast;
                    tableHeader.NestTables = nestTables;
                    tableHeader.ConnConfig = _conn.config;
                    for (int i = 0; i < okPreparePacket.num_params; i++)
                    {
                        FieldPacket field = ParseColumn();
                        tableHeader.AddField(field);
                    }

                    //set table after the table is ready!
                    _prepareContext.Setup(tableHeader);

                    ParseEOF();

                }
                if (okPreparePacket.num_columns > 0)
                {
                    this._tableHeader = new TableHeader();
                    _tableHeader.TypeCast = typeCast;
                    _tableHeader.NestTables = nestTables;
                    _tableHeader.ConnConfig = _conn.config;

                    for (int i = 0; i < okPreparePacket.num_columns; i++)
                    {
                        FieldPacket field = ParseColumn();
                        _tableHeader.AddField(field);
                    }
                    ParseEOF();
                }

            }

        }
        void ExecutePrepareQuery()
        {
            if (_cmdParams == null)
            {
                return;
            }

            if (_prepareContext == null)
            {
                ExecuteNonPrepare();
                return;
            }

            if (_prepareContext.statementId == 0)
            {
                throw new Exception("exec Prepare() first");
            }
            //---------------------------------------------------------------------------------


            _writer.Reset();

            //fill prepared values 
            var excute = new ComExecutePrepareStatement(_prepareContext.statementId, _prepareContext.PrepareBoundData(_cmdParams));

            excute.WritePacket(_writer);

            SendPacket(_writer.ToArray());
            ParseReceivePacket();

            if (OkPacket != null || LoadError != null)
            {
                return;
            }
            _lastPrepareRow = new RowPreparedDataPacket(_tableHeader);

        }



        public bool ReadRow()
        {
            if (_tableHeader == null)
            {
                return _hasSomeRow = false;
            }

            switch (_receiveBuffer[_parser.Position + 4])
            {
                case ERROR_CODE:
                    {
                        LoadError = new ErrPacket();
                        LoadError.ParsePacket(_parser);
                        return _hasSomeRow = false;
                    }
                case EOF_CODE:
                    {
                        EofPacket rowDataEof = ParseEOF();

                        return _hasSomeRow = false;
                    }
                default:
                    {
                        if (_prepareContext != null)
                        {

                            _lastPrepareRow.ReuseSlots();
                            _lastPrepareRow.ParsePacketHeader(_parser);

                            _receiveBuffer = CheckLimit(_lastPrepareRow.GetPacketLength(), _receiveBuffer, DEFAULT_BUFFER_SIZE);
                            _lastPrepareRow.ParsePacket(_parser);
                            CheckBeforeParseHeader(_receiveBuffer);
                        }
                        else
                        {
                            _lastRow.ReuseSlots();
                            _lastRow.ParsePacketHeader(_parser);

                            _receiveBuffer = CheckLimit(_lastRow.GetPacketLength(), _receiveBuffer, DEFAULT_BUFFER_SIZE);
                            _lastRow.ParsePacket(_parser);
                            CheckBeforeParseHeader(_receiveBuffer);
                        }
                        return _hasSomeRow = true;
                    }
            }
        }

        public int GetColumnIndex(string colName)
        {
            return this._tableHeader.GetFieldIndex(colName);
        }

        public void Close()
        {
            if (_hasSomeRow)
            {
                string realSql = "KILL " + _conn.threadId;
                //sql = "FLUSH QUERY CACHE;";
                Connection killConn = new Connection(_conn.config);
                killConn.Connect();
                killConn.CreateQuery(realSql, null).Execute();
                _conn.ClearRemainingInputBuffer();
                killConn.Disconnect();
            }
        }

        void ParseReceivePacket()
        {
            //TODO: review here, optimized buffer
            _receiveBuffer = new byte[DEFAULT_BUFFER_SIZE];
            var socket = _conn.socket;
            int receive = socket.Receive(_receiveBuffer);
            if (receive == 0)
            {
                return;
            }

            //---------------------------------------------------
            _parser.LoadNewBuffer(_receiveBuffer, receive);
            switch (_receiveBuffer[4])
            {
                case ERROR_CODE:
                    LoadError = new ErrPacket();
                    LoadError.ParsePacket(_parser);
                    break;
                case 0xfe://0x00 or 0xfe the OK packet header
                case OK_CODE:
                    OkPacket = new OkPacket(_conn.IsProtocol41);
                    OkPacket.ParsePacket(_parser);
                    break;
                default:
                    ParseResultSet();
                    break;
            }
        }

        void ParseResultSet()
        {
            ResultSetHeaderPacket resultPacket = new ResultSetHeaderPacket();
            resultPacket.ParsePacket(_parser);

            this._tableHeader = new TableHeader();
            _tableHeader.TypeCast = typeCast;
            _tableHeader.NestTables = nestTables;
            _tableHeader.ConnConfig = _conn.config;

            bool protocol41 = _conn.IsProtocol41;

            while (_receiveBuffer[_parser.Position + 4] != EOF_CODE)
            {
                FieldPacket fieldPacket = ParseColumn();
                _tableHeader.AddField(fieldPacket);
            }

            EofPacket fieldEof = ParseEOF();
            //-----
            _lastRow = new RowDataPacket(_tableHeader);
        }

        void SendPacket(byte[] packetBuffer)
        {
            int sent = 0;
            int packetLength = packetBuffer.Length;
            var socket = _conn.socket;

            while (sent < packetLength)
            {//if packet is large
                sent += socket.Send(packetBuffer, sent, packetLength - sent, SocketFlags.None);
            }
        }


        OkPrepareStmtPacket ParsePrepareResponse()
        {
            _receiveBuffer = new byte[DEFAULT_BUFFER_SIZE];
            var socket = _conn.socket;
            int receive = socket.Receive(_receiveBuffer);
            if (receive == 0)
            {
                return null;
            }
            //---------------------------------------------------
            _parser.LoadNewBuffer(_receiveBuffer, receive);
            OkPrepareStmtPacket okPreparePacket = new OkPrepareStmtPacket();
            switch (_receiveBuffer[4])
            {
                case ERROR_CODE:
                    LoadError = new ErrPacket();
                    LoadError.ParsePacket(_parser);
                    okPreparePacket = null;
                    break;
                case OK_CODE:
                    okPreparePacket.ParsePacket(_parser);
                    break;
            }
            return okPreparePacket;
        }

        FieldPacket ParseColumn()
        {
            FieldPacket fieldPacket = new FieldPacket(_conn.IsProtocol41);
            fieldPacket.ParsePacketHeader(_parser);
            _receiveBuffer = CheckLimit(fieldPacket.GetPacketLength(), _receiveBuffer, DEFAULT_BUFFER_SIZE);
            fieldPacket.ParsePacket(_parser);
            CheckBeforeParseHeader(_receiveBuffer);
            return fieldPacket;
        }

        EofPacket ParseEOF()
        {
            EofPacket eofPacket = new EofPacket(_conn.IsProtocol41);//if temp[4]=0xfe then eof packet
            eofPacket.ParsePacketHeader(_parser);
            _receiveBuffer = CheckLimit(eofPacket.GetPacketLength(), _receiveBuffer, DEFAULT_BUFFER_SIZE);
            eofPacket.ParsePacket(_parser);

            CheckBeforeParseHeader(_receiveBuffer);
            return eofPacket;
        }

        byte[] CheckLimit(uint packetLength, byte[] buffer, int limit)
        {
            int remainLength = (int)(_parser.Length - _parser.Position);
            if (packetLength > remainLength)
            {
                int packetRemainLength = (int)packetLength - remainLength;
                int newReceiveBuffLength = (packetLength > limit) ? packetRemainLength : limit;
                int newBufferLength = newReceiveBuffLength + remainLength;

                if (newBufferLength > buffer.Length)
                {
                    var tmpBuffer = new byte[newBufferLength];
                    Buffer.BlockCopy(buffer, (int)_parser.Position, tmpBuffer, 0, remainLength);
                    buffer = tmpBuffer;
                }
                else
                {
                    //use same buffer
                    //just move
                    Buffer.BlockCopy(buffer, (int)_parser.Position, buffer, 0, remainLength);
                }

                var socket = _conn.socket;
                int newReceive = remainLength + socket.Receive(buffer, remainLength, newReceiveBuffLength, SocketFlags.None);
                int timeoutCountdown = 10000;
                while (newReceive < newBufferLength)
                {
                    int available = socket.Available;
                    if (available > 0)
                    {
                        if (newReceive + available < newBufferLength)
                        {
                            newReceive += socket.Receive(buffer, newReceive, available, SocketFlags.None);
                        }
                        else
                        {
                            newReceive += socket.Receive(buffer, newReceive, newBufferLength - newReceive, SocketFlags.None);
                        }
                        timeoutCountdown = 10000;//timeoutCountdown maybe < 10000 when socket receive faster than server send data
                    }
                    else
                    {
                        Thread.Sleep(100);//sometime socket maybe receive faster than server send data
                        timeoutCountdown -= 100;
                        if (socket.Available > 0)
                        {
                            continue;
                        }
                        if (timeoutCountdown <= 0)//sometime server maybe error
                        {
                            break;
                        }
                    }
                }
                _parser.LoadNewBuffer(buffer, newBufferLength);
            }
            return buffer;
        }

        void CheckBeforeParseHeader(byte[] buffer)
        {
            //todo: check memory mx again
            int remainLength = (int)(_parser.Length - _parser.Position);
            if (remainLength < 5)//5 bytes --> 4 bytes from header and 1 byte for find packet type
            {
                //byte[] remainBuff = new byte[remainLength];
                Buffer.BlockCopy(buffer, (int)_parser.Position, buffer, 0, remainLength);
                //remainBuff.CopyTo(buffer, 0);

                var socket = _conn.socket;
                int bufferRemain = buffer.Length - remainLength;
                int available = socket.Available;
                if (available == 0)//it finished
                {
                    return;
                }
                int expectedReceive = (available < bufferRemain ? available : bufferRemain);

                int realReceive = socket.Receive(buffer, remainLength, expectedReceive, SocketFlags.None);
                int newBufferLength = remainLength + realReceive;//sometime realReceive != expectedReceive
                _parser.LoadNewBuffer(buffer, newBufferLength);
                dbugConsole.WriteLine("CheckBeforeParseHeader : LoadNewBuffer");
            }
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