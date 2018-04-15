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
using System.IO;
using System.Text;
namespace SharpConnect.MySql.Internal
{
    class MySqlStreamWriter
    {
        MyBinaryWriter _writer;
        byte _packetNumber;
        long _startPacketPosition;

        int _serverMaxDataLength = Packet.MAX_PACKET_LENGTH;
        Encoding _encoding;
        byte[] _headerBuffer = new byte[4];//reusable header buffer
        const int BIT_16 = (int)1 << 16;//(int)Math.Pow(2, 16);
        const int BIT_24 = (int)1 << 24;//(int)Math.Pow(2, 24);
        public MySqlStreamWriter(Encoding encoding)
        {
            _writer = new MyBinaryWriter();
            _writer.Reset();
            _packetNumber = 0;
            _startPacketPosition = 0;
            _encoding = encoding;
        }

        ~MySqlStreamWriter()
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
        public void SetMaxAllowedPacket(int max)
        {
            _serverMaxDataLength = max;
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

            long totalPacketLength = OnlyPacketContentLength + 4;
#if DEBUG
            //SharpConnect.Internal.dbugConsole.WriteLine("Current Packet Length = " + totalPacketLength);
#endif
            //TODO: review MAX_PACKET_LENGTH here ****
            //it should be 
            //int packetCount = (int)((totalPacketLength - 4) / _maxAllowedLength) + 1;//-4 bytes of reserve header
            if (totalPacketLength > _serverMaxDataLength)
            {
                throw new Exception("Packet for query is too larger than MAX_ALLOWED_LENGTH");
            }
            if (header.ContentLength > Packet.MAX_PACKET_LENGTH)
            {
                throw new Exception("Packet for query is too larger than MAX_ALLOWED_LENGTH");
            }
            WriteEncodedUnsignedNumber0_3(_headerBuffer, header.ContentLength);
            _headerBuffer[3] = header.PacketNumber;
            _writer.RewindWriteAndJumpBack(_headerBuffer, (int)_startPacketPosition);
        }

        public uint OnlyPacketContentLength
        {
            get
            {
                return ((uint)(_writer.OriginalStreamPosition - _startPacketPosition)) - 4;
            }
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
                default:
                    throw new NotSupportedException();
            }
        }

        public static void Write(float value, byte[] outputBuffer)
        {
            //from microsoft's reference source
            //with MIT license
            unsafe
            {
                uint TmpValue = *(uint*)&value;
                outputBuffer[0] = (byte)TmpValue;
                outputBuffer[1] = (byte)(TmpValue >> 8);
                outputBuffer[2] = (byte)(TmpValue >> 16);
                outputBuffer[3] = (byte)(TmpValue >> 24);
            }
        }
        public static void Write(double value, byte[] outputBuffer)
        {
            //from microsoft's reference source
            //with MIT license
            unsafe
            {
                ulong TmpValue = *(ulong*)&value;
                outputBuffer[0] = (byte)TmpValue;
                outputBuffer[1] = (byte)(TmpValue >> 8);
                outputBuffer[2] = (byte)(TmpValue >> 16);
                outputBuffer[3] = (byte)(TmpValue >> 24);
                outputBuffer[4] = (byte)(TmpValue >> 32);
                outputBuffer[5] = (byte)(TmpValue >> 40);
                outputBuffer[6] = (byte)(TmpValue >> 48);
                outputBuffer[7] = (byte)(TmpValue >> 56);
            }
        }
        public static void Write(ulong value, byte[] outputBuffer)
        {
            //from microsoft's reference source
            //with MIT license
            outputBuffer[0] = (byte)value;
            outputBuffer[1] = (byte)(value >> 8);
            outputBuffer[2] = (byte)(value >> 16);
            outputBuffer[3] = (byte)(value >> 24);
            outputBuffer[4] = (byte)(value >> 32);
            outputBuffer[5] = (byte)(value >> 40);
            outputBuffer[6] = (byte)(value >> 48);
            outputBuffer[7] = (byte)(value >> 56);
        }

        public static int WriteUnsignedNumber(int length, uint value, byte[] outputBuffer)
        {
            switch (length)
            {
                case 0: return 0;
                case 1:
                    outputBuffer[0] = ((byte)(value & 0xff));
                    return 1;
                case 2:
                    outputBuffer[0] = ((byte)(value & 0xff));
                    outputBuffer[1] = ((byte)((value >> 8) & 0xff));
                    return 2;
                case 3:
                    outputBuffer[0] = ((byte)(value & 0xff));
                    outputBuffer[1] = ((byte)((value >> 8) & 0xff));
                    outputBuffer[2] = ((byte)((value >> 16) & 0xff));
                    return 3;
                case 4:
                    outputBuffer[0] = ((byte)(value & 0xff));
                    outputBuffer[1] = ((byte)((value >> 8) & 0xff));
                    outputBuffer[2] = ((byte)((value >> 16) & 0xff));
                    outputBuffer[3] = ((byte)((value >> 24) & 0xff));
                    return 4;
                default:
                    throw new NotSupportedException();
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
        public void WriteBuffer(byte[] value, int start, int len)
        {
            _writer.Write(value, start, len);
        }
        public void WriteLengthCodedNull()
        {
            _writer.Write((byte)251);
        }
        public int GenerateEncodeLengthNumber(long value, byte[] outputBuffer)
        {

            if (value < 251)//0xfb
            {
                outputBuffer[0] = (byte)value;
                return 1;
            }
            if (value > Packet.IEEE_754_BINARY_64_PRECISION)
            {
                throw new Exception("writeLengthCodedNumber: JS precision range exceeded, your" +
                  "number is > 53 bit: " + value);
            }

            if (value < 0xffff)
            {
                outputBuffer[0] = (byte)252; //encode  0xfc
                outputBuffer[1] = (byte)(value & 0xff); //encode  0xfc
                outputBuffer[2] = (byte)((value >> 8) & 0xff); //encode  0xfc
                return 3;

                //_writer.Write((byte)252); //encode  0xfc
                ////// 16 Bit
                ////this._buffer[this._offset++] = value & 0xff;
                ////this._buffer[this._offset++] = (value >> 8) & 0xff;
                //_writer.Write((byte)(value & 0xff));
                //_writer.Write((byte)((value >> 8) & 0xff));
            }
            else if (value < 0xffffff)
            {
                outputBuffer[0] = (byte)253; //encode  0xfc
                outputBuffer[1] = (byte)(value & 0xff); //encode  0xfc
                outputBuffer[2] = (byte)((value >> 8) & 0xff); //encode  0xfc
                outputBuffer[3] = (byte)((value >> 16) & 0xff); //encode  0xfcs
                return 4;

                //_writer.Write((byte)253); //encode 0xfd
                //_writer.Write((byte)(value & 0xff));
                //_writer.Write((byte)((value >> 8) & 0xff));
                //_writer.Write((byte)((value >> 16) & 0xff));
            }
            else
            {
                outputBuffer[0] = ((byte)254); //encode 
                outputBuffer[1] = ((byte)(value & 0xff));
                outputBuffer[2] = ((byte)((value >> 8) & 0xff));
                outputBuffer[3] = ((byte)((value >> 16) & 0xff));
                outputBuffer[4] = ((byte)((value >> 24) & 0xff));
                //// Hack: Get the most significant 32 bit (JS bitwise operators are 32 bit)
                //value = value.toString(2);
                //value = value.substr(0, value.length - 32);
                //value = parseInt(value, 2); 
                outputBuffer[5] = ((byte)((value >> 32) & 0xff));
                outputBuffer[6] = ((byte)((value >> 40) & 0xff));
                outputBuffer[7] = ((byte)((value >> 48) & 0xff));
                //// Set last byte to 0, as we can only support 53 bits in JS (see above)
                //this._buffer[this._offset++] = 0;
                outputBuffer[8] = ((byte)0);
                return 9;

                //_writer.Write((byte)254); //encode 
                //_writer.Write((byte)(value & 0xff));
                //_writer.Write((byte)((value >> 8) & 0xff));
                //_writer.Write((byte)((value >> 16) & 0xff));
                //_writer.Write((byte)((value >> 24) & 0xff));
                ////// Hack: Get the most significant 32 bit (JS bitwise operators are 32 bit)
                ////value = value.toString(2);
                ////value = value.substr(0, value.length - 32);
                ////value = parseInt(value, 2); 
                //_writer.Write((byte)((value >> 32) & 0xff));
                //_writer.Write((byte)((value >> 40) & 0xff));
                //_writer.Write((byte)((value >> 48) & 0xff));
                ////// Set last byte to 0, as we can only support 53 bits in JS (see above)
                ////this._buffer[this._offset++] = 0;
                //_writer.Write((byte)0);
            }
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
            byte[] buff = GetEncodeBytes(value.ToCharArray());
            _writer.Write(buff);
        }
        public void WriteBinaryString(byte[] binaryEncodedString)
        {
            _writer.Write(binaryEncodedString);
        }
        public void WriteBinaryString(byte[] binaryEncodedString, int start, int len)
        {
            _writer.Write(binaryEncodedString, start, len);
        }
        public byte[] GetEncodeBytes(char[] buffer)
        {
            return _encoding.GetBytes(buffer);
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
        public void Write(byte[] bytes, int srcIndex, int len)
        {
            _writer.Write(bytes, srcIndex, len);
            _offset += len;
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
            //rewide
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
#if NET20
                _writer.Close();
#endif
                _writer = null;
            }
            if (_ms != null)
            {
#if NET20
                _ms.Close();
#endif
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