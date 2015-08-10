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
            writer.Rewrite();
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
            writer.Rewrite();
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
                            strBuilder.Clear();

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
                            strBuilder.Clear();
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



    class PacketParser
    {
        BinaryReader reader;
        MemoryStream stream;
        int myLength;
        long startPosition;
        long packetLength;
        Encoding encoding = Encoding.UTF8;



        public PacketParser(Encoding encoding)
        {
            this.encoding = encoding;
            stream = new MemoryStream();
            startPosition = stream.Position;//stream.Position = 0;
            reader = new BinaryReader(stream, encoding);
        }

        ~PacketParser()
        {
            Dispose();
        }
        public long Position
        {
            get { return stream.Position; }

        }
        public long Length
        {
            get
            {
                return myLength;
            }
        }

        public void Dispose()
        {
            reader.Close();
            stream.Close();
            stream.Dispose();
        }

        public void Reparse()
        {
            stream.Position = 0;
            myLength = 0;
        }

        public void LoadNewBuffer(byte[] newBuffer, int count)
        {
            Reparse();
            stream.Write(newBuffer, 0, count);
            stream.Position = 0;
            startPosition = 0;
            myLength = count;
        }

        public string ParseNullTerminatedString()
        {
            List<byte> bList = new List<byte>();
            byte temp = reader.ReadByte();
            bList.Add(temp);
            while (temp != 0)
            {
                temp = reader.ReadByte();
                bList.Add(temp);
            }
            byte[] bytes = bList.ToArray();
            return encoding.GetString(bytes);
        }

        public byte[] ParseNullTerminatedBuffer()
        {
            List<byte> list = new List<byte>();
            var temp = reader.ReadByte();
            list.Add(temp);
            while (temp != 0x00)
            {
                temp = reader.ReadByte();
                list.Add(temp);
            }
            return list.ToArray();
        }

        public byte ParseByte()
        {
            return reader.ReadByte();
        }

        public byte[] ParseBuffer(int n)
        {
            if (n > 0)
                return reader.ReadBytes(n);
            else
                return null;
        }

        public PacketHeader ParsePacketHeader()
        {
            startPosition = stream.Position;
            PacketHeader header = new PacketHeader(ParseUnsignedNumber(3), ParseByte());
            packetLength = header.Length + 4;
            return header;
        }

        public string ParseLengthCodedString()
        {
            //var length = this.parseLengthCodedNumber();
            uint length = ParseLengthCodedNumber();
            //if (length === null) {
            //  return null;
            //}
            return ParseString(length);
            //return this.parseString(length);
        }

        public byte[] ParseLengthCodedBuffer()
        {
            //var length = this.parseLengthCodedNumber();
            uint length = ParseLengthCodedNumber();
            //  if (length === null) {
            //    return null;
            //  }
            return ParseBuffer((int)length);
            //  return this.parseBuffer(length);
        }

        public byte[] ParseFiller(int length)
        {
            return ParseBuffer(length);
        }

        public uint ParseLengthCodedNumber()
        {
            //if (this._offset >= this._buffer.length)
            //    {
            //        var err = new Error('Parser: read past end');
            //        err.offset = (this._offset - this._packetOffset);
            //        err.code = 'PARSER_READ_PAST_END';
            //        throw err;
            //    }
            if (Position >= Length)
            {
                throw new Exception("Parser: read past end");
            }
            //    var bits = this._buffer[this._offset++];

            byte bits = reader.ReadByte();

            //    if (bits <= 250)
            //    {
            //        return bits;
            //    }

            if (bits <= 250)
            {
                return bits;
            }
            //    switch (bits)
            //    {
            //        case 251:
            //            return null;
            //        case 252:
            //            return this.parseUnsignedNumber(2);
            //        case 253:
            //            return this.parseUnsignedNumber(3);
            //        case 254:
            //            break;
            //        default:
            //            var err = new Error('Unexpected first byte' + (bits ? ': 0x' + bits.toString(16) : ''));
            //            err.offset = (this._offset - this._packetOffset - 1);
            //            err.code = 'PARSER_BAD_LENGTH_BYTE';
            //            throw err;
            //    }

            switch (bits)
            {
                case 251: return 0;
                case 252: return this.ParseUnsignedNumber(2);
                case 253: return this.ParseUnsignedNumber(3);
                case 254: break;
                default: throw new Exception("Unexpected first byte");
            }
            //    var low = this.parseUnsignedNumber(4);
            //    var high = this.parseUnsignedNumber(4);
            //    var value;
            uint low = this.ParseUnsignedNumber(4);
            uint high = this.ParseUnsignedNumber(4);
            return 0;
            //    if (high >>> 21)
            //    {
            //        value = (new BigNumber(low)).plus((new BigNumber(MUL_32BIT)).times(high)).toString();

            //        if (this._supportBigNumbers)
            //        {
            //            return value;
            //        }

            //        var err = new Error(
            //          'parseLengthCodedNumber: JS precision range exceeded, ' +
            //          'number is >= 53 bit: "' + value + '"'
            //        );
            //        err.offset = (this._offset - this._packetOffset - 8);
            //        err.code = 'PARSER_JS_PRECISION_RANGE_EXCEEDED';
            //        throw err;
            //    }

            //    value = low + (MUL_32BIT * high);

            //    return value;
        }

        public uint ParseUnsignedNumber(int n)
        {
            //if (bytes === 1)
            //{
            //    return this._buffer[this._offset++];
            //}
            if (n == 1)
            {
                return reader.ReadByte();
            }
            //var buffer = this._buffer;
            //var offset = this._offset + bytes - 1;
            //var value = 0;

            //if (bytes > 4)
            //{
            //    var err = new Error('parseUnsignedNumber: Supports only up to 4 bytes');
            //    err.offset = (this._offset - this._packetOffset - 1);
            //    err.code = 'PARSER_UNSIGNED_TOO_LONG';
            //    throw err;
            //}
            if (n > 4)
            {
                throw new Exception("parseUnsignedNumber: Supports only up to 4 bytes");
            }

            long start = Position;
            long end = start + n - 1;

            //while (offset >= this._offset)
            //{
            //    value = ((value << 8) | buffer[offset]) >>> 0;
            //    offset--;
            //}
            byte[] temp = new byte[n];
            uint value = 0;
            for (int i = n - 1; i >= 0; i--)
            {
                temp[i] = reader.ReadByte();
                value = temp[i];
            }
            for (int i = 0; i < n; i++)
            {
                value = value | temp[i];
                if (i < n - 1)
                    value = value << 8;
            }

            //this._offset += bytes;
            //return value;
            return value;
        }

        public string ParsePacketTerminatedString()
        {
            long distance = Length - Position;
            if (distance > 0)
            {
                return new string(reader.ReadChars((int)distance));
            }
            else
            {
                return null;
            }
        }

        public char ParseChar()
        {
            return reader.ReadChar();
        }

        public string ParseString(uint length)
        {
            return encoding.GetString(reader.ReadBytes((int)length));
        }

        public List<Geometry> ParseGeometryValue()
        {
            //var buffer = this.parseLengthCodedBuffer();
            //var offset = 4;
            byte[] buffer = ParseLengthCodedBuffer();
            int offset = 4;
            //if (buffer === null || !buffer.length) {
            //  return null;
            //}
            if (buffer == null)
            {
                return null;
            }

            List<Geometry> result = new List<Geometry>();
            int byteOrder = buffer[offset++];
            int wkbType = byteOrder != 0 ? ReadInt32LE(buffer, offset) : ReadInt32BE(buffer, offset);
            offset += 4;
            //function parseGeometry() {
            //  var result = null;
            //  var byteOrder = buffer.readUInt8(offset); offset += 1;
            //  var wkbType = byteOrder? buffer.readUInt32LE(offset) : buffer.readUInt32BE(offset); offset += 4;

            //return parseGeometry();
            ParseGeometry(result, buffer, byteOrder, wkbType, offset);

            return result;
        }

        void ParseGeometry(List<Geometry> result, byte[] buffer, int byteOrder, int wkbType, int offset)
        {
            double x;
            double y;
            int numPoints;
            Geometry value = new Geometry();
            switch (wkbType)
            {
                case 1:// WKBPoint
                    x = byteOrder != 0 ? ReadDoubleLE(buffer, offset) : ReadDoubleBE(buffer, offset);
                    offset += 8;
                    y = byteOrder != 0 ? ReadDoubleLE(buffer, offset) : ReadDoubleBE(buffer, offset);
                    offset += 8;
                    value.SetValue(x, y);
                    result.Add(value);
                    break;
                //      var x = byteOrder? buffer.readDoubleLE(offset) : buffer.readDoubleBE(offset); offset += 8;
                //      var y = byteOrder? buffer.readDoubleLE(offset) : buffer.readDoubleBE(offset); offset += 8;
                //      result = {x: x, y: y};
                //      break;
                case 2:// WKBLineString
                    numPoints = byteOrder != 0 ? ReadInt32LE(buffer, offset) : ReadInt32BE(buffer, offset);
                    offset += 4;
                    for (int i = numPoints; i > 0; i--)
                    {
                        x = byteOrder != 0 ? ReadDoubleLE(buffer, offset) : ReadDoubleBE(buffer, offset);
                        offset += 8;
                        y = byteOrder != 0 ? ReadDoubleLE(buffer, offset) : ReadDoubleBE(buffer, offset);
                        offset += 8;
                        value.SetValue(x, y);
                        result.Add(value);
                    }
                    break;
                //      var numPoints = byteOrder? buffer.readUInt32LE(offset) : buffer.readUInt32BE(offset); offset += 4;
                //      result = [];
                //      for(var i=numPoints;i>0;i--) {
                //        var x = byteOrder? buffer.readDoubleLE(offset) : buffer.readDoubleBE(offset); offset += 8;
                //        var y = byteOrder? buffer.readDoubleLE(offset) : buffer.readDoubleBE(offset); offset += 8;
                //        result.push({x: x, y: y});
                //      }
                //      break;
                case 3:// WKBPolygon
                    int numRings = byteOrder != 0 ? ReadInt32LE(buffer, offset) : ReadInt32BE(buffer, offset);
                    offset += 4;

                    for (int i = numRings; i > 0; i--)
                    {
                        numPoints = byteOrder != 0 ? ReadInt32LE(buffer, offset) : ReadInt32BE(buffer, offset);
                        offset += 4;
                        List<Geometry> lines = new List<Geometry>();
                        for (int j = numPoints; i > 0; j--)
                        {
                            x = byteOrder != 0 ? ReadDoubleLE(buffer, offset) : ReadDoubleBE(buffer, offset);
                            offset += 8;
                            y = byteOrder != 0 ? ReadDoubleLE(buffer, offset) : ReadDoubleBE(buffer, offset);
                            offset += 8;
                            lines.Add(new Geometry(x, y));
                        }
                        value.AddChildValues(lines);
                        result.Add(value);
                    }
                    break;
                //      var numRings = byteOrder? buffer.readUInt32LE(offset) : buffer.readUInt32BE(offset); offset += 4;
                //      result = [];
                //      for(var i=numRings;i>0;i--) {
                //        var numPoints = byteOrder? buffer.readUInt32LE(offset) : buffer.readUInt32BE(offset); offset += 4;
                //        var line = [];
                //        for(var j=numPoints;j>0;j--) {
                //          var x = byteOrder? buffer.readDoubleLE(offset) : buffer.readDoubleBE(offset); offset += 8;
                //          var y = byteOrder? buffer.readDoubleLE(offset) : buffer.readDoubleBE(offset); offset += 8;
                //          line.push({x: x, y: y});
                //        }
                //        result.push(line);
                //      }
                //      break;
                case 4:// WKBMultiPoint
                case 5:// WKBMultiLineString
                case 6:// WKBMultiPolygon
                case 7:// WKBGeometryCollection
                    int num = byteOrder != 0 ? ReadInt32LE(buffer, offset) : ReadInt32BE(buffer, offset);
                    offset += 4;
                    for (int i = num; i > 0; i--)
                    {
                        ParseGeometry(result, buffer, byteOrder, wkbType, offset);
                    }
                    //var num = byteOrder? buffer.readUInt32LE(offset) : buffer.readUInt32BE(offset); offset += 4;
                    //      var result = [];
                    //      for(var i=num;i>0;i--) {
                    //        result.push(parseGeometry());
                    //      }
                    break;
                    //return reult;
            }
        }

        int ReadInt32LE(byte[] buffer, int start)
        {
            //byte[] temp = new byte[n];
            //uint value = 0;
            //for (int i = n - 1; i >= 0; i--)
            //{
            //    temp[i] = reader.ReadByte();
            //    value = temp[i];
            //}
            //for (int i = 0; i < n; i++)
            //{
            //    value = value | temp[i];
            //    if (i < n - 1)
            //        value = value << 8;
            //}

            return 0;
        }

        int ReadInt32BE(byte[] buffer, int start)
        {
            return 0;
        }

        double ReadDoubleLE(byte[] buffer, int start)
        {
            return 0;
        }

        double ReadDoubleBE(byte[] buffer, int start)
        {
            return 0;
        }

        public int Peak()
        {
            return reader.PeekChar();
        }

        public bool ReachedPacketEnd()
        {
            return this.Position == startPosition + packetLength;
        }

        public byte[] ToArray()
        {
            return stream.ToArray();
        }
    }



    


    class PacketWriter
    {
        MyBinaryWriter writer;
        byte packetNumber;
        long startPacketPosition;

        const int BIT_16 = (int)1 << 16;//(int)Math.Pow(2, 16);
        const int BIT_24 = (int)1 << 24;//(int)Math.Pow(2, 24);
        // The maximum precision JS Numbers can hold precisely
        // Don't panic: Good enough to represent byte values up to 8192 TB
        const long IEEE_754_BINARY_64_PRECISION = (long)1 << 53;
        const int MAX_PACKET_LENGTH = (int)(1 << 24) - 1;//(int)Math.Pow(2, 24) - 1;

        long maxAllowedLength = MAX_PACKET_LENGTH;
        Encoding encoding = Encoding.UTF8;

        public PacketWriter(Encoding encoding)
        {
            writer = new MyBinaryWriter();
            writer.Reset();
            packetNumber = 0;
            startPacketPosition = 0;
            this.encoding = encoding;
        }

        ~PacketWriter()
        {
            Dispose();
        }
        public long Position
        {
            get { return writer.OriginalStreamPosition; }
        }
        public long Length
        {
            get { return writer.Length; }
        }
        public void SetMaxAllowedPacket(long max)
        {
            maxAllowedLength = max;
        }

        public void Rewrite()
        {
            packetNumber = 0;
            startPacketPosition = 0;
            this.writer.Reset();
        }

        public void Dispose()
        {
            writer.Close();
        }

        public void ReserveHeader()
        {
            startPacketPosition = writer.OriginalStreamPosition;
            WriteFiller(4);
        }

        public byte IncrementPacketNumber()
        {
            return packetNumber++;
        }

        public void WriteHeader(PacketHeader header)
        {
            //  var packets  = Math.floor(this._buffer.length / MAX_PACKET_LENGTH) + 1;
            //  var buffer   = this._buffer;
            int MAX = MAX_PACKET_LENGTH;
            if (maxAllowedLength <= MAX_PACKET_LENGTH)
            {
                MAX = (int)maxAllowedLength - 4;//-4 bytes for header
            }
            long curPacketLength = CurrentPacketLength();

            dbugConsole.WriteLine("Current Packet Length = " + curPacketLength);

            int packets = (int)Math.Floor((decimal)(curPacketLength / MAX)) + 1;
            if (packets > 1)
            {
                //  this._buffer = new Buffer(this._buffer.length + packets * 4);
                //  for (var packet = 0; packet < packets; packet++) {


                //  }
                int startContentPos = (int)(startPacketPosition + 4);
                int offset = 0;
                byte startPacketNum = header.PacketNumber;
                byte[] currentPacketBuff = new byte[MAX];
                byte[] allBuffer = new byte[(curPacketLength - 4) + (packets * 4)];
                for (int packet = 0; packet < packets; packet++)
                {
                    //    this._offset = packet * (MAX_PACKET_LENGTH + 4);
                    offset = packet * MAX + startContentPos;
                    //    var isLast = (packet + 1 === packets);
                    //    var packetLength = (isLast)
                    //      ? buffer.length % MAX_PACKET_LENGTH
                    //      : MAX_PACKET_LENGTH;
                    int packetLength = (packet + 1 == packets)
                        ? (int)((curPacketLength - 4) % MAX)
                        : MAX;
                    //    var packetNumber = parser.incrementPacketNumber();

                    //    this.writeUnsignedNumber(3, packetLength);
                    //    this.writeUnsignedNumber(1, packetNumber);

                    //    var start = packet * MAX_PACKET_LENGTH;
                    //    var end   = start + packetLength;

                    //    this.writeBuffer(buffer.slice(start, end));
                    var start = packet * (MAX + 4);

                    byte[] encodeData = new byte[4];
                    EncodeUnsignedNumber(encodeData, 0, 3, (uint)packetLength);
                    encodeData[3] = startPacketNum;
                    encodeData.CopyTo(allBuffer, start);
                    writer.RewindWriteAtOffset(encodeData, (int)start);
                    startPacketNum = 0;
                    if (packetLength < currentPacketBuff.Length)
                    {
                        currentPacketBuff = new byte[packetLength];
                    }
                    writer.Read(currentPacketBuff, offset, packetLength);
                    currentPacketBuff.CopyTo(allBuffer, start + 4);
                }
                writer.RewindWriteAtOffset(allBuffer, (int)startPacketPosition);
            }
            else
            {
                byte[] encodeData = new byte[4];
                EncodeUnsignedNumber(encodeData, 0, 3, header.Length);
                encodeData[3] = header.PacketNumber;
                writer.RewindWriteAtOffset(encodeData, (int)startPacketPosition);
            }
        }

        public long CurrentPacketLength()
        {
            return writer.OriginalStreamPosition - startPacketPosition;
        }

        byte[] CurrentPacketToArray(int length)
        {
            byte[] buffer = new byte[length];
            writer.Read(buffer, (int)startPacketPosition, length);
            return buffer;
        }

        public void WriteNullTerminatedString(string str)
        {
            byte[] buff = encoding.GetBytes(str.ToCharArray());
            writer.Write(buff);
            writer.Write((byte)0);
        }

        public void WriteNullTerminatedBuffer(byte[] value)
        {
            WriteBuffer(value);
            WriteFiller(1);
        }

        public void WriteUnsignedNumber(int length, uint value)
        {
            byte[] tempBuff = new byte[length];
            for (var i = 0; i < length; i++)
            {
                tempBuff[i] = (byte)((value >> (i * 8)) & 0xff);
            }
            writer.Write(tempBuff);
        }

        void EncodeUnsignedNumber(byte[] outputBuffer, int start, int length, uint value)
        {
            int lim = start + length;
            for (var i = start; i < lim; i++)
            {
                outputBuffer[i] = (byte)((value >> (i * 8)) & 0xff);
            }
        }

        public void WriteByte(byte value)
        {
            writer.Write(value);

        }

        public void WriteFiller(int length)
        {
            byte[] filler = new byte[length];
            writer.Write(filler);
        }

        public void WriteBuffer(byte[] value)
        {
            writer.Write(value);
        }

        public void WriteLengthCodedNumber(long? value)
        {
            if (value == null)
            {
                writer.Write((byte)251);

                return;
            }

            if (value <= 250)
            {
                writer.Write((byte)value);

                return;
            }

            if (value > IEEE_754_BINARY_64_PRECISION)
            {
                throw new Exception("writeLengthCodedNumber: JS precision range exceeded, your" +
                  "number is > 53 bit: " + value);
            }

            if (value <= BIT_16)
            {
                //this._allocate(3)
                //this._buffer[this._offset++] = 252;
                writer.Write((byte)252);

            }
            else if (value <= BIT_24)
            {
                //this._allocate(4)
                //this._buffer[this._offset++] = 253;
                writer.Write((byte)253);

            }
            else
            {
                //this._allocate(9);
                //this._buffer[this._offset++] = 254;
                writer.Write((byte)254);

            }

            //// 16 Bit
            //this._buffer[this._offset++] = value & 0xff;
            //this._buffer[this._offset++] = (value >> 8) & 0xff;
            writer.Write((byte)(value & 0xff));

            writer.Write((byte)((value >> 8) & 0xff));


            if (value <= BIT_16) return;

            //// 24 Bit
            //this._buffer[this._offset++] = (value >> 16) & 0xff;
            writer.Write((byte)((value >> 16) & 0xff));


            if (value <= BIT_24) return;

            //this._buffer[this._offset++] = (value >> 24) & 0xff;
            writer.Write((byte)((value >> 24) & 0xff));


            //// Hack: Get the most significant 32 bit (JS bitwise operators are 32 bit)
            //value = value.toString(2);
            //value = value.substr(0, value.length - 32);
            //value = parseInt(value, 2);

            //this._buffer[this._offset++] = value & 0xff;
            //this._buffer[this._offset++] = (value >> 8) & 0xff;
            //this._buffer[this._offset++] = (value >> 16) & 0xff;
            writer.Write((byte)((value >> 32) & 0xff));
            writer.Write((byte)((value >> 40) & 0xff));
            writer.Write((byte)((value >> 48) & 0xff));

            //// Set last byte to 0, as we can only support 53 bits in JS (see above)
            //this._buffer[this._offset++] = 0;
            writer.Write((byte)0);
        }

        public void WriteLengthCodedBuffer(byte[] value)
        {
            var bytes = value.Length;
            WriteLengthCodedNumber(bytes);
            writer.Write(value);
        }

        public void WriteLengthCodedString(string value)
        {
            //          if (value === null) {
            //  this.writeLengthCodedNumber(null);
            //  return;
            //}
            if (value == null)
            {
                WriteLengthCodedNumber(null);
                return;
            }
            //value = (value === undefined)
            //  ? ''
            //  : String(value);

            //var bytes = Buffer.byteLength(value, 'utf-8');
            //this.writeLengthCodedNumber(bytes);
            byte[] buff = Encoding.UTF8.GetBytes(value);
            WriteLengthCodedNumber(buff.Length);
            //if (!bytes) {
            //  return;
            //}
            if (buff == null)
            {
                return;
            }
            //this._allocate(bytes);
            //this._buffer.write(value, this._offset, 'utf-8');
            //this._offset += bytes;
            writer.Write(buff);
        }

        public void WriteString(string value)
        {
            byte[] buff = encoding.GetBytes(value.ToCharArray());
            writer.Write(buff);
        }

        public byte[] ToArray()
        {
            writer.Flush();
            return writer.ToArray();
        }
    }

    struct PacketHeader
    {
        public readonly uint Length;
        public readonly byte PacketNumber;

        public PacketHeader(uint length, byte number)
        {
            Length = length;
            PacketNumber = number;
        }
        public bool IsEmpty()
        {
            return PacketNumber == 0 && Length == 0;
        }
        public static readonly PacketHeader Empty = new PacketHeader();
    }

    class MyBinaryWriter : IDisposable
    {
        readonly BinaryWriter writer;
        int offset;
        MemoryStream ms;


        public MyBinaryWriter()
        {
            ms = new MemoryStream();
            writer = new BinaryWriter(ms);
        }
        public int Length
        {
            get { return this.offset; }
        }
        public void Dispose()
        {
            this.Close();
        }
        public void Write(byte b)
        {
            writer.Write(b);
            offset++;
        }
        public void Write(byte[] bytes)
        {
            writer.Write(bytes);
            offset += bytes.Length;
        }
        public void Write(char[] chars)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(chars);
            Write(bytes);
        }
        public void Reset()
        {
            writer.BaseStream.Position = 0;
            offset = 0;
        }
        public void RewindWriteAtOffset(byte[] buffer, int offset)
        {
            var pos = writer.BaseStream.Position;
            writer.BaseStream.Position = offset;
            writer.Write(buffer);
            writer.BaseStream.Position = pos;

            if (this.offset < buffer.Length)
            {
                this.offset = buffer.Length;
            }
        }
        public long OriginalStreamPosition
        {
            get { return this.writer.BaseStream.Position; }
            set { this.writer.BaseStream.Position = value; }
        }
        public void Close()
        {
            writer.Close();
            ms.Close();
            ms.Dispose();
        }
        public void Flush()
        {
            writer.Flush();
        }
        public byte[] ToArray()
        {
            byte[] output = new byte[offset];
            ms.Position = 0;
            Read(output, 0, offset);
            return output;
        }
        public void Read(byte[] buffer, int offset, int count)
        {
            ms.Position = offset;
            var a = ms.Read(buffer, 0, count);
        }
    }
}