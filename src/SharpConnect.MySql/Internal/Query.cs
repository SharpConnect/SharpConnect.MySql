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
using System.Text;
using System.Threading;

namespace MySqlPacket
{
    class Query
    {
        string _realSql;
        string _rawSql;
        CommandParams _cmdParams;
        Connection _conn;

        public bool typeCast;
        public bool nestTables;

        TableHeader _tableHeader;
        public ErrPacket loadError { get; private set; }
        public OkPacket okPacket { get; private set; }
        //public int index;

        RowDataPacket _lastRow;
        RowPrepaqreDataPacket _lastPrepareRow;

        bool IsPrepare;
        bool hasSomeRow;

        PacketParser _parser;
        PacketWriter _writer;

        List<string> _keys;//all keys
        List<string> _sqlSection;
        List<string> _valuesKeys;

        byte[] _receiveBuffer;
        const int _defaultBufferSize = 512;
        const byte _errorCode = 255;
        const byte _eofCode = 0xfe;
        const byte _okCode = 0;
        
        const int _maxPacketLength = (1 << 24) - 1;//(int)Math.Pow(2, 24) - 1;
        internal MyStructData[] _Cells
        {
            get
            {
                if (IsPrepare)
                {
                    return _lastPrepareRow.Cells;
                }
                else
                {
                    return _lastRow.Cells;
                }
            }
        }

        public Query(Connection conn, string sql, CommandParams command)
        {
            if (sql == null)
            {
                throw new Exception("Sql command can not null.");
            }
            _conn = conn;
            _rawSql = sql;
            _cmdParams = command;

            typeCast = conn.config.typeCast;
            nestTables = false;

            //index = 0;
            loadError = null;

            //*** query use conn resource such as parser,writer
            //so 1 query 1 connection
            _parser = conn.PacketParser;
            _writer = conn.PacketWriter;

            _receiveBuffer = null;

            _keys = new List<string>();
            _sqlSection = new List<string>();
            _valuesKeys = new List<string>();
            IsPrepare = PrepareChecker();
            _realSql = CombindAndReplaceSqlSection();
        }

        public void Execute()
        {
            if (IsPrepare)
            {
                ExecutePrepareQuery();
            }
            else
            {
                ExecuteNonPrepare();
            }
        }

        public void ExecuteNonPrepare()
        {
            _writer.Reset();
            ComQueryPacket queryPacket = new ComQueryPacket(_realSql);
            queryPacket.WritePacket(_writer);
            SendPacket(_writer.ToArray());

            IsPrepare = false;
            ParseReceivePacket();
        }

        public void ExecutePrepareQuery()
        {
            if (_cmdParams == null)
            {
                return;
            }

            if (!IsPrepare)
            {
                ExecuteNonPrepare();
                return;
            }
            _writer.Reset();
            ComPrepareStatementPacket preparePacket = new ComPrepareStatementPacket(_realSql);
            preparePacket.WritePacket(_writer);
            SendPacket(_writer.ToArray());

            OkPrepareStmtPacket okPreparePacket = new OkPrepareStmtPacket();
            okPreparePacket = ParsePrepareResponse();
            if (okPreparePacket != null)
            {
                if (okPreparePacket.num_params > 0)
                {
                    FieldPacket[] fields = new FieldPacket[okPreparePacket.num_params];
                    for (int i = 0; i < okPreparePacket.num_params; i++)
                    {
                        fields[i] = ParseColumn();
                    }
                    ParseEOF();
                }
                if (okPreparePacket.num_columns > 0)
                {
                    _tableHeader = new TableHeader();
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

                _writer.Reset();
                ComExecutePrepareStatement excute;
                excute = new ComExecutePrepareStatement(okPreparePacket.statement_id, _cmdParams, _valuesKeys);
                excute.WritePacket(_writer);
                SendPacket(_writer.ToArray());
                IsPrepare = true;
                ParseReceivePacket();
                if (okPacket != null || loadError != null)
                {
                    return;
                }
                _lastPrepareRow = new RowPrepaqreDataPacket(_tableHeader);
            }
        }

        public bool ReadRow()
        {
            if (_tableHeader == null)
            {
                return hasSomeRow = false;
            }

            switch (_receiveBuffer[_parser.Position + 4])
            {
                case _errorCode:
                    {
                        loadError = new ErrPacket();
                        loadError.ParsePacket(_parser);
                        return hasSomeRow = false;
                    }
                case _eofCode:
                    {
                        EofPacket rowDataEof = ParseEOF();

                        return hasSomeRow = false;
                    }
                default:
                    {
                        if (IsPrepare)
                        {
                            _lastPrepareRow.ReuseSlots();
                            _lastPrepareRow.ParsePacketHeader(_parser);

                            _receiveBuffer = CheckLimit(_lastPrepareRow.GetPacketLength(), _receiveBuffer, _defaultBufferSize);
                            _lastPrepareRow.ParsePacket(_parser);
                            CheckBeforeParseHeader(_receiveBuffer);
                        }
                        else
                        {
                            _lastRow.ReuseSlots();
                            _lastRow.ParsePacketHeader(_parser);

                            _receiveBuffer = CheckLimit(_lastRow.GetPacketLength(), _receiveBuffer, _defaultBufferSize);
                            _lastRow.ParsePacket(_parser);
                            CheckBeforeParseHeader(_receiveBuffer);
                        }
                        return hasSomeRow = true;
                    }
            }
        }

        public int GetColumnIndex(string colName)
        {
            return _tableHeader.GetFieldIndex(colName);
        }

        public void Close()
        {
            if (hasSomeRow)
            {
                _realSql = "KILL " + _conn.threadId;
                Connection killConn = new Connection(_conn.config);
                killConn.Connect();
                killConn.CreateQuery(_realSql, null).ExecuteNonPrepare();
                _conn.ClearRemainingInputBuffer();
                killConn.Disconnect();
            }
        }

        void ParseReceivePacket()
        {
            _receiveBuffer = new byte[_defaultBufferSize];
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
                case _errorCode:
                    loadError = new ErrPacket();
                    loadError.ParsePacket(_parser);
                    break;
                case 0xfe://0x00 or 0xfe the OK packet header
                case _okCode:
                    okPacket = new OkPacket(_conn.IsProtocol41);
                    okPacket.ParsePacket(_parser);
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

            _tableHeader = new TableHeader();
            _tableHeader.TypeCast = typeCast;
            _tableHeader.NestTables = nestTables;
            _tableHeader.ConnConfig = _conn.config;

            bool protocol41 = _conn.IsProtocol41;

            while (_receiveBuffer[_parser.Position + 4] != _eofCode)
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

        bool PrepareChecker()
        {
            ParseSql();
            FindValueKeys();
            return _valuesKeys.Count > 0 ? true : false;
        }

        void ParseSql()
        {
            int length = _rawSql.Length;
            ParseState state = ParseState.FIND_MARKER;
            char ch;

            StringBuilder strBuilder = new StringBuilder();
            string temp;
            for (int i = 0; i < length; i++)
            {
                ch = _rawSql[i];
                switch (state)
                {
                    case ParseState.FIND_MARKER:
                        if (ch == '?')
                        {
                            temp = strBuilder.ToString();
                            _sqlSection.Add(temp);
                            strBuilder.Length = 0;
                            state = ParseState.GET_KEY;
                            //continue;
                        }
                        strBuilder.Append(ch);
                        break;
                    case ParseState.GET_KEY:
                        if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9'))
                        {
                            strBuilder.Append(ch);
                        }
                        else
                        {
                            temp = strBuilder.ToString();
                            _sqlSection.Add(temp);
                            _keys.Add(temp);
                            strBuilder.Length = 0;
                            state = ParseState.FIND_MARKER;

                            strBuilder.Append(ch);
                        }
                        break;
                    default:
                        break;
                }//end swicth
            }//end for
            temp = strBuilder.ToString();
            if (state == ParseState.GET_KEY)
            {
                _keys.Add(temp);
            }
            _sqlSection.Add(temp);
        }//end method

        void FindValueKeys()
        {
            int count = _keys.Count;
            for(int i=0;i< count; i++)
            {
                if (_cmdParams.IsValueKeys(_keys[i]))
                {
                    _valuesKeys.Add(_keys[i]);
                }
            }
        }

        string CombindAndReplaceSqlSection()
        {
            StringBuilder strBuilder = new StringBuilder();
            int count = _sqlSection.Count;
            string temp;
            for (int i = 0; i < count; i++)
            {
                if (_sqlSection[i][0] == '?')
                {
                    temp = _cmdParams.GetFieldName(_sqlSection[i]);
                    if (temp != null)
                    {
                        strBuilder.Append(temp);
                    }
                    else if(_cmdParams.IsValueKeys(_sqlSection[i]))
                    {
                        strBuilder.Append('?');
                    }
                    else
                    {
                        throw new Exception("Error : This key not assign.");
                    }
                }
                else
                {
                    strBuilder.Append(_sqlSection[i]);
                }
            }
            
            return strBuilder.ToString();
        }

        OkPrepareStmtPacket ParsePrepareResponse()
        {
            _receiveBuffer = new byte[_defaultBufferSize];
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
                case _errorCode:
                    loadError = new ErrPacket();
                    loadError.ParsePacket(_parser);
                    okPreparePacket = null;
                    break;
                case _okCode:
                    okPreparePacket.ParsePacket(_parser);
                    break;
            }
            return okPreparePacket;
        }

        FieldPacket ParseColumn()
        {
            FieldPacket fieldPacket = new FieldPacket(_conn.IsProtocol41);
            fieldPacket.ParsePacketHeader(_parser);
            _receiveBuffer = CheckLimit(fieldPacket.GetPacketLength(), _receiveBuffer, _defaultBufferSize);
            fieldPacket.ParsePacket(_parser);
            CheckBeforeParseHeader(_receiveBuffer);
            return fieldPacket;
        }

        EofPacket ParseEOF()
        {
            EofPacket eofPacket = new EofPacket(_conn.IsProtocol41);//if temp[4]=0xfe then eof packet
            eofPacket.ParsePacketHeader(_parser);
            _receiveBuffer = CheckLimit(eofPacket.GetPacketLength(), _receiveBuffer, _defaultBufferSize);
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
            int remainLength = (int)(_parser.Length - _parser.Position);
            if (remainLength < 5)//5 bytes --> 4 bytes from header and 1 byte for find packet type
            {
                Buffer.BlockCopy(buffer, (int)_parser.Position, buffer, 0, remainLength);

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

    enum ParseState
    {
        FIND_MARKER,
        GET_KEY
    }

    public class CommandParams
    {
        Dictionary<string, MyStructData> _prepareValues;
        Dictionary<string, string> _fieldValues;
        MyStructData _reuseData;

        public CommandParams()
        {
            _prepareValues = new Dictionary<string, MyStructData>();
            _fieldValues = new Dictionary<string, string>();
            _reuseData = new MyStructData();
            _reuseData.type = Types.NULL;
        }

        public void AddTable(string key, string tablename)
        {
            key = "?" + key;
            _fieldValues[key] = "`"+tablename+"`";
        }
        public void AddField(string key, string fieldname)
        {
            key = "?" + key;
            _fieldValues[key] = "`"+fieldname+"`";
        }
        public void AddValue(string key, string value)
        {
            if (value != null)
            {
                _reuseData.myString = value;
                _reuseData.type = Types.VAR_STRING;
            }
            else
            {
                _reuseData.myString = null;
                _reuseData.type = Types.NULL;
            }
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, byte value)
        {
            _reuseData.myByte = value;
            _reuseData.type = Types.BIT;
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, int value)
        {
            _reuseData.myInt32 = value;
            _reuseData.type = Types.LONG;//Types.LONG = int32
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, long value)
        {
            _reuseData.myInt64 = value;
            _reuseData.type = Types.LONGLONG;
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, float value)
        {
            _reuseData.myFloat = value;
            _reuseData.type = Types.FLOAT;
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, double value)
        {
            _reuseData.myDouble = value;
            _reuseData.type = Types.DOUBLE;
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, decimal value)
        {
            _reuseData.myDecimal = value;
            _reuseData.type = Types.DECIMAL;
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, byte[] value)
        {
            if (value != null)
            {
                _reuseData.myBuffer = value;
                _reuseData.type = Types.LONG_BLOB;
            }
            else
            {
                _reuseData.myBuffer = null;
                _reuseData.type = Types.NULL;
            }
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, DateTime value)
        {
            _reuseData.myDateTime = value;
            _reuseData.type = Types.DATETIME;
            AddKeyWithReuseData(key);
        }
        void AddKeyWithReuseData(string key)
        {
            key = "?" + key;
            _prepareValues[key] = _reuseData;
        }

        internal MyStructData GetData(string key)
        {
            MyStructData value = new MyStructData();
            string temp;
            if( _prepareValues.TryGetValue(key, out value))
            {
                return value;
            }
            else if (_fieldValues.TryGetValue(key, out temp))
            {
                throw new Exception("Error : This key is table or field key. Please use value key and try again.");
            }
            else
            {
                throw new Exception("Error : Key not found '" + key + "' or value not assigned. Please re-check and try again.");
            }
        }
        internal string GetFieldName(string key)
        {
            MyStructData value = new MyStructData();
            string temp;
            if (_prepareValues.TryGetValue(key, out value))
            {
                return null;
            }
            else if (_fieldValues.TryGetValue(key, out temp))
            {
                return temp;
            }
            else
            {
                return null;
            }
        }
        internal bool IsValueKeys(string key)
        {
            return _prepareValues.TryGetValue(key, out _reuseData);
        }
    }

    class TableHeader
    {
        List<FieldPacket> _fields;
        Dictionary<string, int> _fieldNamePosMap;

        public TableHeader()
        {
            _fields = new List<FieldPacket>();
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
            get { return _fields.Count; }
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