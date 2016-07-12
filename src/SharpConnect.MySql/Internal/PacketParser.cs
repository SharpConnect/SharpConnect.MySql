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
using System.IO;
using System.Text;
namespace SharpConnect.MySql.Internal
{
    /// <summary>
    /// mysql packet stream parser
    /// </summary>
    class PacketParser
    {
        readonly BinaryReader _reader;
        readonly MemoryStream _stream;
        int _currentInputLength;
        long _startPosition;
        long _packetLength;
        Encoding _encoding = Encoding.UTF8;
        List<byte> _bList = new List<byte>();

#if DEBUG
        static int dbugTotalId;
        public readonly int dbugId = dbugTotalId++;
#endif
        public PacketParser(Encoding encoding)
        {
            _encoding = encoding;
            _stream = new MemoryStream();
            _startPosition = _stream.Position;//stream.Position = 0;
            _reader = new BinaryReader(_stream, encoding);
        }

        ~PacketParser()
        {
            Dispose();
        }

        /// <summary>
        /// current stream's paring position
        /// </summary>
        public long ReadPosition
        {
            get { return _stream.Position; }
        }

        public bool Ensure(uint len)
        {
            return _stream.Position + len <= _currentInputLength;
        }
        /// <summary>
        /// actual buffer length
        /// </summary>
        public long CurrentInputLength
        {
            get
            {
                return _currentInputLength;
            }
        }

        public void Dispose()
        {
            _reader.Close();
            _stream.Close();
            _stream.Dispose();
        }

        public void Reset()
        {
            _stream.Position = 0;
            _startPosition = 0;
            _currentInputLength = 0;
        }
        public void SetPosition(int pos)
        {
            _stream.Position = pos;
        }
        public void LoadNewBuffer(byte[] newBuffer, int count)
        {
            Reset();
            _stream.Write(newBuffer, 0, count);
            _stream.Position = 0;
            _startPosition = 0;
            _currentInputLength = count;
        }
        public void AppendBuffer(byte[] buffer, int count)
        {
            long saved_pos = _stream.Position;
            _stream.Position = _currentInputLength;
            _stream.Write(buffer, 0, count);
            _stream.Position = saved_pos;
            _currentInputLength += count;
        }
        public string ParseNullTerminatedString()
        {
            _bList.Clear();
            byte temp = _reader.ReadByte();
            _bList.Add(temp);
            while (temp != 0)
            {
                temp = _reader.ReadByte();
                _bList.Add(temp);
            }
            byte[] bytes = _bList.ToArray();
            return _encoding.GetString(bytes);
        }

        public byte[] ParseNullTerminatedBuffer()
        {
            _bList.Clear();
            var temp = _reader.ReadByte();
            _bList.Add(temp);
            while (temp != 0x00)
            {
                temp = _reader.ReadByte();
                _bList.Add(temp);
            }
            return _bList.ToArray();
        }

        public byte ParseByte()
        {
            return _reader.ReadByte();
        }

        public byte[] ParseBuffer(int n)
        {
            if (n > 0)
                return _reader.ReadBytes(n);
            else
                return null;
        }

        public bool ParseLengthCodedDateTime(out DateTime result)
        {

            byte dateLength = ParseByte(); //***     

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
                    year = (int)ParseUnsigned2();
                    month = ParseUnsigned1();
                    day = ParseUnsigned1();
                    result = new DateTime(year, month, day);
                    return true;
                case 7:
                    year = (int)ParseUnsigned2();
                    month = ParseUnsigned1();
                    day = ParseUnsigned1();
                    hour = ParseUnsigned1();
                    minute = ParseUnsigned1();
                    second = ParseUnsigned1();
                    result = new DateTime(year, month, day, hour, minute, second);
                    return true;
                case 11:
                    year = (int)ParseUnsigned2();
                    month = ParseUnsigned1();
                    day = ParseUnsigned1();
                    hour = ParseUnsigned1();
                    minute = ParseUnsigned1();
                    second = ParseUnsigned1();
                    micro_second = (int)ParseUnsignedNumber(4);
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
#if DEBUG
        static int debugLastPacketNum = 1;
#endif
        public PacketHeader ParsePacketHeader()
        {
            _startPosition = _stream.Position;
            PacketHeader header = new PacketHeader(ParseUnsigned3(), ParseByte());
#if DEBUG
            if (header.PacketNumber > debugLastPacketNum + 1)
            {
                Console.WriteLine("header.PacketNumber : " + header.PacketNumber + " > lastPacketNumber :" + debugLastPacketNum);
            }
            debugLastPacketNum = header.PacketNumber;
#endif
            _packetLength = header.ContentLength + 4;
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

        public void ParseFiller(int length)
        {
            _stream.Position += length;
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
            if (ReadPosition >= CurrentInputLength)
            {
                throw new Exception("Parser: read past end");
            }
            //    var bits = this._buffer[this._offset++];

            byte bits = _reader.ReadByte();
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
                case 252: return ParseUnsigned2();
                case 253: return ParseUnsigned3();
                case 254: break;
                default: throw new Exception("Unexpected first byte");
            }
            //    var low = this.parseUnsignedNumber(4);
            //    var high = this.parseUnsignedNumber(4);
            //    var value;
            uint low = ParseUnsigned4();
            uint high = ParseUnsigned4();
            if ((uint)(high >> 21) > 0)
            {
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

        public byte ParseUnsigned1()
        {
            return _reader.ReadByte();
        }

        public uint ParseUnsigned2()
        {
            uint b0 = _reader.ReadByte(); //low bit
            uint b1 = _reader.ReadByte(); //high bit
            return (b1 << 8) | (b0);
        }

        public uint ParseUnsigned3()
        {
            uint b0 = _reader.ReadByte(); //low bit
            uint b1 = _reader.ReadByte();
            uint b2 = _reader.ReadByte(); //high bit
            return (b2 << 16) | (b1 << 8) | (b0);
        }

        public uint ParseUnsigned4()
        {
            uint b0 = _reader.ReadByte(); //low bit
            uint b1 = _reader.ReadByte();
            uint b2 = _reader.ReadByte();
            uint b3 = _reader.ReadByte(); //high bit
            return (b3 << 24) | (b2 << 16) | (b1 << 8) | (b0);
        }

        public float ParseFloat()
        {
            return _reader.ReadSingle();
        }

        public double ParseDouble()
        {
            return _reader.ReadDouble();
        }

        public decimal ParseDecimal()
        {
            return _reader.ReadDecimal();
        }

        public long ParseInt64()
        {
            return _reader.ReadInt64();
        }

        public uint ParseUnsignedNumber(int n)
        {
            switch (n)
            {
                case 0: throw new NotSupportedException();
                case 1: return _reader.ReadByte();
                case 2:
                    {
                        uint b0 = _reader.ReadByte(); //low bit
                        uint b1 = _reader.ReadByte(); //high bit
                        return (b1 << 8) | (b0);
                    }
                case 3:
                    {
                        uint b0 = _reader.ReadByte(); //low bit
                        uint b1 = _reader.ReadByte();
                        uint b2 = _reader.ReadByte(); //high bit
                        return (b2 << 16) | (b1 << 8) | (b0);
                    }
                case 4:
                    {
                        uint b0 = _reader.ReadByte(); //low bit
                        uint b1 = _reader.ReadByte();
                        uint b2 = _reader.ReadByte();
                        uint b3 = _reader.ReadByte(); //high bit
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

        public string ParsePacketTerminatedString()
        {
            long distance = (_startPosition + _packetLength) - ReadPosition;
            if (distance > 0)
            {
                return new string(_reader.ReadChars((int)distance));
            }
            else
            {
                return null;
            }
        }

        public char ParseChar()
        {
            return _reader.ReadChar();
        }

        public string ParseString(uint length)
        {
            return _encoding.GetString(_reader.ReadBytes((int)length));
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

        public byte PeekByte()
        {
            byte result = _reader.ReadByte();
            _reader.BaseStream.Position--;
            return result;
            //return (byte)_reader.PeekChar();
        }

        public bool ReachedPacketEnd()
        {
            return this.ReadPosition == _startPosition + _packetLength;
        }

        public byte[] ToArray()
        {
            return _stream.ToArray();
        }
    }
}