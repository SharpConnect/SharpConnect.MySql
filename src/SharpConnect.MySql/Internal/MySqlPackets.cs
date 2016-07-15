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
using System.Text;
namespace SharpConnect.MySql.Internal
{
    struct PacketHeader
    {
        public readonly uint ContentLength;
        public readonly byte PacketNumber;
        public PacketHeader(uint length, byte number)
        {
            ContentLength = length;
            PacketNumber = number;
        }
        public bool IsEmpty()
        {
            return PacketNumber == 0 && ContentLength == 0;
        }
        public static readonly PacketHeader Empty = new PacketHeader();
    }

    abstract class Packet
    {
        protected PacketHeader _header;
        public abstract void ParsePacket(MySqlStreamReader r);
        public void ParsePacketHeader(MySqlStreamReader r)
        {
            if (_header.IsEmpty())
            {
                _header = r.ReadPacketHeader();
            }
        }

        public PacketHeader Header
        {
            get { return _header; }
            set
            {
                _header = value;
            }
        }
        public uint GetPacketLength()
        {
            return _header.ContentLength;
        }

        public abstract void WritePacket(MySqlStreamWrtier writer);
        // The maximum precision JS Numbers can hold precisely
        // Don't panic: Good enough to represent byte values up to 8192 TB
        public const long IEEE_754_BINARY_64_PRECISION = (long)1 << 53;
        public const int MAX_PACKET_LENGTH = (int)(1 << 24) - 1;//(int)Math.Pow(2, 24) - 1;
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
        public ClientAuthenticationPacket()
        {
            SetDefaultValues();
        }

        void SetDefaultValues()
        {
            clientFlags = 455631;
            maxPacketSize = 0;
            charsetNumber = 33;
            user = "";
            scrambleBuff = new byte[20];
            database = "";
            protocol41 = true;
        }

        public void SetValues(string username, byte[] scrambleBuff, string databaseName, bool protocol41)
        {
            clientFlags = 455631;
            maxPacketSize = 0;
            charsetNumber = 33;
            this.user = username;
            this.scrambleBuff = scrambleBuff;
            this.database = databaseName;
            this.protocol41 = protocol41;
        }

        public override void ParsePacket(MySqlStreamReader r)
        {
            ParsePacketHeader(r);
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

        public override void WritePacket(MySqlStreamWrtier writer)
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
            _header = new PacketHeader((uint)writer.Length - 4, writer.IncrementPacketNumber());
            writer.WriteHeader(_header);
        }


    }

    class ComQueryPacket : Packet
    {
        byte _QUERY_CMD = (byte)Command.QUERY;//0x03
        string _sql;
        public ComQueryPacket(string sql)
        {
            _sql = sql;
        }

        public override void ParsePacket(MySqlStreamReader r)
        {
            ParsePacketHeader(r);
            _QUERY_CMD = r.U1();//1
            _sql = r.ReadPacketTerminatedString();
        }

        public override void WritePacket(MySqlStreamWrtier writer)
        {
            this._header = Write(writer, this._sql);
        }

        public static PacketHeader Write(MySqlStreamWrtier writer, string sql)
        {
            //for those who don't want to alloc an new packet
            //just write it into a stream
            writer.ReserveHeader();
            writer.WriteByte((byte)Command.QUERY);
            writer.WriteString(sql);
            var header = new PacketHeader((uint)writer.Length - 4, writer.IncrementPacketNumber());
            writer.WriteHeader(header);
            return header;
        }
    }

    class ComQuitPacket : Packet
    {
        //const byte QUIT_CMD = (byte)Command.QUIT;//0x01 
        public override void ParsePacket(MySqlStreamReader r)
        {
            throw new NotImplementedException();
            //ParsePacketHeader(parser);
            //this.command = parser.ParseByte();
        }
        public override void WritePacket(MySqlStreamWrtier writer)
        {
            this._header = Write(writer);
        }
        public static PacketHeader Write(MySqlStreamWrtier writer)
        {
            //for those who don't want to alloc an new packet
            //just write it into a stream
            writer.ReserveHeader();
            writer.WriteUnsigned1((byte)Command.QUIT);
            var h = new PacketHeader((uint)writer.Length, writer.IncrementPacketNumber());
            writer.WriteHeader(h);
            return h;
        }
    }

    class ComPrepareStatementPacket : Packet
    {
        //byte PREPARE_CMD = (byte)Command.STMT_PREPARE;
        string _sql;
        public ComPrepareStatementPacket(string sql)
        {
            _sql = sql;
        }

        public override void ParsePacket(MySqlStreamReader r)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(MySqlStreamWrtier writer)
        {
            this._header = Write(writer, this._sql);
        }
        public static PacketHeader Write(MySqlStreamWrtier writer, string sql)
        {
            //for those who don't want to alloc an new packet
            //just write it into a stream
            writer.ReserveHeader();
            writer.WriteByte((byte)Command.STMT_PREPARE);
            writer.WriteString(sql);
            var h = new PacketHeader((uint)writer.Length - 4, writer.IncrementPacketNumber());
            writer.WriteHeader(h);
            return h;
        }
    }

    class ComExecPrepareStmtPacket : Packet
    {
        //byte EXCUTE_CMD = (byte)Command.STMT_EXECUTE;

        readonly uint _statementId;
        readonly MyStructData[] _prepareValues;
        public ComExecPrepareStmtPacket(uint statementId, MyStructData[] filledValues)
        {
            _statementId = statementId;
            _prepareValues = filledValues;
        }
        public override void ParsePacket(MySqlStreamReader r)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(MySqlStreamWrtier writer)
        {
            Write(writer, this._statementId, _prepareValues);
        }

        static void WriteValueByType(MySqlStreamWrtier writer, ref MyStructData dataTemp)
        {
            switch (dataTemp.type)
            {
                case Types.VARCHAR:
                case Types.VAR_STRING:
                case Types.STRING:
                    writer.WriteLengthCodedString(dataTemp.myString);
                    break;
                case Types.TINY:
                    writer.WriteUnsignedNumber(1, dataTemp.myUInt32);
                    break;
                case Types.SHORT:
                    var a = dataTemp.myInt32;
                    writer.WriteUnsignedNumber(2, dataTemp.myUInt32);
                    break;
                case Types.LONG:
                    //writer.WriteUnsignedNumber(4, (uint)dataTemp.myInt32);
                    writer.WriteUnsignedNumber(4, dataTemp.myUInt32);
                    break;
                case Types.LONGLONG:
                    writer.WriteInt64(dataTemp.myInt64);
                    break;
                case Types.FLOAT:
                    writer.WriteFloat((float)dataTemp.myDouble);
                    break;
                case Types.DOUBLE:
                    writer.WriteDouble(dataTemp.myDouble);
                    break;
                case Types.BIT:
                case Types.BLOB:
                case Types.MEDIUM_BLOB:
                case Types.LONG_BLOB:
                    writer.WriteLengthCodedBuffer(dataTemp.myBuffer);
                    break;
                default:
                    //TODO: review here
                    throw new NotSupportedException();
                    //writer.WriteLengthCodedNull();
            }
        }

        public static PacketHeader Write(MySqlStreamWrtier writer, uint stmtId, MyStructData[] _prepareValues)
        {
            //for those who don't want to alloc an new packet
            //just write it into a stream
            writer.ReserveHeader();
            writer.WriteByte((byte)Command.STMT_EXECUTE);
            writer.WriteUnsignedNumber(4, stmtId);
            writer.WriteByte((byte)CursorFlags.CURSOR_TYPE_NO_CURSOR);
            writer.WriteUnsignedNumber(4, 1);//iteration-count, always 1
            //write NULL-bitmap, length: (num-params+7)/8

            MyStructData[] fillValues = _prepareValues;
            int paramNum = _prepareValues.Length;
            if (paramNum > 0)
            {
                uint bitmap = 0;
                uint bitValue = 1;
                for (int i = 0; i < paramNum; i++)
                {
                    Types dataType = _prepareValues[i].type;
                    if (dataType == Types.NULL)
                    {
                        bitmap += bitValue;
                    }
                    bitValue *= 2;
                    if (bitValue == 256)
                    {
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
            writer.WriteByte(1);//new-params-bound - flag
            //-------------------------------------------------------
            //data types
            for (int i = 0; i < paramNum; i++)
            {
                writer.WriteUnsignedNumber(2, (byte)_prepareValues[i].type);
            }

            //write value of each parameter
            //example:
            //for(int i = 0; i < param.Length; i++)
            //{
            //    switch (param[i].type)
            //    {
            //        case Types.BLOB:writer.WriteLengthCodedBuffer(param[i].myBuffer);
            //    }
            //}

            //--------------------------------------
            //actual data
            for (int i = 0; i < paramNum; i++)
            {
                WriteValueByType(writer, ref _prepareValues[i]);
            }
            var header = new PacketHeader((uint)writer.CurrentPacketLength() - 4, writer.IncrementPacketNumber());
            writer.WriteHeader(header);
            return header;
        }


    }

    class ComStmtClosePacket : Packet
    {
        uint _statementId;
        public ComStmtClosePacket(uint statementId)
        {
            _statementId = statementId;
        }

        public override void ParsePacket(MySqlStreamReader r)
        {
            throw new NotImplementedException();
        }
        public override void WritePacket(MySqlStreamWrtier writer)
        {
            _header = Write(writer, _statementId);
        }
        public static PacketHeader Write(MySqlStreamWrtier writer, uint stmtId)
        {
            //for those who don't want to alloc an new packet
            //just write it into a stream
            writer.ReserveHeader();
            writer.WriteByte((byte)Command.STMT_CLOSE);
            writer.WriteUnsigned4(stmtId);
            var _header = new PacketHeader((uint)writer.CurrentPacketLength() - 4, writer.IncrementPacketNumber());
            writer.WriteHeader(_header);
            return _header;
        }
    }

    class ComStmtResetPacket : Packet
    {
        uint _statementId;
        public ComStmtResetPacket(uint statementId)
        {
            _statementId = statementId;
        }
        public override void ParsePacket(MySqlStreamReader r)
        {
            throw new NotImplementedException();
        }
        public override void WritePacket(MySqlStreamWrtier writer)
        {
            _header = Write(writer, this._statementId);
        }
        public static PacketHeader Write(MySqlStreamWrtier writer, uint stmtId)
        {
            writer.ReserveHeader();
            writer.WriteByte((byte)Command.STMT_RESET);
            writer.WriteUnsigned4(stmtId);
            var _header = new PacketHeader((uint)writer.CurrentPacketLength() - 4, writer.IncrementPacketNumber());
            writer.WriteHeader(_header);
            return _header;
        }
    }

    class ComStmtSendLongDataPacket : Packet
    {
        //TODO: review here 
        //byte command = (byte)Command.STMT_SEND_LONG_DATA;
        uint _statement_id;
        int _param_id;
        MyStructData _data;
        public ComStmtSendLongDataPacket(uint statement_id, int param_id, MyStructData data)
        {
            _statement_id = statement_id;
            _param_id = param_id;
            _data = data;
        }

        public override void ParsePacket(MySqlStreamReader r)
        {
            throw new NotImplementedException();
        }

        public override void WritePacket(MySqlStreamWrtier writer)
        {
            writer.ReserveHeader();
            writer.WriteUnsigned4(_statement_id);
            writer.WriteUnsigned2((uint)_param_id);
            WriteValueByType(writer, _data);
        }

        void WriteValueByType(MySqlStreamWrtier writer, MyStructData dataTemp)
        {
            switch (dataTemp.type)
            {
                case Types.VARCHAR:
                case Types.VAR_STRING:
                case Types.STRING:
                    writer.WriteLengthCodedString(dataTemp.myString);
                    break;
                case Types.LONG:
                    writer.WriteUnsignedNumber(4, (uint)dataTemp.myInt32);
                    break;
                case Types.LONGLONG:
                    writer.WriteInt64(dataTemp.myInt64);
                    break;
                case Types.FLOAT:
                    writer.WriteFloat((float)dataTemp.myDouble);
                    break;
                case Types.DOUBLE:
                    writer.WriteDouble(dataTemp.myDouble);
                    break;
                case Types.BIT:
                case Types.BLOB:
                case Types.MEDIUM_BLOB:
                case Types.LONG_BLOB:
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
        public EofPacket(bool protocol41)
        {
            this.protocol41 = protocol41;
        }

        public override void ParsePacket(MySqlStreamReader r)
        {
            ParsePacketHeader(r);
            fieldCount = r.ReadByte();
            if (protocol41)
            {
                warningCount = r.U2();//2
                serverStatus = r.U2();//2
            }
        }

        public override void WritePacket(MySqlStreamWrtier writer)
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
        public override void ParsePacket(MySqlStreamReader r)
        {
            ParsePacketHeader(r);
            _fieldCount = r.ReadByte();
            _errno = r.U2();//2
            if (r.PeekByte() == 0x23)
            {
                _sqlStateMarker = r.ReadChar();
                _sqlState = r.ReadString(5);
            }

            message = r.ReadPacketTerminatedString();
#if DEBUG
            throw new Exception(_sqlStateMarker + _sqlState + " " + message);
#endif
        }

        public override void WritePacket(MySqlStreamWrtier writer)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return _sqlStateMarker + _sqlState + " " + message;
        }
    }

    class FieldPacket : Packet
    {
        public string catalog;
        public string db;
        public string table;
        public string orgTable;
        public string name;
        public string orgName;
        public uint charsetNr;
        public uint length;
        public int type;
        public uint flags;
        public byte decimals;
        public byte[] filler;
        public bool zeroFill;
        public string strDefault;
        public bool protocol41;
        public FieldPacket(bool protocol41)
        {
            this.protocol41 = protocol41;
        }

        public override void ParsePacket(MySqlStreamReader r)
        {
            ParsePacketHeader(r);
            if (protocol41)
            {
                catalog = r.ReadLengthCodedString();
                db = r.ReadLengthCodedString();
                table = r.ReadLengthCodedString();
                orgTable = r.ReadLengthCodedString();
                name = r.ReadLengthCodedString();
                orgName = r.ReadLengthCodedString();
                if (r.ReadLengthCodedNumber() != 0x0c)
                {
                    //var err  = new TypeError('Received invalid field length');
                    //err.code = 'PARSER_INVALID_FIELD_LENGTH';
                    //throw err;
                    throw new Exception("Received invalid field length");
                }

                charsetNr = r.U2();//2
                length = r.U4();//4
                type = r.ReadByte();
                flags = r.U2();//2
                decimals = r.ReadByte();
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
                //    if (parser.reachedPacketEnd()) {
                //      return;
                //    }
                if (r.ReachedPacketEnd())
                {
                    return;
                }
                strDefault = r.ReadLengthCodedString();
            }
            else
            {
                table = r.ReadLengthCodedString();
                name = r.ReadLengthCodedString();
                length = r.ReadUnsigedNumber(r.ReadByte());
                type = (int)r.ReadUnsigedNumber(r.ReadByte());
            }
        }

        public override void WritePacket(MySqlStreamWrtier writer)
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
        public override void ParsePacket(MySqlStreamReader r)
        {
            ParsePacketHeader(r); //4
            protocolVersion = r.U1();//1
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

        public override void WritePacket(MySqlStreamWrtier writer)
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
        uint _serverStatus;
        uint _warningCount;
        string _message;
        bool _protocol41;
        public OkPacket(bool protocol41)
        {
            _protocol41 = protocol41;
        }

        public override void ParsePacket(MySqlStreamReader r)
        {
            ParsePacketHeader(r);
            _fieldCount = r.U1();
            affectedRows = r.ReadLengthCodedNumber();
            insertId = r.ReadLengthCodedNumber();
            if (_protocol41)
            {
                _serverStatus = r.U2();
                _warningCount = r.U2();
            }
            _message = r.ReadPacketTerminatedString();
            //var m = this.message.match(/\schanged:\s * (\d +) / i);

            //if (m !== null)
            //{
            //    this.changedRows = parseInt(m[1], 10);
            //}
        }

        public override void WritePacket(MySqlStreamWrtier writer)
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
        public override void ParsePacket(MySqlStreamReader r)
        {
            ParsePacketHeader(r);
            _status = r.ReadByte();//alway 0
            statement_id = r.ReadUnsigedNumber(4);
            num_columns = r.ReadUnsigedNumber(2);
            num_params = r.ReadUnsigedNumber(2);
            r.ReadFiller(1);//reserved_1
            waring_count = r.ReadUnsigedNumber(2);
        }

        public override void WritePacket(MySqlStreamWrtier writer)
        {
            throw new NotImplementedException();
        }
    }

    class ResultSetHeaderPacket : Packet
    {
        long _fieldCount;
        uint _extraNumber;
        string _extraStr;
        public ResultSetHeaderPacket()
        {
        }
        public override void ParsePacket(MySqlStreamReader r)
        {

            ParsePacketHeader(r);

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

        public override void WritePacket(MySqlStreamWrtier writer)
        {
            throw new NotImplementedException();
        }
    }


    class DataRowPacket : Packet
    {
        protected MyStructData[] _myDataList;//cell
        protected TableHeader _tableHeader;
        public DataRowPacket(TableHeader tableHeader)
        {
            _tableHeader = tableHeader;
            _myDataList = new MyStructData[tableHeader.ColumnCount];
        }

        public override void ParsePacket(MySqlStreamReader r)
        {
            //function parse(parser, fieldPackets, typeCast, nestTables, connection) {
            //  var self = this;
            //  var next = function () {
            //    return self._typeCast(fieldPacket, parser, connection.config.timezone, connection.config.supportBigNumbers, connection.config.bigNumberStrings, connection.config.dateStrings);
            //  };

            //  for (var i = 0; i < fieldPackets.length; i++) {
            //    var fieldPacket = fieldPackets[i];
            //    var value;


            //---------------------------------------------
            //danger!
            //please note that  ***
            //data in each slot is not completely cleared

            //because we don't want to copy entire MyStructData back and forth
            //we just replace some part of it ***
            //---------------------------------------------
            ParsePacketHeader(r);
            List<FieldPacket> fieldInfos = _tableHeader.GetFields();
            int j = _tableHeader.ColumnCount;


            if (_tableHeader.TypeCast)
            {
                if (_tableHeader.NestTables)
                {
                    throw new NotSupportedException("nest table");
                }
                //------------
                for (int i = 0; i < j; i++)
                {
                    ReadCellWithTypeCast(r, fieldInfos[i], ref _myDataList[i]);
                }
            }
            else
            {
                for (int i = 0; i < j; i++)
                {
                    FieldPacket fieldInfo = fieldInfos[i];

                    if (fieldInfos[i].charsetNr == (int)CharSets.BINARY)
                    {
                        _myDataList[i].myBuffer = r.ReadLengthCodedBuffer();
                        _myDataList[i].type = (Types)fieldInfo.type;
                        //value = new MyStructData();
                        //value.myBuffer = parser.ParseLengthCodedBuffer();
                        //value.type = (Types)fieldInfos[i].type;
                    }
                    else
                    {
                        _myDataList[i].myString = r.ReadLengthCodedString();
                        _myDataList[i].type = (Types)fieldInfo.type;
                        //value = new MyStructData();
                        //value.myString = parser.ParseLengthCodedString();
                        //value.type = (Types)fieldInfos[i].type;
                    }
                    //    if (typeof typeCast == "function") {
                    //      value = typeCast.apply(connection, [ new Field({ packet: fieldPacket, parser: parser }), next ]);
                    //    } else {
                    //      value = (typeCast)
                    //        ? this._typeCast(fieldPacket, parser, connection.config.timezone, connection.config.supportBigNumbers, connection.config.bigNumberStrings, connection.config.dateStrings)
                    //        : ( (fieldPacket.charsetNr === Charsets.BINARY)
                    //          ? parser.parseLengthCodedBuffer()
                    //          : parser.parseLengthCodedString() );
                    //    }


                    //TODO: review here
                    //nestTables=? 


                    //if (nestTables)
                    //{
                    //    //      this[fieldPacket.table] = this[fieldPacket.table] || {};
                    //    //      this[fieldPacket.table][fieldPacket.name] = value;
                    //}
                    //else
                    //{
                    //    //      this[fieldPacket.name] = value;
                    //    myDataList[i] = value;
                    //}


                    //    if (typeof nestTables == "string" && nestTables.length) {
                    //      this[fieldPacket.table + nestTables + fieldPacket.name] = value;
                    //    } else if (nestTables) {
                    //      this[fieldPacket.table] = this[fieldPacket.table] || {};
                    //      this[fieldPacket.table][fieldPacket.name] = value;
                    //    } else {
                    //      this[fieldPacket.name] = value;
                    //    }
                    //  }
                    //}
                }
            }

        }

        /// <summary>
        /// read a data cell with type cast
        /// </summary>
        /// <param name="r"></param>
        /// <param name="fieldPacket"></param>
        /// <param name="data"></param>
        void ReadCellWithTypeCast(MySqlStreamReader r, FieldPacket fieldPacket, ref MyStructData data)
        {
            string numberString;
            Types type = (Types)fieldPacket.type;
            switch (type)
            {
                case Types.TIMESTAMP:
                case Types.DATE:
                case Types.DATETIME:
                case Types.NEWDATE:
                    {
                        StringBuilder tmpStringBuilder = r.TempStringBuilder;
                        QueryParsingConfig qparsingConfig = _tableHeader.ParsingConfig;
                        tmpStringBuilder.Length = 0;//clear 
                        data.myString = r.ReadLengthCodedString();
                        if (qparsingConfig.DateStrings)
                        {
                            //return new FieldData<string>(type, dateString);
                            //data.myString = dateString;
                            data.type = type;
                            return;
                        }

                        if (data.myString == null)
                        {
                            data.type = Types.NULL;
                            return;
                        }

                        //    var originalString = dateString;
                        //    if (field.type === Types.DATE) {
                        //      dateString += ' 00:00:00';
                        //    }
                        tmpStringBuilder.Append(data.myString);
                        //string originalString = dateString;
                        if (fieldPacket.type == (int)Types.DATE)
                        {
                            tmpStringBuilder.Append(" 00:00:00");
                        }
                        //    if (timeZone !== 'local') {
                        //      dateString += ' ' + timeZone;
                        //    }

                        if (!qparsingConfig.UseLocalTimeZone)
                        {
                            tmpStringBuilder.Append(' ' + qparsingConfig.TimeZone);
                        }
                        //var dt;
                        //    dt = new Date(dateString);
                        //    if (isNaN(dt.getTime())) {
                        //      return originalString;
                        //    }

                        data.myDateTime = DateTime.Parse(tmpStringBuilder.ToString());
                        data.type = type;
                        tmpStringBuilder.Length = 0;

                    }
                    return;
                case Types.TINY:
                case Types.SHORT:
                case Types.LONG:
                case Types.INT24:
                case Types.YEAR:

                    //TODO: review here,                    
                    data.myString = numberString = r.ReadLengthCodedString();
                    if (numberString == null || (fieldPacket.zeroFill && numberString[0] == '0') || numberString.Length == 0)
                    {
                        data.type = Types.NULL;
                    }
                    else
                    {
                        data.myInt32 = Convert.ToInt32(numberString);
                        data.type = type;
                    }
                    return;
                case Types.FLOAT:
                case Types.DOUBLE:
                    data.myString = numberString = r.ReadLengthCodedString();
                    if (numberString == null || (fieldPacket.zeroFill && numberString[0] == '0'))
                    {
                        data.myString = numberString;
                        data.type = Types.NULL;
                    }
                    else
                    {
                        data.myDouble = Convert.ToDouble(numberString);
                        data.type = type;
                    }
                    return;
                //    return (numberString === null || (field.zeroFill && numberString[0] == "0"))
                //      ? numberString : Number(numberString);
                case Types.NEWDECIMAL:
                case Types.LONGLONG:
                    //    numberString = parser.parseLengthCodedString();
                    //    return (numberString === null || (field.zeroFill && numberString[0] == "0"))
                    //      ? numberString
                    //      : ((supportBigNumbers && (bigNumberStrings || (Number(numberString) > IEEE_754_BINARY_64_PRECISION)))
                    //        ? numberString
                    //        : Number(numberString));

                    QueryParsingConfig config = _tableHeader.ParsingConfig;
                    data.myString = numberString = r.ReadLengthCodedString();
                    if (numberString == null || (fieldPacket.zeroFill && numberString[0] == '0'))
                    {
                        data.myString = numberString;
                        data.type = Types.NULL;
                    }
                    else if (config.SupportBigNumbers && (config.BigNumberStrings || (Convert.ToInt64(numberString) > IEEE_754_BINARY_64_PRECISION)))
                    {
                        //store as string ?
                        //TODO: review here  again
                        data.myString = numberString;
                        data.type = type;
                        throw new NotSupportedException();
                    }
                    else if (type == Types.LONGLONG)
                    {
                        data.myInt64 = Convert.ToInt64(numberString);
                        data.type = type;
                    }
                    else//decimal
                    {
                        data.myDecimal = Convert.ToDecimal(numberString);
                        data.type = type;
                    }
                    return;
                case Types.BIT:

                    data.myBuffer = r.ReadLengthCodedBuffer();
                    data.type = type;
                    return;
                //    return parser.parseLengthCodedBuffer();
                case Types.STRING:
                case Types.VAR_STRING:
                case Types.TINY_BLOB:
                case Types.MEDIUM_BLOB:
                case Types.LONG_BLOB:
                case Types.BLOB:
                    if (fieldPacket.charsetNr == (int)CharSets.BINARY)
                    {
                        data.myBuffer = r.ReadLengthCodedBuffer(); //CodedBuffer
                        data.type = type;
                    }
                    else
                    {
                        data.myString = r.ReadLengthCodedString();//codeString
                        data.type = type;
                    }
                    return;
                //    return (field.charsetNr === Charsets.BINARY)
                //      ? parser.parseLengthCodedBuffer()
                //      : parser.parseLengthCodedString();
                case Types.GEOMETRY:
                    //TODO: unfinished
                    data.type = Types.GEOMETRY;
                    return;
                default:
                    data.myString = r.ReadLengthCodedString();
                    data.type = type;
                    return;
            }
        }

        public override void WritePacket(MySqlStreamWrtier writer)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            int count = _myDataList.Length;
            switch (count)
            {
                case 0: return "";
                case 1:
                    return _myDataList[0].ToString();
                default:
                    var stBuilder = new StringBuilder();
                    //1st
                    stBuilder.Append(_myDataList[0].ToString());
                    //then
                    for (int i = 1; i < count; ++i)
                    {
                        //then..
                        stBuilder.Append(',');
                        stBuilder.Append(_myDataList[i].ToString());
                    }
                    return stBuilder.ToString();
            }
        }

        internal MyStructData[] Cells
        {
            get { return _myDataList; }
        }
    }


    class PreparedDataRowPacket : DataRowPacket
    {
        public PreparedDataRowPacket(TableHeader tableHeader)
            : base(tableHeader)
        {
        }
        public override void ParsePacket(MySqlStreamReader r)
        {
            var fieldInfos = _tableHeader.GetFields();
            int columnCount = _tableHeader.ColumnCount;
            ParsePacketHeader(r);
            r.ReadFiller(1);//skip start packet byte [00]
            r.ReadFiller((columnCount + 7 + 2) / 8);//skip null-bitmap, length:(column-count+7+2)/8
            for (int i = 0; i < columnCount; i++)
            {
                ParseValues(r, fieldInfos[i], ref _myDataList[i]);
#if DEBUG
                //-------------------------------------------------
                //this code affect on performance when debug
                //byte[] mybuffer = _myDataList[i].myBuffer;
                //dbugBufferView view = new dbugBufferView(mybuffer, 0, mybuffer.Length);
                //view.viewIndex = view.CheckNoDulpicateBytes();
                //-------------------------------------------------
#endif
            }
        }

        static void ParseValues(MySqlStreamReader r, FieldPacket fieldInfo, ref MyStructData myData)
        {
            Types fieldType = (Types)fieldInfo.type;
            switch (fieldType)
            {
                case Types.TIMESTAMP://
                case Types.DATE://
                case Types.DATETIME://
                case Types.NEWDATE://
                    r.ReadLengthCodedDateTime(out myData.myDateTime);
                    myData.type = fieldType;
                    break;
                case Types.TINY://length = 1;
                    myData.myInt32 = r.U1();
                    myData.type = fieldType;
                    break;
                case Types.SHORT://length = 2;
                case Types.YEAR://length = 2;
                    myData.myInt32 = (int)r.U2();
                    myData.type = fieldType;
                    break;
                case Types.INT24:
                case Types.LONG://length = 4;
                    myData.myInt32 = (int)r.U4();
                    myData.type = fieldType;
                    break;
                case Types.FLOAT:
                    myData.myDouble = r.ReadFloat();
                    myData.type = fieldType;
                    break;
                case Types.DOUBLE:
                    myData.myDouble = r.ReadDouble();
                    myData.type = fieldType;
                    break;
                case Types.NEWDECIMAL:
                    myData.myDecimal = r.ReadDecimal();
                    myData.type = fieldType;
                    break;
                case Types.LONGLONG:
                    myData.myInt64 = r.ReadInt64();
                    myData.type = fieldType;
                    break;
                case Types.STRING:
                case Types.VARCHAR:
                case Types.VAR_STRING:
                    myData.myString = r.ReadLengthCodedString();
                    myData.type = fieldType;
                    break;
                case Types.TINY_BLOB:
                case Types.MEDIUM_BLOB:
                case Types.LONG_BLOB:
                case Types.BLOB:
                case Types.BIT:
                    myData.myBuffer = r.ReadLengthCodedBuffer();
                    myData.type = fieldType;
                    break;
                case Types.GEOMETRY:

                default:
                    myData.myBuffer = r.ReadLengthCodedBuffer();
                    myData.type = Types.NULL;
                    break;
            }
        }




    }


#if DEBUG
    public struct dbugBufferView
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