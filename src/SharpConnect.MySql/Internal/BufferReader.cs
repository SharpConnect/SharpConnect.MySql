//MIT, 2016-2018, EngineKit 
using System;
namespace SharpConnect.MySql.Internal
{

    class BufferReader
    {
        byte[] tmpBuffer = new byte[16];
        byte[] srcBuffer;
        int position;
        public BufferReader()
        {

        }
        public void SetSource(byte[] srcBuffer)
        {
            this.srcBuffer = srcBuffer;
            position = 0;
        }
        public int Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }
        public byte ReadByte()
        {
            return srcBuffer[position++];
        }
        public uint ReadUInt32()
        {
            //little endian
            byte[] mybuffer = srcBuffer;
            uint value = (uint)(mybuffer[position] | mybuffer[position + 1] << 8 |
                     mybuffer[position + 2] << 16 | mybuffer[position + 3] << 24);
            position += 4;
            return value;
        }
        public decimal ReadDecimal()
        {
            System.Buffer.BlockCopy(srcBuffer, position, tmpBuffer, 0, 16);
            position += 16;
            return Convert.ToDecimal(tmpBuffer);
        }
        public unsafe double ReadDouble()
        {

            byte[] mybuffer = srcBuffer;

            uint num = (uint)(((mybuffer[position + 0] | (mybuffer[position + 1] << 8)) | (mybuffer[position + 2] << 0x10)) | (mybuffer[position + 3] << 0x18));
            uint num2 = (uint)(((mybuffer[position + 4] | (mybuffer[position + 5] << 8)) | (mybuffer[position + 6] << 0x10)) | (mybuffer[position + 7] << 0x18));
            ulong num3 = (num2 << 0x20) | num;
            position += 8;
            return *(((double*)&num3));

        }
        public unsafe float ReadFloat()
        {
            byte[] mybuffer = srcBuffer;
            uint num = (uint)(((mybuffer[position + 0] | (mybuffer[position + 1] << 8)) | (mybuffer[position + 2] << 0x10)) | (mybuffer[position + 3] << 0x18));
            position += 4;
            return *(((float*)&num));
        }
        public int ReadInt32()
        {
            byte[] mybuffer = srcBuffer;
            int num = (mybuffer[position + 0] | mybuffer[position + 1] << 8 |
                 mybuffer[position + 2] << 16 | mybuffer[position + 3] << 24);
            position += 4;
            return num;
        }
        public short ReadInt16()
        {
            byte[] mybuffer = srcBuffer;
            short num = (short)(mybuffer[position] | mybuffer[position + 1] << 8);
            position += 2;
            return num;
        }
        public ushort ReadUInt16()
        {

            byte[] mybuffer = srcBuffer;
            ushort num = (ushort)(mybuffer[position] | mybuffer[position + 1] << 8);
            position += 2;
            return num;
        }
        public long ReadInt64()
        {

            byte[] mybuffer = srcBuffer;
            uint num = (uint)(((mybuffer[position] | (mybuffer[position + 1] << 8)) | (mybuffer[position + 2] << 0x10)) | (mybuffer[position + 3] << 0x18));
            uint num2 = (uint)(((mybuffer[position + 4] | (mybuffer[position + 5] << 8)) | (mybuffer[position + 6] << 0x10)) | (mybuffer[position + 7] << 0x18));
            position += 8;
            return ((long)num2 << 0x20) | num;
        }
        public ulong ReadUInt64()
        {
            byte[] mybuffer = srcBuffer;
            uint num = (uint)(((mybuffer[position] | (mybuffer[position + 1] << 8)) | (mybuffer[position + 2] << 0x10)) | (mybuffer[position + 3] << 0x18));
            uint num2 = (uint)(((mybuffer[position + 4] | (mybuffer[position + 5] << 8)) | (mybuffer[position + 6] << 0x10)) | (mybuffer[position + 7] << 0x18));
            return ((ulong)num2 << 0x20) | num;
        }

        public byte[] ReadBytes(int num)
        {
            byte[] buffer = new byte[num];
            System.Buffer.BlockCopy(srcBuffer, position, buffer, 0, num);
            position += num;
            return buffer;
        }
    }

   
}