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
using System.Text;
using System.IO;

namespace MySqlPacket
{
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
            int maxPacketLength = MAX_PACKET_LENGTH;
            if (maxAllowedLength <= MAX_PACKET_LENGTH)
            {
                maxPacketLength = (int)maxAllowedLength - 4;//-4 bytes for header
            }

            long curPacketLength = CurrentPacketLength();

            dbugConsole.WriteLine("Current Packet Length = " + curPacketLength);

            int packets = (int)(curPacketLength / maxPacketLength) + 1;
            if (packets == 1)
            {
                byte[] encodeData = new byte[4];
                EncodeUnsignedNumber(encodeData, 0, 3, header.Length);
                encodeData[3] = header.PacketNumber;
                writer.RewindWriteAtOffset(encodeData, (int)startPacketPosition);
            }
            else
            {
                //>1 
                //  this._buffer = new Buffer(this._buffer.length + packets * 4);
                //  for (var packet = 0; packet < packets; packet++) { 
                //  }
                int startContentPos = (int)(startPacketPosition + 4);
                int offset = 0;
                byte startPacketNum = header.PacketNumber;
                byte[] currentPacketBuff = new byte[maxPacketLength];
                byte[] allBuffer = new byte[(curPacketLength - 4) + (packets * 4)];
                for (int packet = 0; packet < packets; packet++)
                {
                    //    this._offset = packet * (MAX_PACKET_LENGTH + 4);
                    offset = packet * maxPacketLength + startContentPos;
                    //    var isLast = (packet + 1 === packets);
                    //    var packetLength = (isLast)
                    //      ? buffer.length % MAX_PACKET_LENGTH
                    //      : MAX_PACKET_LENGTH;
                    int packetLength = (packet + 1 == packets)
                        ? (int)((curPacketLength - 4) % maxPacketLength)
                        : maxPacketLength;
                    //    var packetNumber = parser.incrementPacketNumber();

                    //    this.writeUnsignedNumber(3, packetLength);
                    //    this.writeUnsignedNumber(1, packetNumber);

                    //    var start = packet * MAX_PACKET_LENGTH;
                    //    var end   = start + packetLength;

                    //    this.writeBuffer(buffer.slice(start, end));
                    var start = packet * (maxPacketLength + 4);

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