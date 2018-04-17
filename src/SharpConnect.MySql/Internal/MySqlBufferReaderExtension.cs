//LICENSE: MIT
//Copyright(c) 2012 Felix Geisendörfer(felix @debuggable.com) and contributors 
//Copyright(c) 2013 Andrey Sidorov(sidorares @yandex.ru) and contributors
//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
namespace SharpConnect.MySql.Internal
{
    static class BufferReaderExtension
    {
        /// <summary>
        /// read unsigned 1 byte
        /// </summary>
        /// <returns></returns>
        public static byte U1(this BufferReader bufferReader)
        {

            return bufferReader.ReadByte();
        }

        /// <summary>
        /// read unsigned 2 bytes
        /// </summary>
        /// <returns></returns>
        public static uint U2(this BufferReader bufferReader)
        {

            uint b0 = bufferReader.ReadByte(); //low bit
            uint b1 = bufferReader.ReadByte(); //high bit
            return (b1 << 8) | (b0);
        }
        /// <summary>
        /// read unsigned 3 bytes
        /// </summary>
        /// <returns></returns>
        public static uint U3(this BufferReader bufferReader)
        {

            uint b0 = bufferReader.ReadByte(); //low bit
            uint b1 = bufferReader.ReadByte();
            uint b2 = bufferReader.ReadByte(); //high bit
            return (b2 << 16) | (b1 << 8) | (b0);
        }
        /// <summary>
        /// read unsigned 4 bytes
        /// </summary>
        /// <returns></returns>
        public static uint U4(this BufferReader bufferReader)
        {

            uint b0 = bufferReader.ReadByte(); //low bit
            uint b1 = bufferReader.ReadByte();
            uint b2 = bufferReader.ReadByte();
            uint b3 = bufferReader.ReadByte(); //high bit
            return (b3 << 24) | (b2 << 16) | (b1 << 8) | (b0);
        }

        public static uint ReadLengthCodedNumber(this BufferReader bufferReader)
        {
            //ignore is null
            bool isNull;
            return ReadLengthCodedNumber(bufferReader, out isNull);
        }
        public static uint ReadLengthCodedNumber(this BufferReader reader, out bool isNullData)
        {

            isNullData = false;
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
                case 251:
                    isNullData = true;
                    return 0;
                case 252: return U2(reader);
                case 253: return U3(reader);
                case 254: break;
                default: throw new Exception("Unexpected first byte");
            }
            //    var low = this.parseUnsignedNumber(4);
            //    var high = this.parseUnsignedNumber(4);
            //    var value;
            uint low = U4(reader);
            uint high = U4(reader);
            if ((uint)(high >> 21) > 0)
            {
                //TODO: review here 
                //support big number
                long value = low + ((2 << 32) * high);
            }
            return low + ((2 << 32) * high);
            //if (high >>> 21)
            //{
            //    value = (new BigNumber(low)).plus((new BigNumber(MUL_32BIT)).times(high)).toString();

            //    if (this._supportBigNumbers)
            //    {
            //        return value;
            //    }

            //    var err = new Error(
            //      'parseLengthCodedNumber: JS precision range exceeded, ' +
            //      'number is >= 53 bit: "' + value + '"'
            //    );
            //    err.offset = (this._offset - this._packetOffset - 8);
            //    err.code = 'PARSER_JS_PRECISION_RANGE_EXCEEDED';
            //    throw err;
            //}

            //value = low + (MUL_32BIT * high);

            //return value;
        }
        public static string ReadLengthCodedString(this BufferReader reader, IStringConverter strConverter)
        {

            //var length = this.parseLengthCodedNumber();
            bool isNull;
            uint length = ReadLengthCodedNumber(reader, out isNull);
            //if (length === null) {
            //  return null;
            //}
            return isNull ? null : ReadString(reader, length, strConverter);
            //return this.parseString(length);
        }

        /// <summary>
        /// return 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="outputBuffer"></param>
        /// <returns></returns>
        public static byte[] ReadLengthCodedBuffer(this BufferReader reader, out bool isNull)
        {
            uint length = ReadLengthCodedNumber(reader, out isNull);
            byte[] output = reader.ReadBytes((int)length);
            return isNull ? null : output;

        }

        public static byte[] ReadLengthCodedBuffer(this BufferReader reader)
        {
            bool isNull;
            byte[] output = ReadLengthCodedBuffer(reader, out isNull);
            return isNull ? null : output;
        }
        public static string ReadString(this BufferReader reader,
            uint length,
            IStringConverter strConverter)
        {

            return strConverter.ReadConv(reader.ReadBytes((int)length));

        }

        public static bool ReadLengthCodedDateTime(this BufferReader reader, out DateTime result)
        {

            byte dateLength = reader.ReadByte(); //***     
            int year = 0;
            int month = 0;
            int day = 0;
            int hour = 0;
            int minute = 0;
            int second = 0;
            int micro_second = 0;
            //0, 4,7,11
            switch (dateLength)
            {
                default:
                case 0:
                    result = DateTime.MinValue;
                    return false;
                case 4:
                    year = (int)reader.U2();
                    month = reader.U1();
                    day = reader.U1();
                    result = new DateTime(year, month, day);
                    return true;
                case 7:
                    year = (int)reader.U2();
                    month = reader.U1();
                    day = reader.U1();
                    hour = reader.U1();
                    minute = reader.U1();
                    second = reader.U1();
                    result = new DateTime(year, month, day, hour, minute, second);
                    return true;
                case 11:
                    year = (int)reader.U2();
                    month = reader.U1();
                    day = reader.U1();
                    hour = reader.U1();
                    minute = reader.U1();
                    second = reader.U1();
                    micro_second = (int)reader.ReadUnsigedNumber(4);
                    result = new DateTime(year, month, day, hour, minute, second, micro_second / 1000);
                    return true;
            }
            //if (dateLength == 0)
            //{
            //    result = DateTime.MinValue;
            //    return false;
            //} 
            //if (dateLength >= 4)
            //{
            //    year = (int)ParseUnsigned2();
            //    month = ParseUnsigned1();
            //    day = ParseUnsigned1();
            //    dateTime = new DateTime(year, month, day);
            //}
            //if (dateLength >= 7)
            //{
            //    hour = ParseUnsigned1();
            //    minute = ParseUnsigned1();
            //    second = ParseUnsigned1();
            //    dateTime = new DateTime(year, month, day, hour, minute, second);
            //} 
            //if (dateLength == 11)
            //{
            //    micro_second = (int)ParseUnsignedNumber(4);
            //    int milli_second = micro_second / 1000;
            //    dateTime = new DateTime(year, month, day, hour, minute, second, milli_second);
            //}
            //else
            //{
            //    if (dateLength == 7)
            //    {
            //        dateTime = new DateTime(year, month, day, hour, minute, second);
            //    }
            //    else if (dateLength == 4)
            //    {
            //        dateTime = new DateTime(year, month, day);
            //    }
            //    else
            //    {
            //        dateTime = new DateTime(0, 0, 0, 0, 0, 0, 0, 0);
            //    }
            //}

            //return dateTime;
        }

        public static uint ReadUnsigedNumber(this BufferReader reader, int n)
        {

            switch (n)
            {
                case 0: throw new NotSupportedException();
                case 1: return reader.ReadByte();
                case 2:
                    {
                        uint b0 = reader.ReadByte(); //low bit
                        uint b1 = reader.ReadByte(); //high bit
                        return (b1 << 8) | (b0);
                    }
                case 3:
                    {
                        uint b0 = reader.ReadByte(); //low bit
                        uint b1 = reader.ReadByte();
                        uint b2 = reader.ReadByte(); //high bit
                        return (b2 << 16) | (b1 << 8) | (b0);
                    }
                case 4:
                    {
                        uint b0 = reader.ReadByte(); //low bit
                        uint b1 = reader.ReadByte();
                        uint b2 = reader.ReadByte();
                        uint b3 = reader.ReadByte(); //high bit
                        return (b3 << 24) | (b2 << 16) | (b1 << 8) | (b0);
                    }
                default:
                    throw new Exception("parseUnsignedNumber: Supports only up to 4 bytes");
            }
            //if (bytes === 1)
            //{
            //    return this._buffer[this._offset++];
            //}

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


            //long start = Position;
            //long end = start + n - 1;

            //while (offset >= this._offset)
            //{
            //    value = ((value << 8) | buffer[offset]) >>> 0;
            //    offset--;
            //}


            //this._offset += bytes;
            //return value;
            //return value;
        }

    }

}
