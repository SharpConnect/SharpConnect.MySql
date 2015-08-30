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
using System.Text;
using System.IO;

using System.Threading;
using System.Net;
using System.Net.Sockets;


namespace MySqlPacket
{
    class Query
    {


        CommandParams cmdParams;
        Connection conn;

        public bool typeCast;
        public bool nestTables;

        TableHeader tableHeader;

        RowDataPacket lastRow;
        RowPreparedDataPacket lastPrepareRow;

        bool _isPrepared;
        bool hasSomeRow;

        PacketParser parser;
        PacketWriter writer;

        SqlStringTemplate sqlStrTemplate;

        //---------------
        uint _prepareStmtId;



        //---------------
        byte[] receiveBuffer;

        const int DEFAULT_BUFFER_SIZE = 512;
        const byte ERROR_CODE = 255;
        const byte EOF_CODE = 0xfe;
        const byte OK_CODE = 0;
        const int MAX_PACKET_LENGTH = (1 << 24) - 1;//(int)Math.Pow(2, 24) - 1;


        public Query(Connection conn, string sql, CommandParams cmdParams, bool forPreparedStmt = false)//testing
        {
            if (sql == null)
            {
                throw new Exception("Sql command can not null.");
            }
            this.conn = conn;

            this.cmdParams = cmdParams;

            typeCast = conn.config.typeCast;
            nestTables = false;

            //index = 0;
            loadError = null;

            //*** query use conn resource such as parser,writer
            //so 1 query 1 connection
            parser = conn.PacketParser;
            writer = conn.PacketWriter;
            receiveBuffer = null;

            sqlStrTemplate = new SqlStringTemplate(sql);

        }

        public ErrPacket loadError { get; private set; }
        public OkPacket okPacket { get; private set; }

        internal MyStructData[] Cells
        {
            get
            {
                if (_isPrepared)
                {
                    return lastPrepareRow.Cells;
                }
                else
                {
                    return lastRow.Cells;
                }
            }
        }

        public void Execute()
        {
            if (_isPrepared)
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
            writer.Reset();

            var realSql = sqlStrTemplate.BindValues(cmdParams, false);

            ComQueryPacket queryPacket = new ComQueryPacket(realSql);
            queryPacket.WritePacket(writer);
            SendPacket(writer.ToArray());

            _isPrepared = false;
            ParseReceivePacket();
        }
        public void Prepare()
        {
            //prepare sql query
            this._prepareStmtId = 0;//reset  
            _isPrepared = true;
            if (cmdParams == null)
            {
                return;
            }

            writer.Reset();
            string realSql = sqlStrTemplate.BindValues(cmdParams, true);

            ComPrepareStatementPacket preparePacket = new ComPrepareStatementPacket(realSql);
            preparePacket.WritePacket(writer);
            SendPacket(writer.ToArray());

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
                    this.tableHeader = new TableHeader();
                    tableHeader.TypeCast = typeCast;
                    tableHeader.NestTables = nestTables;
                    tableHeader.ConnConfig = conn.config;

                    for (int i = 0; i < okPreparePacket.num_columns; i++)
                    {
                        FieldPacket field = ParseColumn();
                        tableHeader.AddField(field);
                    }
                    ParseEOF();
                }

                //-------------------
                _prepareStmtId = okPreparePacket.statement_id;
                //-------------------
            }

        }
        public void ExecutePrepareQuery()
        {
            if (cmdParams == null)
            {
                return;
            }

            if (!_isPrepared)
            {
                ExecuteNonPrepare();
                return;
            }

            if (_prepareStmtId == 0)
            {
                throw new Exception("exec Prepare() first");
            }
            //---------------------------------------------------------------------------------
            writer.Reset();
            var excute = new ComExecutePrepareStatement(_prepareStmtId, cmdParams, sqlStrTemplate);
            excute.WritePacket(writer);
            SendPacket(writer.ToArray());
            _isPrepared = true;
            ParseReceivePacket();
            if (okPacket != null || loadError != null)
            {
                return;
            }
            lastPrepareRow = new RowPreparedDataPacket(tableHeader);

        }



        public bool ReadRow()
        {
            if (tableHeader == null)
            {
                return hasSomeRow = false;
            }

            switch (receiveBuffer[parser.Position + 4])
            {
                case ERROR_CODE:
                    {
                        loadError = new ErrPacket();
                        loadError.ParsePacket(parser);
                        return hasSomeRow = false;
                    }
                case EOF_CODE:
                    {
                        EofPacket rowDataEof = ParseEOF();

                        return hasSomeRow = false;
                    }
                default:
                    {
                        if (_isPrepared)
                        {
                            lastPrepareRow.ReuseSlots();
                            lastPrepareRow.ParsePacketHeader(parser);

                            receiveBuffer = CheckLimit(lastPrepareRow.GetPacketLength(), receiveBuffer, DEFAULT_BUFFER_SIZE);
                            lastPrepareRow.ParsePacket(parser);
                            CheckBeforeParseHeader(receiveBuffer);
                        }
                        else
                        {
                            lastRow.ReuseSlots();
                            lastRow.ParsePacketHeader(parser);

                            receiveBuffer = CheckLimit(lastRow.GetPacketLength(), receiveBuffer, DEFAULT_BUFFER_SIZE);
                            lastRow.ParsePacket(parser);
                            CheckBeforeParseHeader(receiveBuffer);
                        }
                        return hasSomeRow = true;
                    }
            }
        }

        public int GetColumnIndex(string colName)
        {
            return this.tableHeader.GetFieldIndex(colName);
        }

        public void Close()
        {
            if (hasSomeRow)
            {
                string realSql = "KILL " + conn.threadId;
                //sql = "FLUSH QUERY CACHE;";
                Connection killConn = new Connection(conn.config);
                killConn.Connect();
                killConn.CreateQuery(realSql, null).ExecuteNonPrepare();
                conn.ClearRemainingInputBuffer();
                killConn.Disconnect();
            }
        }

        void ParseReceivePacket()
        {
            receiveBuffer = new byte[DEFAULT_BUFFER_SIZE];
            var socket = conn.socket;
            int receive = socket.Receive(receiveBuffer);
            if (receive == 0)
            {
                return;
            }

            //---------------------------------------------------
            parser.LoadNewBuffer(receiveBuffer, receive);
            switch (receiveBuffer[4])
            {
                case ERROR_CODE:
                    loadError = new ErrPacket();
                    loadError.ParsePacket(parser);
                    break;
                case 0xfe://0x00 or 0xfe the OK packet header
                case OK_CODE:
                    okPacket = new OkPacket(conn.IsProtocol41);
                    okPacket.ParsePacket(parser);
                    break;
                default:
                    ParseResultSet();
                    break;
            }
        }

        void ParseResultSet()
        {
            ResultSetHeaderPacket resultPacket = new ResultSetHeaderPacket();
            resultPacket.ParsePacket(parser);

            this.tableHeader = new TableHeader();
            tableHeader.TypeCast = typeCast;
            tableHeader.NestTables = nestTables;
            tableHeader.ConnConfig = conn.config;

            bool protocol41 = conn.IsProtocol41;

            while (receiveBuffer[parser.Position + 4] != EOF_CODE)
            {
                FieldPacket fieldPacket = ParseColumn();
                tableHeader.AddField(fieldPacket);
            }

            EofPacket fieldEof = ParseEOF();
            //-----
            lastRow = new RowDataPacket(tableHeader);
        }

        void SendPacket(byte[] packetBuffer)
        {
            int sent = 0;
            int packetLength = packetBuffer.Length;
            var socket = conn.socket;

            while (sent < packetLength)
            {//if packet is large
                sent += socket.Send(packetBuffer, sent, packetLength - sent, SocketFlags.None);
            }
        }


        OkPrepareStmtPacket ParsePrepareResponse()
        {
            receiveBuffer = new byte[DEFAULT_BUFFER_SIZE];
            var socket = conn.socket;
            int receive = socket.Receive(receiveBuffer);
            if (receive == 0)
            {
                return null;
            }
            //---------------------------------------------------
            parser.LoadNewBuffer(receiveBuffer, receive);
            OkPrepareStmtPacket okPreparePacket = new OkPrepareStmtPacket();
            switch (receiveBuffer[4])
            {
                case ERROR_CODE:
                    loadError = new ErrPacket();
                    loadError.ParsePacket(parser);
                    okPreparePacket = null;
                    break;
                case OK_CODE:
                    okPreparePacket.ParsePacket(parser);
                    break;
            }
            return okPreparePacket;
        }

        FieldPacket ParseColumn()
        {
            FieldPacket fieldPacket = new FieldPacket(conn.IsProtocol41);
            fieldPacket.ParsePacketHeader(parser);
            receiveBuffer = CheckLimit(fieldPacket.GetPacketLength(), receiveBuffer, DEFAULT_BUFFER_SIZE);
            fieldPacket.ParsePacket(parser);
            CheckBeforeParseHeader(receiveBuffer);
            return fieldPacket;
        }

        EofPacket ParseEOF()
        {
            EofPacket eofPacket = new EofPacket(conn.IsProtocol41);//if temp[4]=0xfe then eof packet
            eofPacket.ParsePacketHeader(parser);
            receiveBuffer = CheckLimit(eofPacket.GetPacketLength(), receiveBuffer, DEFAULT_BUFFER_SIZE);
            eofPacket.ParsePacket(parser);

            CheckBeforeParseHeader(receiveBuffer);
            return eofPacket;
        }

        byte[] CheckLimit(uint packetLength, byte[] buffer, int limit)
        {
            int remainLength = (int)(parser.Length - parser.Position);
            if (packetLength > remainLength)
            {
                int packetRemainLength = (int)packetLength - remainLength;
                int newReceiveBuffLength = (packetLength > limit) ? packetRemainLength : limit;
                int newBufferLength = newReceiveBuffLength + remainLength;

                if (newBufferLength > buffer.Length)
                {
                    var tmpBuffer = new byte[newBufferLength];
                    Buffer.BlockCopy(buffer, (int)parser.Position, tmpBuffer, 0, remainLength);
                    buffer = tmpBuffer;
                }
                else
                {
                    //use same buffer
                    //just move
                    Buffer.BlockCopy(buffer, (int)parser.Position, buffer, 0, remainLength);
                }

                var socket = conn.socket;
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
                parser.LoadNewBuffer(buffer, newBufferLength);
            }
            return buffer;
        }

        void CheckBeforeParseHeader(byte[] buffer)
        {
            //todo: check memory mx again
            int remainLength = (int)(parser.Length - parser.Position);
            if (remainLength < 5)//5 bytes --> 4 bytes from header and 1 byte for find packet type
            {
                //byte[] remainBuff = new byte[remainLength];
                Buffer.BlockCopy(buffer, (int)parser.Position, buffer, 0, remainLength);
                //remainBuff.CopyTo(buffer, 0);

                var socket = conn.socket;
                int bufferRemain = buffer.Length - remainLength;
                int available = socket.Available;
                if (available == 0)//it finished
                {
                    return;
                }
                int expectedReceive = (available < bufferRemain ? available : bufferRemain);

                int realReceive = socket.Receive(buffer, remainLength, expectedReceive, SocketFlags.None);
                int newBufferLength = remainLength + realReceive;//sometime realReceive != expectedReceive
                parser.LoadNewBuffer(buffer, newBufferLength);
                dbugConsole.WriteLine("CheckBeforeParseHeader : LoadNewBuffer");
            }
            //return buffer;
        }

        //static string BindValues(string sql, CommandParameters values)
        //{
        //    if (values == null)
        //    {
        //        return sql;
        //    }
        //    return ParseAndBindValues(sql, values);
        //}

        //static string ParseAndBindValues(string sql, CommandParameters prepare)
        //{
        //    //TODO: implement prepared query string
        //    int length = sql.Length;
        //    ParseState state = ParseState.FIND_MARKER;
        //    char ch;
        //    StringBuilder strBuilder = new StringBuilder();
        //    List<string> list = new List<string>();
        //    string temp;
        //    for (int i = 0; i < length; i++)
        //    {
        //        ch = sql[i];
        //        switch (state)
        //        {
        //            case ParseState.FIND_MARKER:
        //                if (ch == '?')
        //                {
        //                    list.Add(strBuilder.ToString());
        //                    strBuilder.Length = 0;

        //                    state = ParseState.GET_KEY;
        //                }
        //                else
        //                {
        //                    strBuilder.Append(ch);
        //                }
        //                break;
        //            case ParseState.GET_KEY:
        //                if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9')
        //                {
        //                    strBuilder.Append(ch);
        //                }
        //                else
        //                {
        //                    temp = prepare.GetValue(strBuilder.ToString());
        //                    list.Add(temp);
        //                    strBuilder.Length = 0;//clear
        //                    state = ParseState.FIND_MARKER;
        //                    strBuilder.Append(ch);
        //                }
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        //    temp = strBuilder.ToString();
        //    if (state == ParseState.GET_KEY)
        //    {
        //        temp = prepare.GetValue(temp);
        //    }
        //    list.Add(temp);
        //    return GetSql(list);
        //}

        //static string GetSql(List<string> list)
        //{
        //    int length = list.Count;
        //    StringBuilder strBuilder = new StringBuilder();
        //    for (int i = 0; i < length; i++)
        //    {
        //        strBuilder.Append(list[i]);
        //    }
        //    return strBuilder.ToString();
        //}

    }


    //class CommandParameters
    //{
    //    Dictionary<string, string> prepareValues;

    //    public CommandParameters()
    //    {
    //        prepareValues = new Dictionary<string, string>();
    //    }
    //    public void AddTable(string key, string value)
    //    {
    //        prepareValues[key] = string.Concat("`", value, "`");
    //    }

    //    public void AddField(string key, string value)
    //    {
    //        prepareValues[key] = string.Concat("`", value, "`");
    //    }

    //    public void AddValue(string key, string value)
    //    {
    //        prepareValues[key] = string.Concat("`", value, "`");
    //    }

    //    public void AddValue(string key, decimal value)
    //    {
    //        prepareValues[key] = value.ToString();
    //    }
    //    public void AddValue(string key, int value)
    //    {

    //        prepareValues[key] = value.ToString();
    //    }

    //    public void AddValue(string key, long value)
    //    {
    //        prepareValues[key] = value.ToString();
    //    }

    //    public void AddValue(string key, byte value)
    //    {
    //        prepareValues[key] = Encoding.ASCII.GetString(new byte[] { value });
    //    }

    //    public void AddValue(string key, byte[] value)
    //    {
    //        prepareValues[key] = ConvertByteArrayToHexWithMySqlPrefix(value);
    //        // string.Concat("0x", ByteArrayToHexViaLookup32(value));
    //    }

    //    public void AddValue(string key, DateTime value)
    //    {
    //        prepareValues[key] = value.ToString();
    //    }
    //    public string GetValue(string key)
    //    {
    //        string value;
    //        prepareValues.TryGetValue(key, out value);
    //        return value;
    //    } 
    //}




    public class CommandParams
    {
        Dictionary<string, MyStructData> prepareValues;
        Dictionary<string, string> fieldValues;
        MyStructData reuseData;


        public CommandParams()
        {
            prepareValues = new Dictionary<string, MyStructData>();
            fieldValues = new Dictionary<string, string>();
            reuseData = new MyStructData();
            reuseData.type = Types.NULL;

        }

        public CommandParams(string sql)
        {
            prepareValues = new Dictionary<string, MyStructData>();
            fieldValues = new Dictionary<string, string>();
            reuseData = new MyStructData();
            reuseData.type = Types.NULL;
        }

        public void AddTable(string key, string tablename)
        {
            key = "?" + key;
            fieldValues[key] = "`" + tablename + "`";
        }
        public void AddField(string key, string fieldname)
        {
            key = "?" + key;
            fieldValues[key] = "`" + fieldname + "`";
        }
        public void AddValue(string key, string value)
        {
            if (value != null)
            {
                reuseData.myString = value;
                reuseData.type = Types.VAR_STRING;
            }
            else
            {
                reuseData.myString = null;
                reuseData.type = Types.NULL;
            }
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, byte value)
        {
            reuseData.myByte = value;
            reuseData.type = Types.BIT;
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, int value)
        {
            reuseData.myInt32 = value;
            reuseData.type = Types.LONG;//Types.LONG = int32
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, long value)
        {
            reuseData.myInt64 = value;
            reuseData.type = Types.LONGLONG;
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, float value)
        {
            reuseData.myFloat = value;
            reuseData.type = Types.FLOAT;
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, double value)
        {
            reuseData.myDouble = value;
            reuseData.type = Types.DOUBLE;
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, decimal value)
        {
            reuseData.myDecimal = value;
            reuseData.type = Types.DECIMAL;
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, byte[] value)
        {
            if (value != null)
            {
                reuseData.myBuffer = value;
                reuseData.type = Types.LONG_BLOB;
            }
            else
            {
                reuseData.myBuffer = null;
                reuseData.type = Types.NULL;
            }
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, DateTime value)
        {
            reuseData.myDateTime = value;
            reuseData.type = Types.DATETIME;
            AddKeyWithReuseData(key);
        }
        void AddKeyWithReuseData(string key)
        {
            key = "?" + key;
            prepareValues[key] = reuseData;
        }

        internal MyStructData GetData(string key)
        {
            MyStructData value = new MyStructData();
            string temp;
            if (prepareValues.TryGetValue(key, out value))
            {
                return value;
            }
            else if (fieldValues.TryGetValue(key, out temp))
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
            if (prepareValues.TryGetValue(key, out value))
            {
                return null;
            }
            else if (fieldValues.TryGetValue(key, out temp))
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
            return prepareValues.ContainsKey(key);

        }
    }

    class TableHeader
    {
        List<FieldPacket> fields;
        Dictionary<string, int> fieldNamePosMap;

        public TableHeader()
        {
            this.fields = new List<FieldPacket>();
        }

        public void AddField(FieldPacket field)
        {
            fields.Add(field);
        }
        public List<FieldPacket> GetFields()
        {
            return fields;
        }
        public int ColumnCount
        {
            get { return this.fields.Count; }
        }
        public int GetFieldIndex(string fieldName)
        {
            if (fieldNamePosMap == null)
            {
                ///build map index
                int j = fields.Count;
                fieldNamePosMap = new Dictionary<string, int>(j);
                for (int i = 0; i < j; ++i)
                {
                    fieldNamePosMap.Add(fields[i].name, i);
                }
            }
            int found;
            if (!fieldNamePosMap.TryGetValue(fieldName, out found))
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