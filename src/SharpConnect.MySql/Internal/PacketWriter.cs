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
        Encoding encoding;

        byte[] headerBuffer = new byte[4];//reusable header buffer

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

        public void Reset()
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
            //int maxPacketLength = MAX_PACKET_LENGTH;

            long curPacketLength = CurrentPacketLength();

            dbugConsole.WriteLine("Current Packet Length = " + curPacketLength);

            int packets = (int)(curPacketLength / MAX_PACKET_LENGTH) + 1;
            if (packets == 1)
            {
                if (header.Length > maxAllowedLength)
                {
                    throw new Exception("Packet for query is too larger than MAX_ALLOWED_LENGTH");
                }
                EncodeUnsignedNumber0_3(headerBuffer, header.Length);
                headerBuffer[3] = header.PacketNumber;
                writer.RewindAndWriteAt(headerBuffer, (int)startPacketPosition);
            }
            else //>1 
            {
                long allDataLength = (curPacketLength - 4) + (packets * 4);
                if (allDataLength > maxAllowedLength)
                {
                    throw new Exception("Packet for query is too larger than MAX_ALLOWED_LENGTH");
                }
                byte[] allBuffer = new byte[allDataLength];
                int startContentPos = (int)(startPacketPosition + 4);
                int offset = 0;
                byte startPacketNum = header.PacketNumber;
                byte[] currentPacketBuff = new byte[MAX_PACKET_LENGTH];
                
                for (int packet = 0; packet < packets; packet++)
                {
                    //    this._offset = packet * (MAX_PACKET_LENGTH + 4);
                    offset = packet * MAX_PACKET_LENGTH + startContentPos;
                    //    var isLast = (packet + 1 === packets);
                    //    var packetLength = (isLast)
                    //      ? buffer.length % MAX_PACKET_LENGTH
                    //      : MAX_PACKET_LENGTH;
                    int packetLength = (packet + 1 == packets)
                        ? (int)((curPacketLength - 4) % MAX_PACKET_LENGTH)
                        : MAX_PACKET_LENGTH;
                    //    var packetNumber = parser.incrementPacketNumber();

                    //    this.writeUnsignedNumber(3, packetLength);
                    //    this.writeUnsignedNumber(1, packetNumber);

                    //    var start = packet * MAX_PACKET_LENGTH;
                    //    var end   = start + packetLength;

                    //    this.writeBuffer(buffer.slice(start, end));
                    int start = packet * (MAX_PACKET_LENGTH + 4);//+4 for add header

                    //byte[] encodeData = new byte[4];
                    EncodeUnsignedNumber0_3(headerBuffer, (uint)packetLength);
                    headerBuffer[3] = startPacketNum++;

                    headerBuffer.CopyTo(allBuffer, start);
                    writer.RewindAndWriteAt(headerBuffer, start);
                    //startPacketNum = 0;
                    if (packetLength < currentPacketBuff.Length)
                    {
                        currentPacketBuff = new byte[packetLength];
                    }
                    writer.Read(currentPacketBuff, offset, packetLength);
                    currentPacketBuff.CopyTo(allBuffer, start + 4);
                }
                writer.RewindAndWriteAt(allBuffer, (int)startPacketPosition);
            }
        }
        public long CurrentPacketLength()
        {
            return writer.OriginalStreamPosition - startPacketPosition;
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
        public void WriteUnsigned1(uint value)
        {
            writer.Write((byte)(value & 0xff));
        }
        public void WriteUnsigned2(uint value)
        {
            writer.Write((byte)(value & 0xff));
            writer.Write((byte)((value >> 8) & 0xff));
        }
        public void WriteUnsigned3(uint value)
        {
            writer.Write((byte)(value & 0xff));
            writer.Write((byte)((value >> 8) & 0xff));
            writer.Write((byte)((value >> 16) & 0xff));
        }
        public void WriteUnsigned4(uint value)
        {
            writer.Write((byte)(value & 0xff));
            writer.Write((byte)((value >> 8) & 0xff));
            writer.Write((byte)((value >> 16) & 0xff));
            writer.Write((byte)((value >> 24) & 0xff));
        }
        public void WriteUnsignedNumber(int length, uint value)
        {
            switch (length)
            {
                case 0: break;
                case 1:

                    writer.Write((byte)(value & 0xff));
                    break;
                case 2:

                    writer.Write((byte)(value & 0xff));
                    writer.Write((byte)((value >> 8) & 0xff));
                    break;
                case 3:

                    writer.Write((byte)(value & 0xff));
                    writer.Write((byte)((value >> 8) & 0xff));
                    writer.Write((byte)((value >> 16) & 0xff));
                    break;
                case 4:

                    writer.Write((byte)(value & 0xff));
                    writer.Write((byte)((value >> 8) & 0xff));
                    writer.Write((byte)((value >> 16) & 0xff));
                    writer.Write((byte)((value >> 24) & 0xff));
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

        //static void EncodeUnsignedNumber(byte[] outputBuffer, int start, int length, uint value)
        //{
        //    int lim = start + length;
        //    for (var i = start; i < lim; i++)
        //    {
        //        outputBuffer[i] = (byte)((value >> (i * 8)) & 0xff);
        //    }
        //}
        static void EncodeUnsignedNumber0_3(byte[] outputBuffer, uint value)
        {
            //start at 0
            //length= 3
            outputBuffer[0] = (byte)(value & 0xff);
            outputBuffer[1] = (byte)((value >> 8) & 0xff);
            outputBuffer[2] = (byte)((value >> 24) & 0xff);
        }

        public void WriteByte(byte value)
        {
            writer.Write(value);
        }

        public void WriteInt64(long value)
        {
            writer.WriteInt64(value);
        }

        public void WriteFloat(float value)
        {
            writer.WriteFloat(value);
        }

        public void WriteDouble(double value)
        {
            writer.WriteDouble(value);
        }

        public void WriteFiller(int length)
        {
            switch (length)
            {
                case 0:
                    break;
                case 1:
                    writer.Write((byte)0);//1
                    break;
                case 2:
                    writer.WriteInt16(0);//2
                    break;
                case 3:
                    writer.WriteInt16(0);//2
                    writer.Write((byte)0);//1
                    break;
                case 4:
                    writer.WriteInt32(0);//4
                    break;
                default:
                    //else
                    byte[] filler = new byte[length];
                    writer.Write(filler);
                    break;
            }
        }

        public void WriteBuffer(byte[] value)
        {
            writer.Write(value);
        }

        public void WriteLengthCodedNull()
        {
            writer.Write((byte)251);
        }

        public void WriteLengthCodedNumber(long value)
        {

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

                writer.Write((byte)252); //endcode

                //// 16 Bit
                //this._buffer[this._offset++] = value & 0xff;
                //this._buffer[this._offset++] = (value >> 8) & 0xff;
                writer.Write((byte)(value & 0xff));
                writer.Write((byte)((value >> 8) & 0xff));
            }
            else if (value <= BIT_24)
            {
                writer.Write((byte)253); //encode

                writer.Write((byte)(value & 0xff));
                writer.Write((byte)((value >> 8) & 0xff));
                writer.Write((byte)((value >> 16) & 0xff));
            }
            else
            {
                writer.Write((byte)254); //encode 

                writer.Write((byte)(value & 0xff));
                writer.Write((byte)((value >> 8) & 0xff));
                writer.Write((byte)((value >> 16) & 0xff));
                writer.Write((byte)((value >> 24) & 0xff));

                //// Hack: Get the most significant 32 bit (JS bitwise operators are 32 bit)
                //value = value.toString(2);
                //value = value.substr(0, value.length - 32);
                //value = parseInt(value, 2); 
                writer.Write((byte)((value >> 32) & 0xff));
                writer.Write((byte)((value >> 40) & 0xff));
                writer.Write((byte)((value >> 48) & 0xff));

                //// Set last byte to 0, as we can only support 53 bits in JS (see above)
                //this._buffer[this._offset++] = 0;
                writer.Write((byte)0);
            }

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
        public void WriteInt64(long value)
        {
            writer.Write(value);
            offset += 8;
        }
        public void WriteInt32(int value)
        {
            writer.Write(value);
            offset += 4;
        }
        public void WriteInt16(short value)
        {
            writer.Write(value);
            offset += 2;
        }
        public void WriteFloat(float value)
        {
            writer.Write(value);
            offset += 4;
        }
        public void WriteDouble(double value)
        {
            writer.Write(value);
            offset += 8;
        }

        public void Reset()
        {
            writer.BaseStream.Position = 0;
            offset = 0;
        }
        public void RewindAndWriteAt(byte[] buffer, int offset)
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