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

namespace MySqlPacket
{
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

        void Reset()
        {
            stream.Position = 0;
            myLength = 0;
        }

        public void LoadNewBuffer(byte[] newBuffer, int count)
        {
            Reset();
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


        public void ParseFiller(int length)
        {
            this.stream.Position += length;
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
}