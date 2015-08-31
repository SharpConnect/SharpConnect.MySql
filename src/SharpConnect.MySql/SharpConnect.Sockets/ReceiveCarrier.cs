//2010, CPOL, Stan Kirk  
using System;
using System.Net.Sockets;

namespace SharpConnect.Sockets
{
    class ReceiveCarrier
    {

        readonly SocketAsyncEventArgs _recvSendArgs;
        readonly int _recvStartBufferOffset;
        internal Action _recvAction;

        public ReceiveCarrier(SocketAsyncEventArgs recvSendArgs)
        {
            _recvSendArgs = recvSendArgs;
            _recvStartBufferOffset = recvSendArgs.Offset;
        }
        public int BytesTransferred
        {
            get { return _recvSendArgs.BytesTransferred; }
        }
        public void CopyTo(int srcIndex, byte[] destBuffer, int destIndex, int count)
        {
            Buffer.BlockCopy(_recvSendArgs.Buffer,
                _recvStartBufferOffset + srcIndex,
                destBuffer,
                destIndex, count);
        }
        /// <summary>
        /// copy all data to target
        /// </summary>
        /// <param name="targetBuffer"></param>
        public void CopyTo(byte[] destBuffer, int destIndex)
        {
            Buffer.BlockCopy(_recvSendArgs.Buffer,
                _recvStartBufferOffset,
                destBuffer,
                destIndex, BytesTransferred);
        }
        public byte[] ToArray()
        {
            var buffer = new byte[BytesTransferred];
            CopyTo(buffer, 0);
            return buffer;
        }
        public byte ReadByte(int index)
        {
            return _recvSendArgs.Buffer[_recvStartBufferOffset + index];
        }
        public void ReadBytes(byte[] output, int start, int count)
        {
            Buffer.BlockCopy(_recvSendArgs.Buffer,
                 _recvStartBufferOffset + start,
                 output, 0, count);
        }
    }
}