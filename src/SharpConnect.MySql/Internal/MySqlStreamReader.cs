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
using System.IO;
using System.Text;
namespace SharpConnect.MySql.Internal
{
    /// <summary>
    /// mysql packet stream parser
    /// </summary>
    class MySqlStreamReader : IDisposable
    {
        BinaryReader _reader;
        MemoryStream _stream;
        int _currentInputLength;
        long _packetHeaderStartAt;
        long _packetLength;
        Encoding _encoding = Encoding.UTF8;
        List<byte> _bList = new List<byte>();

#if DEBUG
        static int dbugTotalId;
        public readonly int dbugId = dbugTotalId++;
        public bool dbugMonitorData1;
        public int debugLastPacketNum = 1;
#endif
        public MySqlStreamReader(Encoding encoding)
        {
            _encoding = encoding;
            _stream = new MemoryStream();
            _reader = new BinaryReader(_stream, encoding);
        }

#if DEBUG
        [System.Diagnostics.Conditional("DEBUG")]
        void dbugBreakOnMonitorData()
        {
            if (dbugMonitorData1)
            {
            }
        }
#endif

        ~MySqlStreamReader()
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
            if (_reader != null)
            {
#if NET20
                _reader.Close();
#endif
                _reader = null;
            }
            if (_stream != null)
            {
#if NET20
                _stream.Close();
#endif
                _stream.Dispose();
                _stream = null;
            }
        }
        public void Reset()
        {
#if DEBUG
            dbugBreakOnMonitorData();
            if (_stream.Position < _currentInputLength)
            {

            }
#endif
            //_stream = new MemoryStream();
            //_reader = new BinaryReader(_stream); 

            _stream.Position = 0;
            _packetHeaderStartAt = 0;
            _currentInputLength = 0;
        }
        //------------------------------------------------------ 
        internal void AppendBuffer(SharpConnect.Internal.RecvIO recvIO, int count)
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif

            long saved_pos = _stream.Position;
            _stream.Position = _currentInputLength;
            //----------------------------
            recvIO.CopyTo(0, _stream, count);
            //----------------------------
            //_stream.Write(buffer, 0, count);
            _stream.Position = saved_pos;
            _currentInputLength += count;
        }

        //------------------------------------------------------
        public string ReadNullTerminatedString()
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
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

        public byte ReadByte()
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            return _reader.ReadByte();
        }

        public byte[] ReadBuffer(int n)
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            if (n > 0)
                return _reader.ReadBytes(n);
            else
                return null;
        }
        public void SkipForward(int byteOffset)
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            _reader.BaseStream.Position += byteOffset;
        }

        public PacketHeader ReadPacketHeader()
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            _packetHeaderStartAt = _stream.Position;

            PacketHeader header = new PacketHeader(U3(), ReadByte());
#if DEBUG
            if (header.PacketNumber > debugLastPacketNum + 1)
            {
                SharpConnect.Internal.dbugConsole.WriteLine("header.PacketNumber : " + header.PacketNumber + " > lastPacketNumber :" + debugLastPacketNum);
            }
            else if (header.PacketNumber > 0 && header.PacketNumber < debugLastPacketNum && debugLastPacketNum != 2)
            {
                //?
            }
            SharpConnect.Internal.dbugConsole.WriteLine("h>> " + header.PacketNumber + ":" + debugLastPacketNum + ":" + _packetHeaderStartAt);
            debugLastPacketNum = header.PacketNumber;
#endif
            _packetLength = header.ContentLength + 4;
            return header;
        }

        public string ReadLengthCodedString()
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            //var length = this.parseLengthCodedNumber();
            bool isNull;
            uint length = ReadLengthCodedNumber(out isNull);
            //if (length === null) {
            //  return null;
            //}
            return isNull ? null : ReadString(length);


            //return this.parseString(length);
        }

        public bool ReadLengthCodedBuffer(out byte[] outputBuffer)
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            bool isNull;
            uint length = ReadLengthCodedNumber(out isNull);
            outputBuffer = isNull ? null : ReadBuffer((int)length);
            return isNull;
        }

        public byte[] ReadLengthCodedBuffer()
        {
            byte[] output = null;
            if (ReadLengthCodedBuffer(out output))
            {
                return output;
            }
            return null;
        }
        public void ReadFiller(int length)
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            _stream.Position += length;
        }

        public uint ReadLengthCodedNumber()
        {
            //ignore is null
            bool isNull;
            return ReadLengthCodedNumber(out isNull);
        }
        public uint ReadLengthCodedNumber(out bool isNullData)
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            isNullData = false;
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
                case 251:
                    isNullData = true;
                    return 0;
                case 252: return U2();
                case 253: return U3();
                case 254: break;
                default: throw new Exception("Unexpected first byte");
            }
            //    var low = this.parseUnsignedNumber(4);
            //    var high = this.parseUnsignedNumber(4);
            //    var value;
            uint low = U4();
            uint high = U4();
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


        /// <summary>
        /// read unsigned 2 bytes
        /// </summary>
        /// <returns></returns>
        public uint U2()
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            uint b0 = _reader.ReadByte(); //low bit
            uint b1 = _reader.ReadByte(); //high bit
            return (b1 << 8) | (b0);
        }

        /// <summary>
        /// read unsigned 3 bytes
        /// </summary>
        /// <returns></returns>
        public uint U3()
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            uint b0 = _reader.ReadByte(); //low bit
            uint b1 = _reader.ReadByte();
            uint b2 = _reader.ReadByte(); //high bit
            return (b2 << 16) | (b1 << 8) | (b0);
        }
        /// <summary>
        /// read unsigned 4 bytes
        /// </summary>
        /// <returns></returns>
        public uint U4()
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            uint b0 = _reader.ReadByte(); //low bit
            uint b1 = _reader.ReadByte();
            uint b2 = _reader.ReadByte();
            uint b3 = _reader.ReadByte(); //high bit
            return (b3 << 24) | (b2 << 16) | (b1 << 8) | (b0);
        }


        public uint ReadUnsigedNumber(int n)
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
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

        public string ReadPacketTerminatedString()
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            long distance = (_packetHeaderStartAt + _packetLength) - ReadPosition;
            if (distance > 0)
            {
                return new string(_reader.ReadChars((int)distance));
            }
            else
            {
                return null;
            }
        }

        public char ReadChar()
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            return _reader.ReadChar();
        }

        public string ReadString(uint length)
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            return _encoding.GetString(_reader.ReadBytes((int)length));
        }

        public List<Geometry> ReadGeometryValues()
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            //var buffer = this.parseLengthCodedBuffer();
            //var offset = 4;
            byte[] buffer = ReadLengthCodedBuffer();
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
            ReadGeometry(result, buffer, byteOrder, wkbType, offset);
            return result;
        }

        void ReadGeometry(List<Geometry> result, byte[] buffer, int byteOrder, int wkbType, int offset)
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
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
                        ReadGeometry(result, buffer, byteOrder, wkbType, offset);
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
#if DEBUG
            dbugBreakOnMonitorData();
#endif
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
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            return 0;
        }

        double ReadDoubleLE(byte[] buffer, int start)
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            return 0;
        }

        double ReadDoubleBE(byte[] buffer, int start)
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            return 0;
        }

        public byte PeekByte()
        {
#if DEBUG
            dbugBreakOnMonitorData();
#endif
            byte result = _reader.ReadByte();
            _reader.BaseStream.Position--;
            return result;
            //return (byte)_reader.PeekChar();
        }

        public bool ReachedPacketEnd()
        {
            return this.ReadPosition == _packetHeaderStartAt + _packetLength;
        }

    }
}