//LICENSE: MIT
//Copyright(c) 2012 Felix Geisendörfer(felix @debuggable.com) and contributors 
//Copyright(c) 2013 Andrey Sidorov(sidorares @yandex.ru) and contributors
//MIT, 2015-2018, brezza92, EngineKit and contributors

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
namespace SharpConnect.MySql.Internal
{
    struct PacketHeader
    {
        public readonly uint ContentLength;
        public readonly byte PacketNumber;
        public PacketHeader(uint contentLength, byte number)
        {
            //if (contentLength == 5 && number == 0)
            //{ 
            //}
            ContentLength = contentLength;
            PacketNumber = number;
        }

    }

    abstract class Packet
    {
        protected PacketHeader _header;
        public Packet(PacketHeader header)
        {
            //ensure that we have header before parse packet body 
            this._header = header;
        }
        /// <summary>
        /// parse only body part, please ensure content length before parse
        /// </summary>
        /// <param name="r"></param>
        public abstract void ParsePacketContent(MySqlStreamReader r);

        public PacketHeader Header
        {
            get { return _header; }
        }


        public abstract void WritePacket(MySqlStreamWriter writer);
        // The maximum precision JS Numbers can hold precisely
        // Don't panic: Good enough to represent byte values up to 8192 TB
        public const long IEEE_754_BINARY_64_PRECISION = (long)1 << 53;
        public const int MAX_PACKET_LENGTH = (int)(1 << 24) - 1;//(int)Math.Pow(2, 24) - 1;
    }


    public enum MySqlServerStatus
    {
        //
        //https://dev.mysql.com/doc/internals/en/status-flags.html
        //
        SERVER_STATUS_IN_TRANS = 0x0001, //	a transaction is active
        SERVER_STATUS_AUTOCOMMIT = 0x0002, //	auto-commit is enabled
        SERVER_MORE_RESULTS_EXISTS = 0x0008,
        SERVER_STATUS_NO_GOOD_INDEX_USED = 0x0010,
        SERVER_STATUS_NO_INDEX_USED = 0x0020,
        SERVER_STATUS_CURSOR_EXISTS = 0x0040, //	Used by Binary Protocol Resultset to signal that COM_STMT_FETCH must be used to fetch the row-data.
        SERVER_STATUS_LAST_ROW_SENT = 0x0080,
        SERVER_STATUS_DB_DROPPED = 0x0100,
        SERVER_STATUS_NO_BACKSLASH_ESCAPES = 0x0200,
        SERVER_STATUS_METADATA_CHANGED = 0x0400,
        SERVER_QUERY_WAS_SLOW = 0x0800,
        SERVER_PS_OUT_PARAMS = 0x1000,
        SERVER_STATUS_IN_TRANS_READONLY = 0x2000,//	in a read-only transaction
        SERVER_SESSION_STATE_CHANGED = 0x4000,  //connection state information has changed 
    }



    class ClientAuthenticationPacket : Packet
    {
        public uint clientFlags;
        public uint maxPacketSize;
        public byte charsetNumber;
        public string user;
        public byte[] scrambleBuff;
        public string database;
        public bool protocol41;
        public ClientAuthenticationPacket(PacketHeader header)
            : base(header)
        {
            SetDefaultValues();
        }
        void SetDefaultValues()
        {
            clientFlags = (uint)GetDefaultServerCapFlags();
            maxPacketSize = 0;
            charsetNumber = 33;
            user = "";
            scrambleBuff = new byte[20];
            database = "";
            protocol41 = true;
        }
        public void SetValues(string username, byte[] scrambleBuff, string databaseName, bool protocol41)
        {

            clientFlags = (uint)GetDefaultServerCapFlags();
            maxPacketSize = 0;
            charsetNumber = 33;
            this.user = username;
            this.scrambleBuff = scrambleBuff;
            this.database = databaseName;
            this.protocol41 = protocol41;
        }
        ServerCapabilityFlags GetDefaultServerCapFlags()
        {
            //if ((int)serverCap != 455631)
            //{ 
            //}

            //455631
            //note: 455631 = 0b_1101111001111001111  
            return ServerCapabilityFlags.CLIENT_LONG_PASSWORD |
                ServerCapabilityFlags.CLIENT_FOUND_ROWS |
                ServerCapabilityFlags.CLIENT_LONG_FLAG |
                ServerCapabilityFlags.CLIENT_CONNECT_WITH_DB |
                //skip 4,5
                ServerCapabilityFlags.CLIENT_ODBC |
                ServerCapabilityFlags.CLIENT_LOCAL_FILES |//TODO: review remove this if not use
                ServerCapabilityFlags.CLIENT_IGNORE_SPACE |
                ServerCapabilityFlags.CLIENT_PROTOCOL_41 |
                //skip 10,11
                ServerCapabilityFlags.CLIENT_IGNORE_SIGPIPE |
                ServerCapabilityFlags.CLIENT_TRANSACTIONS |
                ServerCapabilityFlags.CLIENT_RESERVED |
                ServerCapabilityFlags.CLIENT_SECURE_CONNECTION |
                //skip 16 
                ServerCapabilityFlags.CLIENT_MULTI_RESULTS |
                ServerCapabilityFlags.CLIENT_PS_MULTI_RESULTS;
        }

        public override void ParsePacketContent(MySqlStreamReader r)
        {

            if (protocol41)
            {
                clientFlags = r.U4(); //4
                maxPacketSize = r.U4(); //4 
                charsetNumber = r.ReadByte();
                r.ReadFiller(23);
                user = r.ReadNullTerminatedString();
                scrambleBuff = r.ReadLengthCodedBuffer();
                database = r.ReadNullTerminatedString();
            }
            else
            {
                clientFlags = r.U2();//2
                maxPacketSize = r.U3();//3
                user = r.ReadNullTerminatedString();
                scrambleBuff = r.ReadBuffer(8);
                database = r.ReadLengthCodedString();
            }
        }

        public override void WritePacket(MySqlStreamWriter writer)
        {
            writer.ReserveHeader();//allocate header
            if (protocol41)
            {
                writer.WriteUnsigned4(clientFlags);
                writer.WriteUnsigned4(maxPacketSize);
                writer.WriteUnsigned1(charsetNumber);
                writer.WriteFiller(23);
                writer.WriteNullTerminatedString(user);
                writer.WriteLengthCodedBuffer(scrambleBuff);
                writer.WriteNullTerminatedString(database);
            }
            else
            {
                writer.WriteUnsigned2(clientFlags);
                writer.WriteUnsigned3(maxPacketSize);
                writer.WriteNullTerminatedString(user);
                writer.WriteBuffer(scrambleBuff);
                if (database != null && database.Length > 0)
                {
                    writer.WriteFiller(1);
                    writer.WriteBuffer(Encoding.ASCII.GetBytes(database));
                }
            }
            _header = new PacketHeader(writer.OnlyPacketContentLength, writer.IncrementPacketNumber());
            writer.WriteHeader(_header);
        }


    }

    class ComQueryPacket : Packet
    {
        byte _QUERY_CMD = (byte)Command.QUERY;//0x03
        string _sql;
        public ComQueryPacket(PacketHeader header, string sql)
            : base(header)
        {
            _sql = sql;
        }

        public override void ParsePacketContent(MySqlStreamReader r)
        {

            _QUERY_CMD = r.ReadByte();//1
            _sql = r.ReadPacketTerminatedString();
        }

        public override void WritePacket(MySqlStreamWriter writer)
        {
            Write(writer, this._sql);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static void Write(MySqlStreamWriter writer, string sql)
        {
            //for those who don't want to alloc an new packet
            //just write it into a stream  
            byte[] buffer = writer.GetEncodeBytes(sql.ToCharArray());
            int totalLen = buffer.Length;
            int packetCount = (totalLen / Packet.MAX_PACKET_LENGTH) + 1;

            if (packetCount <= 1)
            {
                writer.ReserveHeader();
                writer.WriteByte((byte)Command.QUERY);
                //check if we can write data in 1 packet or not 
                writer.WriteBinaryString(buffer);
                var header = new PacketHeader(writer.OnlyPacketContentLength, writer.IncrementPacketNumber());
                writer.WriteHeader(header);
            }
            else
            {
                //we need to split to multiple packet

                int currentPacketContentSize = Packet.MAX_PACKET_LENGTH;
                int pos = 0;

                for (int i = 0; i < packetCount; ++i)
                {
                    //write each packet to stream
                    writer.ReserveHeader();
                    if (i == 0)
                    {
                        //first packet
                        writer.WriteByte((byte)Command.QUERY);
                        writer.WriteBinaryString(buffer, pos, currentPacketContentSize - 1);//remove 1 query cmd
                        pos += (currentPacketContentSize - 1);

                    }
                    else if (i == packetCount - 1)
                    {
                        //last packet
                        currentPacketContentSize = totalLen - pos;
                        writer.WriteBinaryString(buffer, pos, currentPacketContentSize);
                    }
                    else
                    {
                        writer.WriteBinaryString(buffer, pos, currentPacketContentSize);
                        pos += (currentPacketContentSize);
                    }

                    //check if we can write data in 1 packet or not                   
                    var header = new PacketHeader((uint)currentPacketContentSize, writer.IncrementPacketNumber());
                    writer.WriteHeader(header);
                }

            }

        }
    }

    class ComQuitPacket : Packet
    {
        //const byte QUIT_CMD = (byte)Command.QUIT;//0x01 
        public ComQuitPacket(PacketHeader header)
            : base(header) { }
        public override void ParsePacketContent(MySqlStreamReader r)
        {
            throw new NotImplementedException();
            //ParsePacketHeader(parser);
            //this.command = parser.ParseByte();
        }
        public override void WritePacket(MySqlStreamWriter writer)
        {
            this._header = Write(writer);
        }
        public static PacketHeader Write(MySqlStreamWriter writer)
        {
            //for those who don't want to alloc an new packet
            //just write it into a stream
            writer.ReserveHeader();
            writer.WriteUnsigned1((byte)Command.QUIT);
            var h = new PacketHeader(writer.OnlyPacketContentLength, writer.IncrementPacketNumber());
            writer.WriteHeader(h);
            return h;
        }
    }

    class ComPrepareStatementPacket : Packet
    {
        //byte PREPARE_CMD = (byte)Command.STMT_PREPARE;
        string _sql;
        public ComPrepareStatementPacket(PacketHeader header, string sql)
            : base(header)
        {
            _sql = sql;
        }

        public override void ParsePacketContent(MySqlStreamReader r)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(MySqlStreamWriter writer)
        {
            this._header = Write(writer, this._sql);
        }
        public static PacketHeader Write(MySqlStreamWriter writer, string sql)
        {
            //for those who don't want to alloc an new packet
            //just write it into a stream
            writer.ReserveHeader();
            writer.WriteByte((byte)Command.STMT_PREPARE);
            writer.WriteString(sql);
            var h = new PacketHeader(writer.OnlyPacketContentLength, writer.IncrementPacketNumber());
            writer.WriteHeader(h);
            return h;
        }
    }

    class ComExecPrepareStmtPacket : Packet
    {

        readonly uint _statementId;
        readonly MyStructData[] _prepareValues;
        public ComExecPrepareStmtPacket(PacketHeader header, uint statementId, MyStructData[] filledValues)
            : base(header)
        {
            _statementId = statementId;
            _prepareValues = filledValues;
        }
        public override void ParsePacketContent(MySqlStreamReader r)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(MySqlStreamWriter writer)
        {
            Write(writer, this._statementId, _prepareValues);
        }
        //----------------------------
        struct TempSingleValueHolder
        {
            public byte[] headerLenBuffer;
            public int encodedLengthBufferWriteIndex;
            public int encodedLengthLen;
            //------------------------------
            public int contentStart;
            public int contentLen;
            public byte[] bufferContent;
            public byte[] generalContent;
            //-------------------------------

            public int round;
            public bool isBufferOrString;
            public int TotalLength
            {
                get { return encodedLengthLen + contentLen; }
            }
            public void Reset()
            {
                this.encodedLengthBufferWriteIndex = 0;
                this.encodedLengthLen = 0;
                this.contentLen = 0;
                this.contentStart = 0;
                this.isBufferOrString = false;
                this.round = 0;
            }
        }
        //----------------------------
        static byte[] GetBytes<T>(T str)
            where T : struct
        {
            int size = System.Runtime.InteropServices.Marshal.SizeOf(str);
            byte[] arr = new byte[size]; 
            unsafe
            {
                fixed (byte* h = &arr[0])
                {
                    System.Runtime.InteropServices.Marshal.StructureToPtr(str, (IntPtr)h, true);
                }
            }
            return arr;
        }
        
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct EncodedDateTime
        {
            public ushort year;
            public byte month, day, hour, minute, second;
            public int second_part;            
        }
         
        static bool WriteValueByType(MySqlStreamWriter writer,
            ref MyStructData dataTemp,
            ref TempSingleValueHolder singleValueHolder)
        {
            //TODO: review here  
            int availableContentSpace = Packet.MAX_PACKET_LENGTH - (int)writer.OnlyPacketContentLength;
            if (singleValueHolder.round == 0)
            {
                //---------------------------
                //this is first round *** 
                //---------------------------
                switch (dataTemp.type)
                {
                    case MySqlDataType.NULL:
                        //we not write null data
                        return true;
                    case MySqlDataType.DATE:
                    case MySqlDataType.DATETIME:                    
                        {
                            EncodedDateTime mysqlDtm = new EncodedDateTime(); 

                            DateTime dtm = dataTemp.myDateTime;
                            mysqlDtm.year = (ushort)dtm.Year;
                            mysqlDtm.month = (byte)dtm.Month;
                            mysqlDtm.day = (byte)dtm.Day;
                            //
                            mysqlDtm.hour = (byte)dtm.Hour;
                            mysqlDtm.minute = (byte)dtm.Minute;
                            mysqlDtm.second = (byte)dtm.Second; 
                            singleValueHolder.isBufferOrString = true; 

                            byte[] dtmBuffer = GetBytes(mysqlDtm); 

                            singleValueHolder.bufferContent = dtmBuffer;
                            singleValueHolder.contentLen = dtmBuffer.Length;

                            //generate output
                            singleValueHolder.encodedLengthLen = writer.GenerateEncodeLengthNumber(singleValueHolder.contentLen, singleValueHolder.headerLenBuffer);

                            if (availableContentSpace > singleValueHolder.TotalLength)
                            {
                                //write in first round
                                writer.WriteLengthCodedBuffer(singleValueHolder.bufferContent);
                                return true;
                            } 
                        } 
                        break;
                    case MySqlDataType.BIT:
                    case MySqlDataType.BLOB:
                    case MySqlDataType.MEDIUM_BLOB:
                    case MySqlDataType.LONG_BLOB:

                        //this is first round
                        singleValueHolder.isBufferOrString = true;
                        singleValueHolder.bufferContent = dataTemp.myBuffer;
                        singleValueHolder.contentLen = singleValueHolder.bufferContent.Length;
                        //generate output
                        singleValueHolder.encodedLengthLen = writer.GenerateEncodeLengthNumber(singleValueHolder.contentLen, singleValueHolder.headerLenBuffer);
                        if (availableContentSpace > singleValueHolder.TotalLength)
                        {
                            //write in first round
                            writer.WriteLengthCodedBuffer(singleValueHolder.bufferContent);
                            return true;
                        }
                        //go below 
                        break;
                    case MySqlDataType.VARCHAR:
                    case MySqlDataType.VAR_STRING:
                    case MySqlDataType.STRING:
                        {
                            //this is first round
                            singleValueHolder.isBufferOrString = true;
                            singleValueHolder.bufferContent = Encoding.UTF8.GetBytes(dataTemp.myString);
                            singleValueHolder.contentLen = singleValueHolder.bufferContent.Length;
                            //generate output
                            singleValueHolder.encodedLengthLen = writer.GenerateEncodeLengthNumber(singleValueHolder.contentLen, singleValueHolder.headerLenBuffer);
                            if (availableContentSpace > singleValueHolder.TotalLength)
                            {
                                //write in first round
                                writer.WriteLengthCodedBuffer(singleValueHolder.bufferContent);
                                return true;
                            }
                            //go below 
                        }
                        break;
                    case MySqlDataType.TINY:
                        //if we can write in 1st round 
                        if (availableContentSpace > 1)
                        {
                            writer.WriteUnsignedNumber(1, dataTemp.myUInt32);
                            return true;
                        }
                        else
                        {
                            //go below 
                            MySqlStreamWriter.WriteUnsignedNumber(1, dataTemp.myUInt32, singleValueHolder.generalContent);
                        }
                        break;
                    case MySqlDataType.SHORT:
                        if (availableContentSpace > 2)
                        {
                            writer.WriteUnsignedNumber(2, dataTemp.myUInt32);
                            return true;
                        }
                        else
                        {
                            MySqlStreamWriter.WriteUnsignedNumber(2, dataTemp.myUInt32, singleValueHolder.generalContent);
                        }
                        break;
                    case MySqlDataType.LONG:
                        if (availableContentSpace > 4)
                        {
                            writer.WriteUnsignedNumber(4, dataTemp.myUInt32);
                            return true;
                        }
                        else
                        {
                            MySqlStreamWriter.WriteUnsignedNumber(4, dataTemp.myUInt32, singleValueHolder.generalContent);
                            //go below 
                        }
                        break;
                    case MySqlDataType.LONGLONG:
                        if (availableContentSpace > 8)
                        {
                            writer.WriteInt64(dataTemp.myInt64);
                            return true;
                        }
                        else
                        {
                            //go below 
                            MySqlStreamWriter.Write(dataTemp.myInt64, singleValueHolder.generalContent);
                        }

                        break;
                    case MySqlDataType.FLOAT:
                        if (availableContentSpace > 4)
                        {
                            writer.WriteFloat((float)dataTemp.myDouble);
                            return true;
                        }
                        else
                        {
                            //go below 
                            MySqlStreamWriter.Write((float)dataTemp.myDouble, singleValueHolder.generalContent);
                        }
                        break;
                    case MySqlDataType.DOUBLE:
                        if (availableContentSpace > 8)
                        {
                            writer.WriteDouble(dataTemp.myDouble);
                            return true;
                        }
                        else
                        {
                            //go below 
                            MySqlStreamWriter.Write(dataTemp.myDouble, singleValueHolder.generalContent);
                        }
                        break;
                    default:
                        //TODO: review here
                        throw new NotSupportedException();
                        //writer.WriteLengthCodedNull();
                }
            }
            //----------------------------------------------------------------------------------------------------------
            if (singleValueHolder.isBufferOrString)
            {
                //write encoded len & data
                //check if we write complete header or not   
                if (singleValueHolder.encodedLengthBufferWriteIndex < singleValueHolder.encodedLengthLen)
                {
                    //can we write complete encoded length header here?
                    int remaining = singleValueHolder.encodedLengthLen - singleValueHolder.encodedLengthBufferWriteIndex;
                    if (availableContentSpace > remaining)
                    {
                        for (int i = 0; i < remaining; ++i)
                        {
                            writer.WriteByte(singleValueHolder.headerLenBuffer[singleValueHolder.encodedLengthBufferWriteIndex]);
                            singleValueHolder.encodedLengthBufferWriteIndex++;
                        }
                        availableContentSpace -= remaining;
                    }
                    else if (availableContentSpace == remaining)
                    {
                        //TODO: review here
                        throw new NotSupportedException();
                        //we can write only header ***
                        for (int i = 0; i < remaining; ++i)
                        {
                            writer.WriteByte(singleValueHolder.headerLenBuffer[singleValueHolder.encodedLengthBufferWriteIndex]);
                            singleValueHolder.encodedLengthBufferWriteIndex++;
                        }
                        availableContentSpace -= remaining;
                    }
                    else
                    {
                        //can't complete encoded length here in this step
                        for (int i = 0; i < availableContentSpace; ++i)
                        {
                            writer.WriteByte(singleValueHolder.headerLenBuffer[singleValueHolder.encodedLengthBufferWriteIndex]);
                            singleValueHolder.encodedLengthBufferWriteIndex++;
                        }
                        return false;
                    }
                }
                //---------------
                //write the body
                int remainBodyLen = singleValueHolder.contentLen - singleValueHolder.contentStart;
                if (availableContentSpace > remainBodyLen)
                {
                    //write buffer 
                    //we use bufferContent ****
                    writer.WriteBuffer(singleValueHolder.bufferContent, singleValueHolder.contentStart, remainBodyLen);
                    singleValueHolder.contentStart += remainBodyLen;
                    return true;

                }
                else if (availableContentSpace == remainBodyLen)
                {
                    //TODO: review here
                    throw new NotSupportedException();
                    //this needs last packet with 0 byte content size ***
                    writer.WriteBuffer(singleValueHolder.generalContent, singleValueHolder.contentStart, remainBodyLen);
                    singleValueHolder.contentStart += remainBodyLen;
                    return false;
                }
                else
                {
                    //write buffer 
                    //we use bufferContent ****
                    writer.WriteBuffer(singleValueHolder.bufferContent, singleValueHolder.contentStart, availableContentSpace);
                    singleValueHolder.contentStart += availableContentSpace;
                    return false;
                }
            }
            else
            {
                //---------------
                //write the body
                int remainBodyLen = singleValueHolder.contentLen - singleValueHolder.contentStart;
                if (availableContentSpace > remainBodyLen)
                {
                    //write buffer 
                    //we use generalContent ****
                    writer.WriteBuffer(singleValueHolder.generalContent, singleValueHolder.contentStart, remainBodyLen);
                    singleValueHolder.contentStart += remainBodyLen;
                    return true;
                }
                else if (availableContentSpace == remainBodyLen)
                {

                    //TODO: review here
                    throw new NotSupportedException();
                    //this needs last packet with 0 byte content size ***
                    writer.WriteBuffer(singleValueHolder.generalContent, singleValueHolder.contentStart, remainBodyLen);
                    singleValueHolder.contentStart += remainBodyLen;
                    return false;
                }
                else
                {
                    //write buffer 
                    //we use generalContent ****
                    writer.WriteBuffer(singleValueHolder.generalContent, singleValueHolder.contentStart, availableContentSpace);
                    singleValueHolder.contentStart += availableContentSpace;
                    return false;
                }
            }
        }
        public static void Write(MySqlStreamWriter writer, uint stmtId, MyStructData[] _prepareValues)
        {
            //https://dev.mysql.com/doc/internals/en/com-stmt-execute.html#packet-COM_STMT_EXECUTE
            //COM_STMT_EXECUTE asks the server to execute a prepared statement as identified by stmt-id. 
            //It sends the values for the placeholders of the prepared statement(if it contained any) in Binary Protocol Value form. 
            //---------------
            //The type of each parameter is made up of two bytes:
            //---------------
            //1. the type as in Protocol::ColumnType
            //2. a flag byte which has the highest bit set if the type is unsigned[80]
            //--------------- 

            //for those who don't want to alloc an new packet
            //just write it into a stream
            bool completeCurrentHeader = false;

            writer.ReserveHeader();
            writer.WriteByte((byte)Command.STMT_EXECUTE);
            writer.WriteUnsignedNumber(4, stmtId);
            writer.WriteByte((byte)CursorFlags.CURSOR_TYPE_NO_CURSOR);

            writer.WriteUnsignedNumber(4, 1);//iteration_count, always 1
            //
            //write NULL-bitmap, length: (num-params+7)/8 
            MyStructData[] fillValues = _prepareValues;
            int paramNum = _prepareValues.Length;
            if (paramNum > 0)
            {
                uint bitmap = 0;
                uint bitValue = 1;
                for (int i = 0; i < paramNum; i++)
                {
                    MySqlDataType dataType = _prepareValues[i].type;
                    if (dataType == MySqlDataType.NULL)
                    {
                        bitmap += bitValue;
                    }
                    bitValue <<= 1; //shift to left 1 bit
                    if (bitValue == 256)
                    {
                        //end of 8 bits
                        //just store data
                        writer.WriteUnsigned1(bitmap);
                        bitmap = 0;
                        bitValue = 1;
                    }
                }
                if (bitValue != 1)
                {
                    writer.WriteUnsigned1(bitmap);
                }
            }
            //new-params-bound - flag
            writer.WriteByte(1);
            //-------------------------------------------------------
            //data types
            for (int i = 0; i < paramNum; i++)
            {
                writer.WriteUnsignedNumber(2, (byte)_prepareValues[i].type);
                //TODO: review unsigned value here
                //if this type is unsigned number
                //we must note with flags ****
            }

            //--------------------------------------
            //actual data
            //--------------------------------------
            //stream of data may large than 1 packet ***
            var tempSingleValueHolder = new TempSingleValueHolder();
            tempSingleValueHolder.headerLenBuffer = new byte[9];
            tempSingleValueHolder.generalContent = new byte[16];

            for (int i = 0; i < paramNum; i++)
            {
                //write each value
                bool isComplete = WriteValueByType(writer, ref _prepareValues[i], ref tempSingleValueHolder);
                if (!isComplete)
                {
                    //some value may long more than 1 packet
                    //in that case -> isComplete = false.
                    //and we must move to a new packet number
                    var header = new PacketHeader(writer.OnlyPacketContentLength, writer.IncrementPacketNumber());
                    writer.WriteHeader(header);
                    completeCurrentHeader = true;
                    //-------------
                    //write until complete
                    for (; ; )
                    {

                        //write until complete
                        tempSingleValueHolder.round++;
                        writer.ReserveHeader();
                        completeCurrentHeader = false;
                        isComplete = WriteValueByType(writer, ref _prepareValues[i], ref tempSingleValueHolder);
                        if (!isComplete)
                        {
                            header = new PacketHeader(writer.OnlyPacketContentLength, writer.IncrementPacketNumber());
                            writer.WriteHeader(header);
                            completeCurrentHeader = true;
                        }
                        else
                        {
                            //complete
                            break;
                        }
                    }
                    //-------------
                }
                //reset
                tempSingleValueHolder.Reset();
            }
            //--------------------------------------
            if (!completeCurrentHeader)
            {
                var header = new PacketHeader(writer.OnlyPacketContentLength, writer.IncrementPacketNumber());
                writer.WriteHeader(header);
                completeCurrentHeader = true;
            }
        }
    }

    class ComStmtClosePacket : Packet
    {
        uint _statementId;
        public ComStmtClosePacket(PacketHeader header, uint statementId)
            : base(header)
        {
            _statementId = statementId;
        }
        public override void ParsePacketContent(MySqlStreamReader r)
        {
            throw new NotImplementedException();
        }
        public override void WritePacket(MySqlStreamWriter writer)
        {
            _header = Write(writer, _statementId);
        }
        public static PacketHeader Write(MySqlStreamWriter writer, uint stmtId)
        {
            //for those who don't want to alloc an new packet
            //just write it into a stream
            writer.ReserveHeader();
            writer.WriteByte((byte)Command.STMT_CLOSE);
            writer.WriteUnsigned4(stmtId);
            var h = new PacketHeader(writer.OnlyPacketContentLength, writer.IncrementPacketNumber());
            writer.WriteHeader(h);
            return h;
        }
    }

    class ComStmtResetPacket : Packet
    {
        uint _statementId;
        public ComStmtResetPacket(PacketHeader header, uint statementId)
            : base(header)
        {
            _statementId = statementId;
        }
        public override void ParsePacketContent(MySqlStreamReader r)
        {
            throw new NotImplementedException();
        }
        public override void WritePacket(MySqlStreamWriter writer)
        {
            _header = Write(writer, this._statementId);
        }
        public static PacketHeader Write(MySqlStreamWriter writer, uint stmtId)
        {
            writer.ReserveHeader();
            writer.WriteByte((byte)Command.STMT_RESET);
            writer.WriteUnsigned4(stmtId);
            var _header = new PacketHeader(writer.OnlyPacketContentLength, writer.IncrementPacketNumber());
            writer.WriteHeader(_header);
            return _header;
        }
    }

    class ComStmtSendLongDataPacket : Packet
    {
        //TODO: review here 
        //byte command = (byte)Command.STMT_SEND_LONG_DATA;
        //------------------------------------------------------------------------------------------------------

        //https://dev.mysql.com/doc/internals/en/com-stmt-send-long-data.html
        //COM_STMT_SEND_LONG_DATA sends the data for a column.Repeating to send it, appends the data to the parameter.
        //--------------------
        //No response is sent back to the client.
        //COM_STMT_SEND_LONG_DATA:
        //COM_STMT_SEND_LONG_DATA
        //  direction: client -> server
        //  response: none
        //  payload:
        //    1              [18] COM_STMT_SEND_LONG_DATA
        //    4              statement-id
        //    2              param-id
        //    n              data
        //COM_STMT_SEND_LONG_DATA has to be sent before COM_STMT_EXECUTE.
        //---------------------------------------------------------------------------------------------------------

        uint _statement_id;
        int _param_id;
        MyStructData _data;
        public ComStmtSendLongDataPacket(PacketHeader header, uint statement_id, int param_id, MyStructData data)
            : base(header)
        {
            _statement_id = statement_id;
            _param_id = param_id;
            _data = data;
        }

        public override void ParsePacketContent(MySqlStreamReader r)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(MySqlStreamWriter writer)
        {
            writer.ReserveHeader();
            writer.WriteUnsigned4(_statement_id);
            writer.WriteUnsigned2((uint)_param_id);
            WriteValueByType(writer, _data);
        }

        void WriteValueByType(MySqlStreamWriter writer, MyStructData dataTemp)
        {
            switch (dataTemp.type)
            {
                case MySqlDataType.VARCHAR:
                case MySqlDataType.VAR_STRING:
                case MySqlDataType.STRING:
                    writer.WriteLengthCodedString(dataTemp.myString);
                    break;
                case MySqlDataType.LONG:
                    writer.WriteUnsignedNumber(4, (uint)dataTemp.myInt32);
                    break;
                case MySqlDataType.LONGLONG:
                    writer.WriteInt64(dataTemp.myInt64);
                    break;
                case MySqlDataType.FLOAT:
                    writer.WriteFloat((float)dataTemp.myDouble);
                    break;
                case MySqlDataType.DOUBLE:
                    writer.WriteDouble(dataTemp.myDouble);
                    break;
                case MySqlDataType.BIT:
                case MySqlDataType.BLOB:
                case MySqlDataType.MEDIUM_BLOB:
                case MySqlDataType.LONG_BLOB:
                    writer.WriteLengthCodedBuffer(dataTemp.myBuffer);
                    break;
                default:
                    writer.WriteLengthCodedNull();
                    break;
            }
        }
    }

    class EofPacket : Packet
    {
        public byte fieldCount;
        public uint warningCount;
        public uint serverStatus;
        public bool protocol41;
        public EofPacket(PacketHeader header, bool protocol41)
            : base(header)
        {
            this.protocol41 = protocol41;
        }
        public override void ParsePacketContent(MySqlStreamReader r)
        {
            fieldCount = r.ReadByte();
            if (protocol41)
            {
                warningCount = r.U2();//2
                serverStatus = r.U2();//2
            }
        }
        public override void WritePacket(MySqlStreamWriter writer)
        {
            throw new NotImplementedException();
            //writer.ReserveHeader();//allocate packet header
            //writer.WriteUnsigned1(0xfe);
            //if (protocol41)
            //{
            //    writer.WriteUnsigned2(warningCount);
            //    writer.WriteUnsigned2(serverStatus);
            //}
            //_header = new PacketHeader((uint)writer.Length - 4, writer.IncrementPacketNumber());
            //writer.WriteHeader(_header);//write packet header
        }
    }

    class ErrPacket : Packet
    {
        byte _fieldCount;
        uint _errno;
        char _sqlStateMarker;
        string _sqlState;
        public string message;
        public ErrPacket(PacketHeader header) : base(header) { }
        public override void ParsePacketContent(MySqlStreamReader r)
        {

            _fieldCount = r.ReadByte();
            _errno = r.U2();//2
            if (r.PeekByte() == 0x23)
            {
                _sqlStateMarker = r.ReadChar();
                _sqlState = r.ReadString(5);
            }

            message = r.ReadPacketTerminatedString(); 
        }

        public override void WritePacket(MySqlStreamWriter writer)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return _sqlStateMarker + _sqlState + " " + message;
        }
    }

    /// <summary>
    /// https://dev.mysql.com/doc/internals/en/com-query-response.html#column-definition
    /// </summary>
    class FieldPacket : Packet
    {
        /// <summary>
        /// catalog (always "def")
        /// </summary>
        public string catalog;
        /// <summary>
        /// schema-name
        /// </summary>
        public string schema;
        /// <summary>
        /// vittual table name
        /// </summary>
        public string table;
        /// <summary>
        /// physical table name
        /// </summary>
        public string orgTable;
        /// <summary>
        /// virtual column name
        /// </summary>
        public string name;
        /// <summary>
        /// physical column name
        /// </summary>
        public string orgName;
        /// <summary>
        /// character set (2 byte)
        /// </summary>
        public uint charsetNr;
        /// <summary>
        /// maximum length of field
        /// </summary>
        public uint maxLengthOfField;
        /// <summary>
        /// mysql type (1 byte) in protocol 4
        /// </summary>
        public int columnType;
        public uint flags;
        /// <summary>
        /// decimals (1) -- max shown decimal digits 
        /// 
        /// 0x00 for integers and static strings
        /// 0x1f for dynamic strings, double, float
        /// 0x00 to 0x51 for decimals
        /// </summary>
        public byte maxShownDecimalDigits;
        //decimals and column_length can be used for text-output formatting. 


        public byte[] filler;
        public bool zeroFill;
        public string strDefault;
        public bool protocol41;

        public FieldPacket(PacketHeader header, bool protocol41)
            : base(header)
        {
            this.protocol41 = protocol41;
        }
        internal int FieldIndex { get; set; }
#if DEBUG
        public bool dbugFailure { get; set; }
#endif
        public override void ParsePacketContent(MySqlStreamReader r)
        {

            if (protocol41)
            {
                catalog = r.ReadLengthCodedString();//3,100,101,102,103  (always "def")
                schema = r.ReadLengthCodedString();
                table = r.ReadLengthCodedString();
                orgTable = r.ReadLengthCodedString();
                name = r.ReadLengthCodedString();
                orgName = r.ReadLengthCodedString();

                //next_length (lenenc_int) -- length of the following fields (always 0x0c)  ***
                uint lengthCodedNumber = r.ReadLengthCodedNumber();
                if (lengthCodedNumber != 0x0c)
                {

                    //var err  = new TypeError('Received invalid field length');
                    //err.code = 'PARSER_INVALID_FIELD_LENGTH';
                    //throw err;
#if DEBUG
                    if (lengthCodedNumber == 0)
                    {
                        //error
                        //this package is error packet 
                        //server may send the correct one?
                        dbugFailure = true;
                        return;
                    }
#endif
                    throw new Exception("Received invalid field length");
                }

                charsetNr = r.U2();//2
                maxLengthOfField = r.U4();//4
                columnType = r.ReadByte();//1
                flags = r.U2();//2
                maxShownDecimalDigits = r.ReadByte();
                filler = r.ReadBuffer(2);
                if (filler[0] != 0x0 || filler[1] != 0x0)
                {
                    //var err  = new TypeError('Received invalid filler');
                    //err.code = 'PARSER_INVALID_FILLER';
                    //throw err;
                    throw new Exception("Received invalid filler");
                }
                // parsed flags
                //this.zeroFill = (this.flags & 0x0040 ? true : false);
                zeroFill = ((flags & 0x0040) == 0x0040 ? true : false);
                if (r.ReachedPacketEnd())
                {
                    return;
                }
                //----
                //if command was COM_FIELD_LIST {
                //   lenenc_int length of default- values
                //string[$len]   default values
                //}
                strDefault = r.ReadLengthCodedString();
            }
            else
            {
                table = r.ReadLengthCodedString();
                name = r.ReadLengthCodedString();
                maxLengthOfField = r.ReadUnsigedNumber(r.ReadByte());
                columnType = (int)r.ReadUnsigedNumber(r.ReadByte());
            }
        }

        public override void WritePacket(MySqlStreamWriter writer)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return name;
        }
    }

    class HandshakePacket : Packet
    {
        public uint protocolVersion;
        public string serverVertion;
        public uint threadId;
        public byte[] scrambleBuff1;
        public byte filler1;
        public uint serverCapabilities1;
        public byte serverLanguage;
        public uint serverStatus;
        public bool protocol41;
        public uint serverCapabilities2;
        public byte scrambleLength;
        public byte[] filler2;
        public byte[] scrambleBuff2;
        public byte filler3;
        public string pluginData;
        public HandshakePacket(PacketHeader header)
            : base(header)
        {
        }
        public override void ParsePacketContent(MySqlStreamReader r)
        {
            //we already have header ***
            protocolVersion = r.ReadByte();//1
            serverVertion = r.ReadNullTerminatedString();
            threadId = r.U4();//4
            scrambleBuff1 = r.ReadBuffer(8);
            filler1 = r.ReadByte();
            serverCapabilities1 = r.U2();//2
            serverLanguage = r.ReadByte();
            serverStatus = r.U2();//2
            protocol41 = (serverCapabilities1 & (1 << 9)) > 0;
            if (protocol41)
            {
                serverCapabilities2 = r.U2();
                scrambleLength = r.ReadByte();
                filler2 = r.ReadBuffer(10);
                scrambleBuff2 = r.ReadBuffer(12);
                filler3 = r.ReadByte();
            }
            else
            {
                filler2 = r.ReadBuffer(13);
            }

            if (r.ReadPosition == r.CurrentInputLength)
            {
                return;
            }

            pluginData = r.ReadPacketTerminatedString();
            var last = pluginData.Length - 1;
            if (pluginData[last] == '\0')
            {
                pluginData = pluginData.Substring(0, last);
            }
        }

        public override void WritePacket(MySqlStreamWriter writer)
        {
            throw new NotImplementedException();
            //writer.writeUnsignedNumber(1, this.protocolVersion);
            //writer.writeNullTerminatedString(this.serverVersion);
            //writer.writeUnsignedNumber(4, this.threadId);
            //writer.writeBuffer(this.scrambleBuff1);
            //writer.writeFiller(1);
            //writer.writeUnsignedNumber(2, this.serverCapabilities1);
            //writer.writeUnsignedNumber(1, this.serverLanguage);
            //writer.writeUnsignedNumber(2, this.serverStatus);
            //if (this.protocol41) {
            //  writer.writeUnsignedNumber(2, this.serverCapabilities2);
            //  writer.writeUnsignedNumber(1, this.scrambleLength);
            //  writer.writeFiller(10);
            //}
            //writer.writeNullTerminatedBuffer(this.scrambleBuff2);

            //if (this.pluginData !== undefined) {
            //  writer.writeNullTerminatedString(this.pluginData);
            //}
        }
    }

    class OkPacket : Packet
    {
        uint _fieldCount;
        public uint affectedRows;
        public uint insertId;
        public uint _serverStatus;
        public uint _warningCount;
        public string _message;
        bool _protocol41;
        public OkPacket(PacketHeader header, bool protocol41)
            : base(header)
        {
            _protocol41 = protocol41;
        }

        public override void ParsePacketContent(MySqlStreamReader r)
        {

            _fieldCount = r.ReadByte();
            affectedRows = r.ReadLengthCodedNumber();
            insertId = r.ReadLengthCodedNumber();
            if (_protocol41)
            {
                _serverStatus = r.U2();
                _warningCount = r.U2();
            }

            //TODO: review here again
            //https://dev.mysql.com/doc/internals/en/packet-OK_Packet.html

            _message = r.ReadPacketTerminatedString();
            //var m = this.message.match(/\schanged:\s * (\d +) / i);

            //if (m !== null)
            //{
            //    this.changedRows = parseInt(m[1], 10);
            //}
        }

        public override void WritePacket(MySqlStreamWriter writer)
        {
            throw new NotImplementedException();
        }


    }

    class OkPrepareStmtPacket : Packet
    {
        byte _status;
        public uint statement_id;
        public uint num_columns;
        public uint num_params;
        public uint waring_count;
        public OkPrepareStmtPacket(PacketHeader header)
            : base(header) { }

        public override void ParsePacketContent(MySqlStreamReader r)
        {

            _status = r.ReadByte();//alway 0
            statement_id = r.ReadUnsigedNumber(4);
            num_columns = r.ReadUnsigedNumber(2);
            num_params = r.ReadUnsigedNumber(2);
            r.ReadFiller(1);//reserved_1
            waring_count = r.ReadUnsigedNumber(2);
        }

        public override void WritePacket(MySqlStreamWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    class ResultSetHeaderPacket : Packet
    {
        long _fieldCount;
        uint _extraNumber;
        string _extraStr;
        public ResultSetHeaderPacket(PacketHeader header) : base(header) { }
        public override void ParsePacketContent(MySqlStreamReader r)
        {

            _fieldCount = r.ReadLengthCodedNumber();
            if (r.ReachedPacketEnd())
            {
                return;
            }
            if (_fieldCount == 0)
            {
                _extraStr = r.ReadPacketTerminatedString();
            }
            else
            {
                _extraNumber = r.ReadLengthCodedNumber();
                _extraStr = r.ReadPacketTerminatedString();//null;
            }
        }

        public override void WritePacket(MySqlStreamWriter writer)
        {
            throw new NotImplementedException();
        }
    }



    class DataRowPacket : Packet
    {

        internal byte[] _rowDataBuffer;
        public DataRowPacket(PacketHeader header, byte[] rowDataBuffer)
            : base(header)
        {
            this._rowDataBuffer = rowDataBuffer;
        }
        public override void ParsePacketContent(MySqlStreamReader r)
        {
            //we 've set _rowDataBuffer from ctor
            throw new NotSupportedException();
        }
        public override void WritePacket(MySqlStreamWriter writer)
        {
            throw new NotSupportedException();
        }
    }


#if DEBUG
    struct dbugBufferView
    {
        public readonly byte[] buffer;
        public readonly int start;
        public readonly int length;
        public int viewIndex;
        public dbugBufferView(byte[] buffer, int start, int length)
        {
            this.buffer = buffer;
            this.start = start;
            this.length = length;
            viewIndex = 0;
        }
        public int CheckNoDulpicateBytes()
        {
            //for test byte content in longblob testcase
            byte prevByte = buffer[length - 1];
            for (int i = length - 2; i >= start; --i)
            {
                byte test = buffer[i];
                if (prevByte == test)
                {
                    return i;
                }
                prevByte = test;
            }
            return 0;
        }
        public override string ToString()
        {
            var stbuilder = new StringBuilder();
            if (viewIndex > 10)
            {
                //before view,
                int s = viewIndex - 10;
                if (s < 0)
                {
                    s = 0;
                }
                for (int i = s; i < viewIndex; ++i)
                {
                    stbuilder.Append(buffer[i] + ", ");
                }
                stbuilder.Append(" {" + viewIndex + ":" + buffer[viewIndex] + "} ");
                //after view index
                int e = viewIndex + 10;
                if (e > length)
                {
                    e = length;
                }
                for (int i = viewIndex + 1; i < e; ++i)
                {
                    stbuilder.Append(buffer[i] + ", ");
                }
            }
            else
            {
                if (length > 10)
                {
                    stbuilder.Append("s:[");
                    for (int i = 0; i < 10; ++i)
                    {
                        stbuilder.Append(buffer[i] + ", ");
                    }

                    stbuilder.Append("]");
                    //show last 10                
                    stbuilder.Append(" end:[");
                    for (int i = length - 10; i < length; ++i)
                    {
                        stbuilder.Append(buffer[i] + ", ");
                    }
                    stbuilder.Append(']');
                }
            }
            return stbuilder.ToString();
        }
    }
#endif

}