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
        CommandParameters values;
        CommandParam2 command;
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

        public Query(Connection conn, string sql, CommandParameters values)
        {
            this.conn = conn;
            this.sql = sql;
            this.values = values;
            typeCast = conn.config.typeCast;
            nestTables = false;

            index = 0;
            loadError = null;

            //*** query use conn resource such as parser,writer
            //so 1 query 1 connection
            parser = conn.PacketParser;
            writer = conn.PacketWriter;

            this.sql = BindValues(sql, values);
            this.receiveBuffer = null;

        }

        public Query(Connection conn, CommandParam2 command)//testing
        {
            this.conn = conn;
            sql = command.SQL;
            this.command = command;
            values = null;

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

        public void ExecuteQuery()
        {
            //send query packet
            writer.Reset();
            ComQueryPacket queryPacket = new ComQueryPacket(sql);
            queryPacket.WritePacket(writer);
            SendPacket(writer.ToArray());

            IsPrepare = false;
            ParseReceivePacket();
        }

        public void ExecuteQuery(string sql, CommandParameters cmdParams)//testing
        {
            this.sql = sql;
            this.sql = BindValues(sql, cmdParams);
            ExecuteQuery();
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
        
        public void ExecutePrepareQuery(CommandParam2 values)
        {
            string sql = values.SQL;
            if (values.KeysCount == 0)
            {
                this.sql = sql;
                ExecuteQuery();
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
                    for (int i = 0; i < okPreparePacket.num_params; i++)
                    {
                        FieldPacket field = ParseColumn();
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
        int binfile = 2;
        void SendPacket(byte[] packetBuffer)
        {
            //writer.Reset();
            //ComQueryPacket queryPacket = new ComQueryPacket(sql);
            //queryPacket.WritePacket(writer);
            //byte[] packetBuffer = writer.ToArray();
            int sent = 0;
            //if send data more than max_allowed_packet in mysql server it will be close connection
            int packetLength = packetBuffer.Length;
            var socket = conn.socket;
            File.WriteAllBytes("D:/[]TestDir/CSharpPacketBin_"+binfile+".test", packetBuffer);
            binfile++;
            while (sent < packetLength)
            {
                sent += socket.Send(packetBuffer, sent, packetLength - sent, SocketFlags.None);
            }
            //if (packetLength > MAX_PACKET_LENGTH)
            //{
            //    int packs = (int)Math.Floor(packetBuffer.Length / (double)MAX_PACKET_LENGTH) + 1;
            //    for (int pack = 0; pack < packs; pack++)
            //    {
            //        //TODO: not sure >> waiting to test
            //        //sent = socket.Send(packetBuffer, MAX_PACKET_LENGTH, SocketFlags.None);
            //        sent = socket.Send(packetBuffer, sent, MAX_PACKET_LENGTH, SocketFlags.None);
            //    }
            //}
            //else
            //{
            //    while (sent < packetLength)
            //    {
            //        sent += socket.Send(packetBuffer, sent, packetLength - sent, SocketFlags.None);
            //    }
            //}
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
            //sql = "RESET QUERY CACHE";//not work
            //writer.Rewrite();
            //ComQuitPacket quitPacket = new ComQuitPacket();
            //quitPacket.WritePacket(writer);
            //int send = socket.Send(writer.ToArray());
            //sql = "KILL " + threadId;
            //Connection connection = new Connection(config);
            //connection.Connect();
            //Query q = connection.CreateQuery(sql, null);
            //sql = "RESET QUERY CACHE";
            //sql = SqlFormat(sql, null);
            //SendQuery(sql);
            //Console.WriteLine("sql : '" + sql + "'");
            //sql = "KILL " + threadId;
            //sql = SqlFormat(sql, null);
            //SendQuery(sql);
            //Console.WriteLine("sql : '" + sql + "'");
            //Thread.Sleep(1000);
            //socket.Disconnect(false);
            //this.Disconnect(); 
            //TODO: review here !
            if (hasSomeRow)
            {
                sql = "KILL " + conn.threadId;
                Connection killConn = new Connection(conn.config);
                killConn.Connect();
                killConn.CreateQuery(sql, null).ExecuteQuery();
                conn.ClearRemainingInputBuffer();
                killConn.Disconnect();
            }

            //socket = null;
            ////TODO :test
            //int test = 44;
            //if (lastReceive > test)
            //{
            //    byte[] dd = new byte[test];
            //    dd = CopyBufferBlock(temp, lastReceive - test, test);

            //    parser.LoadNewBuffer(dd, test);
            //    loadError = new ErrPacket();
            //    loadError.ParsePacket(parser);
            //}
            //socket.Disconnect(false);

            //Connection newConnect = new Connection(config);
            //newConnect.Connect();
            //socket = newConnect.socket;
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

        //static byte[] CopyBufferBlock(byte[] inputBuffer, int start, int length)
        //{
        //    byte[] outputBuff = new byte[length];
        //    Buffer.BlockCopy(inputBuffer, start, outputBuff, 0, length);
        //    return outputBuff;
        //    //for (int index = 0; index < length; index++)
        //    //{
        //    //    outputBuff[index] = inputBuffer[start + index];
        //    //}
        //    //return outputBuff;
        //}

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

        static string BindValues(string sql, CommandParameters values)
        {
            if (values == null)
            {
                return sql;
            }
            return ParseAndBindValues(sql, values);
        }

        List<string> FindKeysAndReplaceMarker(string sql, List<string> keys)
        {
            int length = sql.Length;
            ParseState state = ParseState.FIND_MARKER;
            char ch;
            StringBuilder strBuilder = new StringBuilder();
            List<string> list = new List<string>();
            string temp;
            for (int i = 0; i < length; i++)
            {
                ch = sql[i];
                switch (state)
                {
                    case ParseState.FIND_MARKER:
                        if (ch == '?')
                        {
                            list.Add(strBuilder.ToString());
                            strBuilder.Length = 0;
                            //strBuilder.Append(ch);
                            state = ParseState.GET_KEY;
                        }
                        else
                        {
                            strBuilder.Append(ch);
                        }
                        break;
                    case ParseState.GET_KEY:
                        if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9')
                        {
                            strBuilder.Append(ch);
                        }
                        else
                        {
                            temp = strBuilder.ToString();
                            keys.Add(temp);//save key by sequence
                            list.Add("?");
                            strBuilder.Length = 0;//clear
                            state = ParseState.FIND_MARKER;
                            strBuilder.Append(ch);
                        }
                        break;
                    default:
                        break;
                }
            }
            temp = strBuilder.ToString();
            if (state == ParseState.GET_KEY)
            {
                keys.Add(temp);
                temp = "?";
            }
            list.Add(temp);
            return list;
        }

        //string FindKeysAndReplaceMarker(List<string> list, List<string> keys)
        //{
        //    StringBuilder builder = new StringBuilder();
        //    int length = list.Count;
        //    for(int i = 0; i < length; i++)
        //    {
        //        if (list[i][0] == '?')
        //        {
        //            builder.Append('?');
        //            keys.Add(list[i]);
        //        }
        //        else
        //        {
        //            builder.Append(list[i]);
        //        }
        //    }
        //    return builder.ToString();
        //}

        static string ParseAndBindValues(string sql, CommandParameters prepare)
        {
            //TODO: implement prepared query string
            int length = sql.Length;
            ParseState state = ParseState.FIND_MARKER;
            char ch;
            StringBuilder strBuilder = new StringBuilder();
            List<string> list = new List<string>();
            string temp;
            for (int i = 0; i < length; i++)
            {
                ch = sql[i];
                switch (state)
                {
                    case ParseState.FIND_MARKER:
                        if (ch == '?')
                        {
                            list.Add(strBuilder.ToString());
                            strBuilder.Length = 0;

                            state = ParseState.GET_KEY;
                        }
                        else
                        {
                            strBuilder.Append(ch);
                        }
                        break;
                    case ParseState.GET_KEY:
                        if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9')
                        {
                            strBuilder.Append(ch);
                        }
                        else
                        {
                            temp = prepare.GetValue(strBuilder.ToString());
                            list.Add(temp);
                            strBuilder.Length = 0;//clear
                            state = ParseState.FIND_MARKER;
                            strBuilder.Append(ch);
                        }
                        break;
                    default:
                        break;
                }
            }
            temp = strBuilder.ToString();
            if (state == ParseState.GET_KEY)
            {
                temp = prepare.GetValue(temp);
            }
            list.Add(temp);
            return GetSql(list);
        }

        static string GetSql(List<string> list)
        {
            int length = list.Count;
            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                strBuilder.Append(list[i]);
            }
            return strBuilder.ToString();
        }

        //public void Start(Socket socket, bool protocol41, ConnectionConfig config)
        //{
        //    this.socket = socket;
        //    this.config = config;
        //    this.protocol41 = protocol41;
        //    writer.Rewrite();
        //    ComQueryPacket queryPacket = new ComQueryPacket(sql);
        //    queryPacket.WritePacket(writer);

        //    byte[] qr = writer.ToArray();
        //    int sent = socket.Send(qr);

        //    byte[] receiveBuffer = new byte[DEFAULT_BUFFER_SIZE];
        //    int receive = socket.Receive(receiveBuffer);

        //    parser.LoadNewBuffer(receiveBuffer, receive);
        //    if (receiveBuffer[4] == ERROR_CODE)
        //    {
        //        loadError = new ErrPacket();
        //        loadError.ParsePacket(parser);
        //    }
        //    else if (receiveBuffer[4] == OK_CODE)
        //    {
        //        okPacket = new OkPacket(protocol41);
        //        okPacket.ParsePacket(parser);
        //    }
        //    else
        //    {
        //        ResultSetHeaderPacket resultPacket = new ResultSetHeaderPacket();
        //        resultPacket.ParsePacket(parser);
        //        resultSet = new ResultSet(resultPacket);

        //        while (receiveBuffer[parser.Position + 4] != EOF_CODE)
        //        {
        //            FieldPacket fieldPacket = new FieldPacket(protocol41);
        //            fieldPacket.ParsePacketHeader(parser);
        //            receiveBuffer = CheckLimit(fieldPacket.GetPacketLength(), receiveBuffer, (int)parser.Length);
        //            fieldPacket.ParsePacket(parser);
        //            resultSet.Add(fieldPacket);

        //            receiveBuffer = CheckBeforeParseHeader(receiveBuffer, (int)parser.Position, (int)parser.Length);
        //        }

        //        EofPacket fieldEof = new EofPacket(protocol41);//if temp[4]=0xfe then eof packet
        //        fieldEof.ParsePacketHeader(parser);
        //        receiveBuffer = CheckLimit(fieldEof.GetPacketLength(), receiveBuffer, (int)parser.Length);
        //        fieldEof.ParsePacket(parser);
        //        resultSet.Add(fieldEof);

        //        receiveBuffer = CheckBeforeParseHeader(receiveBuffer, (int)parser.Position, (int)parser.Length);

        //        var fieldList = resultSet.GetFields();
        //        while (receiveBuffer[parser.Position + 4] != EOF_CODE)
        //        {
        //            RowDataPacket rowData = new RowDataPacket(fieldList, typeCast, nestTables, config);
        //            rowData.ParsePacketHeader(parser);
        //            receiveBuffer = CheckLimit(rowData.GetPacketLength(), receiveBuffer, (int)parser.Length);
        //            rowData.ParsePacket(parser);
        //            resultSet.Add(rowData);

        //            receiveBuffer = CheckBeforeParseHeader(receiveBuffer, (int)parser.Position, (int)parser.Length);
        //        }

        //        EofPacket rowDataEof = new EofPacket(protocol41);
        //        rowDataEof.ParsePacketHeader(parser);
        //        receiveBuffer = CheckLimit(rowDataEof.GetPacketLength(), receiveBuffer, (int)parser.Length);
        //        rowDataEof.ParsePacket(parser);
        //        resultSet.Add(rowDataEof);
        //    }
        //}
    }

    enum ParseState
    {
        FIND_MARKER,
        GET_KEY
    }

    class CommandParameters
    {
        Dictionary<string, string> prepareValues;

        public CommandParameters()
        {
            prepareValues = new Dictionary<string, string>();
        }
        public void AddTable(string key, string value)
        {
            prepareValues[key] = string.Concat("`", value, "`");
        }

        public void AddField(string key, string value)
        {
            prepareValues[key] = string.Concat("`", value, "`");
        }

        public void AddValue(string key, string value)
        {
            prepareValues[key] = string.Concat("`", value, "`");
        }

        public void AddValue(string key, decimal value)
        {
            prepareValues[key] = value.ToString();
        }
        public void AddValue(string key, int value)
        {

            prepareValues[key] = value.ToString();
        }

        public void AddValue(string key, long value)
        {
            prepareValues[key] = value.ToString();
        }

        public void AddValue(string key, byte value)
        {
            prepareValues[key] = Encoding.ASCII.GetString(new byte[] { value });
        }

        public void AddValue(string key, byte[] value)
        {
            prepareValues[key] = ConvertByteArrayToHexWithMySqlPrefix(value);
            // string.Concat("0x", ByteArrayToHexViaLookup32(value));
        }

        public void AddValue(string key, DateTime value)
        {
            prepareValues[key] = value.ToString();
        }
        public string GetValue(string key)
        {
            string value;
            prepareValues.TryGetValue(key, out value);
            return value;
        }



        //-------------------------------------------------------
        //convert byte array to binary
        //from http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa/24343727#24343727
        static readonly uint[] _lookup32 = CreateLookup32();

        static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("X2");
                result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
            }
            return result;
        }

        static string ConvertByteArrayToHexWithMySqlPrefix(byte[] bytes)
        {
            //for mysql only !, 
            //we prefix with 0x

            var lookup32 = _lookup32;
            int j = bytes.Length;
            var result = new char[(j * 2) + 2];

            int m = 0;
            result[0] = '0';
            result[1] = 'x';
            m = 2;

            for (int i = 0; i < j; i++)
            {
                uint val = lookup32[bytes[i]];
                result[m] = (char)val;
                result[m + 1] = (char)(val >> 16);
                m += 2;
            }

            return new string(result);
        }
    }

    class CommandParam2
    {
        Dictionary<string, MyStructData> prepareValues;
        Dictionary<string, string> fieldValues;
        List<string> keys;//all keys
        List<string> valueKeys;

        List<string> sqlSection;
        MyStructData reuseData;

        string lastSql;
        bool hasUpdate;
        public string SQL { get{ return lastSql = hasUpdate ? CombindAndReplace() : lastSql; } }
        public int KeysCount { get { return keys.Count - fieldValues.Count; } }

        public CommandParam2(string sql)
        {
            prepareValues = new Dictionary<string, MyStructData>();
            fieldValues = new Dictionary<string, string>();
            reuseData = new MyStructData();
            reuseData.type = Types.NULL;

            sqlSection = new List<string>();
            valueKeys = new List<string>();
            keys = new List<string>();

            ParseSQL(sql);
            hasUpdate = true;
        }

        void FindValueKeys()
        {
            if(KeysCount == 0 || valueKeys.Count == KeysCount)
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
        }
        string CombindAndReplace()
        {
            StringBuilder strBuilder = new StringBuilder();
            int count = sqlSection.Count;
            string temp;
            for (int i = 0; i < count; i++)
            {
                if(prepareValues.TryGetValue(sqlSection[i], out reuseData))
                {
                    strBuilder.Append('?');
                }
                else if (fieldValues.TryGetValue(sqlSection[i], out temp))
                {
                    strBuilder.Append(temp);
                }
                else
                {
                    if (sqlSection[i][0] == '?')
                    {
                        throw new Exception(sqlSection[i] + " not assign. please assign value and try again.");
                    }
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
            if( prepareValues.TryGetValue(key, out value))
            {
                return value;
            }
            else
            {
                string temp;
                if(fieldValues.TryGetValue(key, out temp))
                {
                    throw new Exception("Error : This key is key of table or field. Please use key of value and try again.");
                }
                throw new Exception("Error : Key not found (" + key + "). Please re-check key and try again.");
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