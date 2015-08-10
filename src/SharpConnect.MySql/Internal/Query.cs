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
        Connection conn;

        public bool typeCast;
        public bool nestTables;

        TableHeader tableHeader;
        public ErrPacket loadError;
        public OkPacket okPacket;
        public int index;

        RowDataPacket lastRow;
        bool hasSomeRow;

        PacketParser parser;
        PacketWriter writer;


        byte[] receiveBuffer;
        const int DEFAULT_BUFFER_SIZE = 512;
        const byte ERROR_CODE = 255;
        const byte EOF_CODE = 0xfe;
        const byte OK_CODE = 0;

        long MAX_ALLOWED_SEND = 0;



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


        public void SetMaxSend(long max)
        {
            MAX_ALLOWED_SEND = max;
        }


        public void ExecuteQuery()
        {
            //send query
            SendQuery(sql);

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
                case OK_CODE:
                    okPacket = new OkPacket(conn.IsProtocol41);
                    okPacket.ParsePacket(parser);
                    break;
                default:
                    ParseResultSet();
                    break;
            }
        }

        void SendQuery(string sql)
        {
            writer.Reset();
            ComQueryPacket queryPacket = new ComQueryPacket(sql);
            queryPacket.WritePacket(writer);
            byte[] qr = writer.ToArray();
            int sent = 0;
            //if send data more than max_allowed_packet in mysql server it will be close connection

            var socket = conn.socket;
            if (MAX_ALLOWED_SEND > 0 && qr.Length > MAX_ALLOWED_SEND)
            {
                int packs = (int)Math.Floor(qr.Length / (double)MAX_ALLOWED_SEND) + 1;
                for (int pack = 0; pack < packs; pack++)
                {
                    sent = socket.Send(qr, (int)MAX_ALLOWED_SEND, SocketFlags.None);
                }
            }
            else
            {
                sent = socket.Send(qr, qr.Length, SocketFlags.None);
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
                FieldPacket fieldPacket = new FieldPacket(protocol41);
                fieldPacket.ParsePacketHeader(parser);
                receiveBuffer = CheckLimit(fieldPacket.GetPacketLength(), receiveBuffer, DEFAULT_BUFFER_SIZE);
                fieldPacket.ParsePacket(parser);
                tableHeader.AddField(fieldPacket);
                receiveBuffer = CheckBeforeParseHeader(receiveBuffer, (int)parser.Position, DEFAULT_BUFFER_SIZE);
            }



            EofPacket fieldEof = new EofPacket(protocol41);//if temp[4]=0xfe then eof packet
            fieldEof.ParsePacketHeader(parser);
            receiveBuffer = CheckLimit(fieldEof.GetPacketLength(), receiveBuffer, DEFAULT_BUFFER_SIZE);
            fieldEof.ParsePacket(parser);


            receiveBuffer = CheckBeforeParseHeader(receiveBuffer, (int)parser.Position, DEFAULT_BUFFER_SIZE);

            //-----
            lastRow = new RowDataPacket(tableHeader);

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
                        EofPacket rowDataEof = new EofPacket(conn.IsProtocol41);
                        rowDataEof.ParsePacketHeader(parser);
                        receiveBuffer = CheckLimit(rowDataEof.GetPacketLength(), receiveBuffer, DEFAULT_BUFFER_SIZE);
                        rowDataEof.ParsePacket(parser);


                        return hasSomeRow = false;
                    }
                default:
                    {
                        dbugConsole.WriteLine("Before parse [Position] : " + parser.Position);

                        lastRow.ReuseSlots();
                        lastRow.ParsePacketHeader(parser);

                        dbugConsole.WriteLine("After parse header [Position] : " + parser.Position);

                        receiveBuffer = CheckLimit(lastRow.GetPacketLength(), receiveBuffer, DEFAULT_BUFFER_SIZE);
                        lastRow.ParsePacket(parser);

                        receiveBuffer = CheckBeforeParseHeader(receiveBuffer, (int)parser.Position, DEFAULT_BUFFER_SIZE);
                        dbugConsole.WriteLine("After parse Row [Position] : " + parser.Position);


                        return hasSomeRow = true;
                    }
            }
        }

        public MyStructData GetFieldData(string fieldName)
        {
            return lastRow.GetDataInField(fieldName);
        }
        public MyStructData GetFieldData(int fieldIndex)
        {
            return lastRow.GetDataInField(fieldIndex);
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
                SendQuery(sql);
                conn.ClearRemainingInputBuffer();
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

        void Disconnect()
        {
            writer.Reset();
            ComQuitPacket quitPacket = new ComQuitPacket();
            quitPacket.WritePacket(writer);
            var socket = conn.socket;
            int send = socket.Send(writer.ToArray());
        }

        byte[] CheckLimit(uint packetLength, byte[] buffer, int limit)
        {
            int remainLength = (int)(parser.Length - parser.Position);
            if (packetLength > remainLength)
            {
                //TODO: review here, use buffer pool ?
                byte[] remainBuff = CopyBufferBlock(buffer, (int)parser.Position, remainLength);
                byte[] receiveBuff;
                int packetRemainLength = (int)packetLength - remainLength;
                if (packetRemainLength > limit)
                {
                    receiveBuff = new byte[packetRemainLength];
                }
                else
                {
                    receiveBuff = new byte[limit];
                }
                int newBufferLength = receiveBuff.Length + remainLength;
                if (newBufferLength > buffer.Length)
                {
                    buffer = new byte[newBufferLength];
                }
                remainBuff.CopyTo(buffer, 0);
                var socket = conn.socket;
                int newReceive = socket.Receive(receiveBuff);
                if (newReceive < newBufferLength)//get some but not complete
                {
                    int newIndex = 0;
                    byte[] temp = new byte[newReceive];
                    temp = CopyBufferBlock(receiveBuff, 0, newReceive);
                    temp.CopyTo(buffer, remainLength);
                    newIndex = newReceive + remainLength;
                    while (newIndex < newBufferLength)
                    {

                        //TODO: review here, 
                        //use AsyncSocket

                        Thread.Sleep(100);
                        var s = socket.Available;
                        if (s == 0)
                        {
                            break;
                        }
                        newReceive = socket.Receive(receiveBuff);
                        if (newReceive > 0)
                        {
                            if (newReceive + newIndex + remainLength > newBufferLength)
                            {
                                temp = new byte[newReceive];
                                temp = CopyBufferBlock(receiveBuff, 0, newReceive);
                                byte[] bytes;// = new byte[newReceive + newIndex];
                                bytes = CopyBufferBlock(buffer, 0, newIndex);
                                buffer = new byte[newReceive + newIndex];
                                bytes.CopyTo(buffer, 0);
                                temp.CopyTo(buffer, newIndex);
                                newIndex += newReceive;
                            }
                            else
                            {
                                temp = new byte[newReceive];
                                temp = CopyBufferBlock(receiveBuff, 0, newReceive);
                                temp.CopyTo(buffer, remainLength + newIndex);
                                newIndex += newReceive;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    newBufferLength = newIndex;
                }
                else
                {
                    receiveBuff.CopyTo(buffer, remainLength);
                }
                parser.LoadNewBuffer(buffer, newBufferLength);
            }
            return buffer;
        }

        byte[] CopyBufferBlock(byte[] inputBuffer, int start, int length)
        {

            byte[] outputBuff = new byte[length];
            Buffer.BlockCopy(inputBuffer, start, outputBuff, 0, length);
            return outputBuff;
            //for (int index = 0; index < length; index++)
            //{
            //    outputBuff[index] = inputBuffer[start + index];
            //}
            //return outputBuff;
        }

        byte[] CheckBeforeParseHeader(byte[] buffer, int position, int limit)
        {
            int remainLength = (int)parser.Length - position;
            if (remainLength < 5)//5 bytes --> 4 bytes from header and 1 byte for find packet type
            {
                byte[] remainBuff = CopyBufferBlock(buffer, position, remainLength);
                byte[] receiveBuff = new byte[limit];
                var socket = conn.socket;
                int newReceive = socket.Receive(receiveBuff);
                int newBufferLength = newReceive + remainLength;
                if (newBufferLength > buffer.Length)
                {
                    buffer = new byte[newBufferLength];
                }
                remainBuff.CopyTo(buffer, 0);
                receiveBuff.CopyTo(buffer, remainLength);
                //buffer = remainBuff.Concat(receiveBuff).ToArray();
                parser.LoadNewBuffer(buffer, newReceive + remainLength);

                dbugConsole.WriteLine("CheckBeforeParseHeader : LoadNewBuffer");
            }
            return buffer;
        }

        static string BindValues(string sql, CommandParameters values)
        {
            if (values == null)
            {
                return sql;
            }
            return ParseAndBindValues(sql, values);
        }

        enum ParseState
        {
            FIND_MARKER,
            GET_KEY
        }

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