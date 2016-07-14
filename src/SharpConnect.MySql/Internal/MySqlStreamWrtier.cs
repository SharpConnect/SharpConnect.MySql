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
using System.IO;
using System.Text;
namespace SharpConnect.MySql.Internal
{
    class MySqlStreamWrtier
    {
        MyBinaryWriter _writer;
        byte _packetNumber;
        long _startPacketPosition;
        int _maxAllowedLength = Packet.MAX_PACKET_LENGTH;
        Encoding _encoding;
        byte[] _headerBuffer = new byte[4];//reusable header buffer
        const int BIT_16 = (int)1 << 16;//(int)Math.Pow(2, 16);
        const int BIT_24 = (int)1 << 24;//(int)Math.Pow(2, 24);
        public MySqlStreamWrtier(Encoding encoding)
        {
            _writer = new MyBinaryWriter();
            _writer.Reset();
            _packetNumber = 0;
            _startPacketPosition = 0;
            _encoding = encoding;
        }

        ~MySqlStreamWrtier()
        {
            Dispose();
        }
        public long Position
        {
            get { return _writer.OriginalStreamPosition; }
        }
        public long Length
        {
            get { return _writer.Length; }
        }

        public void SetMaxAllowedPacket(long max)
        {
            _maxAllowedLength = max;
        }

        public void Reset()
        {
            _packetNumber = 0;
            _startPacketPosition = 0;
            _writer.Reset();
        }

        public void Dispose()
        {
            _writer.Close();
        }

        public void ReserveHeader()
        {
            _startPacketPosition = _writer.OriginalStreamPosition;
            WriteFiller(4);
        }

        public byte IncrementPacketNumber()
        {
            return _packetNumber++;
        }

        public void WriteHeader(PacketHeader header)
        {
            //  var packets  = Math.floor(this._buffer.length / MAX_PACKET_LENGTH) + 1;
            //  var buffer   = this._buffer;
            //int maxPacketLength = MAX_PACKET_LENGTH;

            long curPacketLength = CurrentPacketLength();
            dbugConsole.WriteLine("Current Packet Length = " + curPacketLength);

            //TODO: review MAX_PACKET_LENGTH here ****
            //it should be 
            int packetCount = (int)((curPacketLength - 4) / _maxAllowedLength) + 1;//-4 bytes of reserve header
            //int packetCount = (int)((curPacketLength - 4) / Packet.MAX_PACKET_LENGTH) + 1;//-4 bytes of reserve header
            if (packetCount == 1)
            {
                if (header.ContentLength > _maxAllowedLength)
                {
                    throw new Exception("Packet for query is too larger than MAX_ALLOWED_LENGTH");
                }
                WriteEncodedUnsignedNumber0_3(_headerBuffer, header.ContentLength);
                _headerBuffer[3] = header.PacketNumber;
                _writer.RewindWriteAndJumpBack(_headerBuffer, (int)_startPacketPosition);
            }
            else //>1 
            {
                _packetNumber = header.PacketNumber;//set start current packet number
                long allDataLength = (curPacketLength - 4) + (packetCount * 4);
                if (allDataLength > _maxAllowedLength)
                {
                    throw new Exception("Packet for query is too larger than MAX_ALLOWED_LENGTH");
                }
                byte[] allBuffer = new byte[allDataLength];
                byte[] dataBuffer = new byte[allDataLength - 4];//remove header
                _writer.Read(dataBuffer, 4, (int)allDataLength - 4);//skip reserve header bytes
                int offset = 0;
                for (int packet = 0; packet < packetCount; packet++)
                {
                    offset = (packet * _maxAllowedLength);
                    //    var isLast = (packet + 1 === packets);
                    //    var packetLength = (isLast)
                    //      ? buffer.length % MAX_PACKET_LENGTH
                    //      : MAX_PACKET_LENGTH;
                    int packetLength = (packet + 1 == packetCount)
                        ? (int)((curPacketLength - 4) % _maxAllowedLength)
                        : _maxAllowedLength;
                    //    var packetNumber = parser.incrementPacketNumber();

                    //    this.writeUnsignedNumber(3, packetLength);
                    //    this.writeUnsignedNumber(1, packetNumber);

                    //    var start = packet * MAX_PACKET_LENGTH;
                    //    var end   = start + packetLength;

                    //    this.writeBuffer(buffer.slice(start, end));
                    int start = packet * (_maxAllowedLength + 4);
                    //byte[] encodeData = new byte[4];
                    WriteEncodedUnsignedNumber0_3(_headerBuffer, (uint)packetLength);
                    _headerBuffer[3] = IncrementPacketNumber();
                    Buffer.BlockCopy(_headerBuffer, 0, allBuffer, start, 4);
                    Buffer.BlockCopy(dataBuffer, offset, allBuffer, start + 4, packetLength);
                }
                _writer.RewindWriteAndJumpBack(allBuffer, (int)_startPacketPosition);
            }
        }
        public long CurrentPacketLength()
        {
            return _writer.OriginalStreamPosition - _startPacketPosition;
        }
        public void WriteNullTerminatedString(string str)
        {
            byte[] buff = _encoding.GetBytes(str.ToCharArray());
            _writer.Write(buff);
            _writer.Write((byte)0);
        }

        public void WriteNullTerminatedBuffer(byte[] value)
        {
            WriteBuffer(value);
            WriteFiller(1);
        }
        public void WriteUnsigned1(uint value)
        {
            _writer.Write((byte)(value & 0xff));
        }
        public void WriteUnsigned2(uint value)
        {
            _writer.Write((byte)(value & 0xff));
            _writer.Write((byte)((value >> 8) & 0xff));
        }
        public void WriteUnsigned3(uint value)
        {
            _writer.Write((byte)(value & 0xff));
            _writer.Write((byte)((value >> 8) & 0xff));
            _writer.Write((byte)((value >> 16) & 0xff));
        }
        public void WriteUnsigned4(uint value)
        {
            _writer.Write((byte)(value & 0xff));
            _writer.Write((byte)((value >> 8) & 0xff));
            _writer.Write((byte)((value >> 16) & 0xff));
            _writer.Write((byte)((value >> 24) & 0xff));
        }
        public void WriteUnsignedNumber(int length, uint value)
        {
            switch (length)
            {
                case 0: break;
                case 1:

                    _writer.Write((byte)(value & 0xff));
                    break;
                case 2:

                    _writer.Write((byte)(value & 0xff));
                    _writer.Write((byte)((value >> 8) & 0xff));
                    break;
                case 3:

                    _writer.Write((byte)(value & 0xff));
                    _writer.Write((byte)((value >> 8) & 0xff));
                    _writer.Write((byte)((value >> 16) & 0xff));
                    break;
                case 4:

                    _writer.Write((byte)(value & 0xff));
                    _writer.Write((byte)((value >> 8) & 0xff));
                    _writer.Write((byte)((value >> 16) & 0xff));
                    _writer.Write((byte)((value >> 24) & 0xff));
                    break;
                case 5:
                    throw new NotSupportedException();
                    ////?  not possible?
                    //byte[] tempBuff = new byte[length];
                    //for (var i = 0; i < length; i++)
                    //{
                    //    tempBuff[i] = (byte)((value >> (i * 8)) & 0xff);
                    //}
                    //writer.Write(tempBuff);
                    //break;
            }
        }
        /// <summary>
        /// write encode number at index 0 of this outputBuffer
        /// </summary>
        /// <param name="outputBuffer"></param>
        /// <param name="value"></param>
        static void WriteEncodedUnsignedNumber0_3(byte[] outputBuffer, uint value)
        {
            //start at 0
            //length= 3
            outputBuffer[0] = (byte)(value & 0xff);
            outputBuffer[1] = (byte)((value >> 8) & 0xff);
            outputBuffer[2] = (byte)((value >> 16) & 0xff);
        }

        public void WriteByte(byte value)
        {
            _writer.Write(value);
        }

        public void WriteInt64(long value)
        {
            _writer.WriteInt64(value);
        }

        public void WriteFloat(float value)
        {
            _writer.WriteFloat(value);
        }

        public void WriteDouble(double value)
        {
            _writer.WriteDouble(value);
        }

        public void WriteFiller(int length)
        {
            switch (length)
            {
                case 0:
                    break;
                case 1:
                    _writer.Write((byte)0);//1
                    break;
                case 2:
                    _writer.WriteInt16(0);//2
                    break;
                case 3:
                    _writer.WriteInt16(0);//2
                    _writer.Write((byte)0);//1
                    break;
                case 4:
                    _writer.WriteInt32(0);//4
                    break;
                default:
                    //else
                    byte[] filler = new byte[length];
                    _writer.Write(filler);
                    break;
            }
        }

        public void WriteBuffer(byte[] value)
        {
            _writer.Write(value);
        }

        public void WriteLengthCodedNull()
        {
            _writer.Write((byte)251);
        }

        public void WriteLengthCodedNumber(long value)
        {
            //http://dev.mysql.com/doc/internals/en/overview.html#length-encoded-integer

            if (value < 251)//0xfb
            {
                _writer.Write((byte)value);
                return;
            }
            if (value > Packet.IEEE_754_BINARY_64_PRECISION)
            {
                throw new Exception("writeLengthCodedNumber: JS precision range exceeded, your" +
                  "number is > 53 bit: " + value);
            }

            if (value < 0xffff)
            {
                _writer.Write((byte)252); //encode  0xfc
                //// 16 Bit
                //this._buffer[this._offset++] = value & 0xff;
                //this._buffer[this._offset++] = (value >> 8) & 0xff;
                _writer.Write((byte)(value & 0xff));
                _writer.Write((byte)((value >> 8) & 0xff));
            }
            else if (value < 0xffffff)
            {
                _writer.Write((byte)253); //encode 0xfd
                _writer.Write((byte)(value & 0xff));
                _writer.Write((byte)((value >> 8) & 0xff));
                _writer.Write((byte)((value >> 16) & 0xff));
            }
            else
            {
                _writer.Write((byte)254); //encode 
                _writer.Write((byte)(value & 0xff));
                _writer.Write((byte)((value >> 8) & 0xff));
                _writer.Write((byte)((value >> 16) & 0xff));
                _writer.Write((byte)((value >> 24) & 0xff));
                //// Hack: Get the most significant 32 bit (JS bitwise operators are 32 bit)
                //value = value.toString(2);
                //value = value.substr(0, value.length - 32);
                //value = parseInt(value, 2); 
                _writer.Write((byte)((value >> 32) & 0xff));
                _writer.Write((byte)((value >> 40) & 0xff));
                _writer.Write((byte)((value >> 48) & 0xff));
                //// Set last byte to 0, as we can only support 53 bits in JS (see above)
                //this._buffer[this._offset++] = 0;
                _writer.Write((byte)0);
            }
        }
        public void WriteLengthCodedBuffer(byte[] value)
        {
            var bytes = value.Length;
            WriteLengthCodedNumber(bytes);
            _writer.Write(value);
        }
        public void WriteLengthCodedString(string value)
        {
            //if (value === null) {
            //  this.writeLengthCodedNumber(null);
            //  return;
            //}
            if (value == null)
            {
                WriteLengthCodedNull();
                return;
            }
            //value = (value === undefined)
            //  ? ''
            //  : String(value);

            //var bytes = Buffer.byteLength(value, 'utf-8');
            //this.writeLengthCodedNumber(bytes);

            //TODO: review here , always UTF8 ?
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
            _writer.Write(buff);
        }

        public void WriteString(string value)
        {
            byte[] buff = _encoding.GetBytes(value.ToCharArray());
            _writer.Write(buff);
        }

        public byte[] ToArray()
        {
            _writer.Flush();
            return _writer.ToArray();
        }
    }

    class MyBinaryWriter : IDisposable
    {
        BinaryWriter _writer;
        int _offset;
        MemoryStream _ms;
        public MyBinaryWriter()
        {
            _ms = new MemoryStream();
            _writer = new BinaryWriter(_ms);
        }
        public int Length
        {
            get { return _offset; }
        }
        public void Dispose()
        {
            Close();
        }
        public void Write(byte b)
        {
            _writer.Write(b);
            _offset++;
        }
        public void Write(byte[] bytes)
        {
            _writer.Write(bytes);
            _offset += bytes.Length;
        }
        public void WriteInt64(long value)
        {
            _writer.Write(value);
            _offset += 8;
        }
        public void WriteInt32(int value)
        {
            _writer.Write(value);
            _offset += 4;
        }
        public void WriteInt16(short value)
        {
            _writer.Write(value);
            _offset += 2;
        }
        public void WriteFloat(float value)
        {
            _writer.Write(value);
            _offset += 4;
        }
        public void WriteDouble(double value)
        {
            _writer.Write(value);
            _offset += 8;
        }

        public void Reset()
        {
            _writer.BaseStream.Position = 0;
            _offset = 0;
        }
        public void RewindWriteAndJumpBack(byte[] buffer, int offset)
        {
            var pos = _writer.BaseStream.Position;
            //rewidr
            _writer.BaseStream.Position = offset;
            //write
            _writer.Write(buffer);
            //jump back
            _writer.BaseStream.Position = pos;
            if (_offset < buffer.Length)
            {
                _offset = buffer.Length;
            }
        }
        public long OriginalStreamPosition
        {
            get { return _writer.BaseStream.Position; }
            set { _writer.BaseStream.Position = value; }
        }
        public void Close()
        {
            if (_writer != null)
            {
                _writer.Close();
                _writer = null;
            }
            if (_ms != null)
            {
                _ms.Close();
                _ms.Dispose();
                _ms = null;
            }
        }
        public void Flush()
        {
            _writer.Flush();
        }
        public byte[] ToArray()
        {
            var output = new byte[_offset];
            _ms.Position = 0;
            Read(output, 0, _offset);
            return output;
        }
        public void Read(byte[] buffer, int offset, int count)
        {
            _ms.Position = offset;
            var a = _ms.Read(buffer, 0, count);
        }
    }
}