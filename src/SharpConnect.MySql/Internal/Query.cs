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
        public string sql;
        
        CommandParams command;
        Connection conn;

        public bool typeCast;
        public bool nestTables;

        TableHeader tableHeader;
        public ErrPacket loadError;
        public OkPacket okPacket;
        public int index;

        RowDataPacket lastRow;
        RowPrepaqreDataPacket lastPrepareRow;

        bool IsPrepare;
        bool hasSomeRow;

        PacketParser parser;
        PacketWriter writer;
        
        byte[] receiveBuffer;
        const int DEFAULT_BUFFER_SIZE = 512;
        const byte ERROR_CODE = 255;
        const byte EOF_CODE = 0xfe;
        const byte OK_CODE = 0;
        
        const int MAX_PACKET_LENGTH = (1 << 24) - 1;//(int)Math.Pow(2, 24) - 1;

        public Query(Connection connecion)//testing
        {
            this.conn = connecion;
            typeCast = connecion.config.typeCast;
            nestTables = false;

            index = 0;
            loadError = null;

            //*** query use conn resource such as parser,writer
            //so 1 query 1 connection
            parser = connecion.PacketParser;
            writer = connecion.PacketWriter;
            
            receiveBuffer = null;
        }

        public Query(Connection conn, string sql)
        {
            this.conn = conn;
            this.sql = sql;
            //this.values = values;
            typeCast = conn.config.typeCast;
            nestTables = false;

            index = 0;
            loadError = null;

            //*** query use conn resource such as parser,writer
            //so 1 query 1 connection
            parser = conn.PacketParser;
            writer = conn.PacketWriter;

            this.receiveBuffer = null;

        }

        public Query(Connection conn, CommandParams command)//testing
        {
            this.conn = conn;
            sql = command.SQL;
            this.command = command;

            typeCast = conn.config.typeCast;
            nestTables = false;

            index = 0;
            loadError = null;

            //*** query use conn resource such as parser,writer
            //so 1 query 1 connection
            parser = conn.PacketParser;
            writer = conn.PacketWriter;
            
            receiveBuffer = null;
        }

        public void ExecuteQuerySql(string sql)
        {
            //send query packet
            if(sql != null)
            {
                writer.Reset();
                ComQueryPacket queryPacket = new ComQueryPacket(sql);
                queryPacket.WritePacket(writer);
                SendPacket(writer.ToArray());

                IsPrepare = false;
                ParseReceivePacket();
            }
            else
            {
                throw new Exception("Error : Sql can not null.");
            }
        }

        //public void ExecuteQuery(string sql, CommandParameters cmdParams)//testing
        //{
        //    if (sql != null)
        //    {
        //        this.sql = sql;
        //        this.sql = BindValues(sql, cmdParams);
        //        ExecuteQuerySql();
        //    }
        //    else
        //    {

        //    }
        //}

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

        //public bool PrepareQuery(string prepareSql, int paramsNum)
        //{
        //    string sql = prepareSql;
        //    if (paramsNum == 0)
        //    {
        //        this.sql = sql;
        //        ExecuteQuerySql();
        //        return false;
        //    }
        //    writer.Reset();
        //    ComPrepareStatementPacket preparePacket = new ComPrepareStatementPacket(sql);
        //    preparePacket.WritePacket(writer);
        //    SendPacket(writer.ToArray());

        //    okPreparePacket = new OkPrepareStmtPacket();
        //    okPreparePacket = ParsePrepareResponse();
        //    if (okPreparePacket != null)
        //    {
        //        if (okPreparePacket.num_params > 0)
        //        {
        //            FieldPacket[] fields = new FieldPacket[okPreparePacket.num_params];
        //            for (int i = 0; i < okPreparePacket.num_params; i++)
        //            {
        //                fields[i] = ParseColumn();
        //            }
        //            ParseEOF();
        //        }
        //        if (okPreparePacket.num_columns > 0)
        //        {
        //            tableHeader = new TableHeader();
        //            tableHeader.TypeCast = typeCast;
        //            tableHeader.NestTables = nestTables;
        //            tableHeader.ConnConfig = conn.config;

        //            for (int i = 0; i < okPreparePacket.num_columns; i++)
        //            {
        //                FieldPacket field = ParseColumn();
        //                tableHeader.AddField(field);
        //            }
        //            ParseEOF();
        //        }
        //        return true;
        //    }
        //    return false;
        //}

        public void ExecutePrepareQuery(string sql)
        {
            if (sql != null)
            {
                ExecuteQuerySql(sql);
            }
        }

        public void ExecutePrepareQuery(CommandParams values)
        {
            if (values == null)
            {
                return;
            }
            sql = values.SQL;
            if (values.KeysCount == 0)
            {
                ExecuteQuerySql(sql);
                return;
            }
            writer.Reset();
            ComPrepareStatementPacket preparePacket = new ComPrepareStatementPacket(sql);
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

                writer.Reset();
                ComExcutePrepareStatement excute = new ComExcutePrepareStatement(okPreparePacket.statement_id, values);
                excute.WritePacket(writer);
                SendPacket(writer.ToArray());
                IsPrepare = true;
                ParseReceivePacket();
                if (okPacket != null || loadError != null)
                {
                    return;
                }
                lastPrepareRow = new RowPrepaqreDataPacket(tableHeader);
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
                        if (IsPrepare)
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
        
        internal MyStructData[] Cells
        {
            get
            {
                if (IsPrepare)
                {
                    return lastPrepareRow.Cells;
                }
                else
                {
                    return lastRow.Cells;
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
                sql = "KILL " + conn.threadId;
                //sql = "FLUSH QUERY CACHE;";
                Connection killConn = new Connection(conn.config);
                killConn.Connect();
                killConn.CreateQuery().ExecuteQuerySql(sql);
                conn.ClearRemainingInputBuffer();
                conn = new Connection(conn.config);
                conn.Connect();
                killConn.Disconnect();
            }
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

    enum ParseState
    {
        FIND_MARKER,
        GET_KEY
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



    //    //-------------------------------------------------------
    //    //convert byte array to binary
    //    //from http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa/24343727#24343727
    //    static readonly uint[] _lookup32 = CreateLookup32();

    //    static uint[] CreateLookup32()
    //    {
    //        var result = new uint[256];
    //        for (int i = 0; i < 256; i++)
    //        {
    //            string s = i.ToString("X2");
    //            result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
    //        }
    //        return result;
    //    }

    //    static string ConvertByteArrayToHexWithMySqlPrefix(byte[] bytes)
    //    {
    //        //for mysql only !, 
    //        //we prefix with 0x

    //        var lookup32 = _lookup32;
    //        int j = bytes.Length;
    //        var result = new char[(j * 2) + 2];

    //        int m = 0;
    //        result[0] = '0';
    //        result[1] = 'x';
    //        m = 2;

    //        for (int i = 0; i < j; i++)
    //        {
    //            uint val = lookup32[bytes[i]];
    //            result[m] = (char)val;
    //            result[m + 1] = (char)(val >> 16);
    //            m += 2;
    //        }

    //        return new string(result);
    //    }
    //}

    class CommandParams
    {
        Dictionary<string, MyStructData> prepareValues;
        Dictionary<string, string> fieldValues;
        List<string> keys;//all keys
        List<string> valueKeys;

        List<string> sqlSection;
        MyStructData reuseData;

        string lastSql;
        bool hasUpdate;
        bool key_finded;
        public string SQL { get{ return lastSql = hasUpdate ? CombindAndReplace() : lastSql; } }
        public int KeysCount { get {
                FindValueKeys();
                return valueKeys.Count;
            }
        }

        public CommandParams(string sql)
        {
            prepareValues = new Dictionary<string, MyStructData>();
            fieldValues = new Dictionary<string, string>();
            reuseData = new MyStructData();
            reuseData.type = Types.NULL;

            sqlSection = new List<string>();
            valueKeys = new List<string>();
            keys = new List<string>();

            if (sql != null)
            {
                ParseSQL(sql);
                hasUpdate = true;
                key_finded = false;
            }
            else
            {
                throw new Exception("Error : Sql can not null");
            }
        }

        void FindValueKeys()
        {
            if (key_finded)
            {
                return;
            }
            string temp;
            for(int index = 0; index < keys.Count; index++)
            {
                if(!fieldValues.TryGetValue(keys[index], out temp))
                {
                    valueKeys.Add(keys[index]);
                }
            }
            key_finded = true;
        }
        string CombindAndReplace()
        {
            StringBuilder strBuilder = new StringBuilder();
            int count = sqlSection.Count;
            string temp;
            for (int i = 0; i < count; i++)
            {
                if(sqlSection[i][0] == '?')
                {
                    if(fieldValues.TryGetValue(sqlSection[i], out temp))
                    {
                        strBuilder.Append(temp);
                    }
                    else
                    {
                        strBuilder.Append('?');
                    }
                }
                else
                {
                    strBuilder.Append(sqlSection[i]);
                }
            }
            hasUpdate = false;
            return strBuilder.ToString();
        }

        void ParseSQL(string sql)
        {
            int length = sql.Length;
            ParseState state = ParseState.FIND_MARKER;
            char ch;

            StringBuilder strBuilder = new StringBuilder();
            string temp;
            for(int i = 0; i < length; i++)
            {
                ch = sql[i];
                switch (state)
                {
                    case ParseState.FIND_MARKER:
                        if (ch == '?')
                        {
                            temp = strBuilder.ToString();
                            sqlSection.Add(temp);
                            strBuilder.Length = 0;
                            state = ParseState.GET_KEY;
                            //continue;
                        }
                        strBuilder.Append(ch);
                        break;
                    case ParseState.GET_KEY:
                        if((ch>='a'&&ch<='z')|| (ch >= 'A' && ch <= 'Z')|| (ch >= '0' && ch <= '9'))
                        {
                            strBuilder.Append(ch);
                        }
                        else
                        {
                            temp = strBuilder.ToString();
                            sqlSection.Add(temp);
                            keys.Add(temp);
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
            if(state== ParseState.GET_KEY)
            {
                keys.Add(temp);
            }
            sqlSection.Add(temp);
        }//end method
        bool HasKey(string key)
        {
            for(int i = 0; i < keys.Count; i++)
            {
                if (keys[i].Equals(key))
                {
                    return true;
                }
            }
            return false;
        }
        public void AddTable(string key, string tablename)
        {
            key = "?" + key;
            if (!HasKey(key))
            {
                throw new Exception("Not have key '" + key + "' in Sql string.");
            }
            fieldValues[key] = "`"+tablename+"`";
        }
        public void AddField(string key, string fieldname)
        {
            key = "?" + key;
            if (!HasKey(key))
            {
                throw new Exception("Not have key '" + key + "' in Sql string.");
            }
            fieldValues[key] = "`"+fieldname+"`";
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
            if (!HasKey(key))
            {
                throw new Exception("Not have key '" + key + "' in Sql string.");
            }
            hasUpdate = true;
            prepareValues[key] = reuseData;
        }

        public MyStructData GetData(string key)
        {
            MyStructData value = new MyStructData();
            string temp;
            if( prepareValues.TryGetValue(key, out value))
            {
                return value;
            }
            else if (fieldValues.TryGetValue(key, out temp))
            {
                throw new Exception("Error : This key is key of table or field. Please use key of value and try again.");
            }
            else
            {
                throw new Exception("Error : Key not found '" + key + "' or value not assigned. Please re-check and try again.");
            }
        }
        public List<string> GetValuesKeys()
        {
            FindValueKeys();
            return valueKeys;
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