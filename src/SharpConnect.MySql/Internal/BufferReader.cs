//MIT, 2016-2018, EngineKit 
using System;
namespace SharpConnect.MySql.Internal
{

    class BufferReader
    {
        byte[] _tmpBuffer = new byte[16];
        byte[] _srcBuffer;
        int _position;
        public BufferReader()
        {

        }
        public void SetSource(byte[] srcBuffer)
        {
            _srcBuffer = srcBuffer;
            _position = 0;
        }
        public int Position
        {
            get => _position;
            set => _position = value;

        }
        public byte ReadByte()
        {
            return _srcBuffer[_position++];
        }
        public uint ReadUInt32()
        {
            //little endian
            byte[] mybuffer = _srcBuffer;
            uint value = (uint)(mybuffer[_position] | mybuffer[_position + 1] << 8 |
                     mybuffer[_position + 2] << 16 | mybuffer[_position + 3] << 24);
            _position += 4;
            return value;
        }
        public decimal ReadDecimal()
        {
            System.Buffer.BlockCopy(_srcBuffer, _position, _tmpBuffer, 0, 16);
            _position += 16;
            return Convert.ToDecimal(_tmpBuffer);
        }
        public unsafe double ReadDouble()
        {
            double value = BitConverter.ToDouble(_srcBuffer, _position);
            _position += 8;
            return value;
        }
        public unsafe float ReadFloat()
        {
            float value = BitConverter.ToSingle(_srcBuffer, _position);
            _position += 4;
            return value;
        }
        public int ReadInt32()
        {
            byte[] mybuffer = _srcBuffer;
            int num = (mybuffer[_position + 0] | mybuffer[_position + 1] << 8 |
                 mybuffer[_position + 2] << 16 | mybuffer[_position + 3] << 24);
            _position += 4;
            return num;
        }
        public short ReadInt16()
        {
            byte[] mybuffer = _srcBuffer;
            short num = (short)(mybuffer[_position] | mybuffer[_position + 1] << 8);
            _position += 2;
            return num;
        }
        public ushort ReadUInt16()
        {

            byte[] mybuffer = _srcBuffer;
            ushort num = (ushort)(mybuffer[_position] | mybuffer[_position + 1] << 8);
            _position += 2;
            return num;
        }
        public long ReadInt64()
        {

            byte[] mybuffer = _srcBuffer;
            uint num = (uint)(((mybuffer[_position] | (mybuffer[_position + 1] << 8)) | (mybuffer[_position + 2] << 0x10)) | (mybuffer[_position + 3] << 0x18));
            uint num2 = (uint)(((mybuffer[_position + 4] | (mybuffer[_position + 5] << 8)) | (mybuffer[_position + 6] << 0x10)) | (mybuffer[_position + 7] << 0x18));
            _position += 8;
            return ((long)num2 << 0x20) | num;
        }
        public ulong ReadUInt64()
        {
            byte[] mybuffer = _srcBuffer;
            uint num = (uint)(((mybuffer[_position] | (mybuffer[_position + 1] << 8)) | (mybuffer[_position + 2] << 0x10)) | (mybuffer[_position + 3] << 0x18));
            uint num2 = (uint)(((mybuffer[_position + 4] | (mybuffer[_position + 5] << 8)) | (mybuffer[_position + 6] << 0x10)) | (mybuffer[_position + 7] << 0x18));
            return ((ulong)num2 << 0x20) | num;
        }

        public byte[] ReadBytes(int num)
        {
            byte[] buffer = new byte[num];
            System.Buffer.BlockCopy(_srcBuffer, _position, buffer, 0, num);
            _position += num;
            return buffer;
        }
    }


}