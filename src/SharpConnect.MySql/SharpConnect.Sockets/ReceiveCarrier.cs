//2010, CPOL, Stan Kirk  
using System;
using System.Net.Sockets;

namespace SharpConnect.Sockets
{
    class ReceiveCarrier
    {

        readonly SocketAsyncEventArgs recvSendArgs;
        readonly int recvStartBufferOffset;
        internal Action recvAction;

        public ReceiveCarrier(SocketAsyncEventArgs recvSendArgs)
        {
            this.recvSendArgs = recvSendArgs;
            this.recvStartBufferOffset = recvSendArgs.Offset;
        }
        public int BytesTransferred
        {
            get { return this.recvSendArgs.BytesTransferred; }
        }
        public void CopyTo(int srcIndex, byte[] destBuffer, int destIndex, int count)
        {
            Buffer.BlockCopy(recvSendArgs.Buffer,
                recvStartBufferOffset + srcIndex,
                destBuffer,
                destIndex, count);
        }
        /// <summary>
        /// copy all data to target
        /// </summary>
        /// <param name="targetBuffer"></param>
        public void CopyTo(byte[] destBuffer, int destIndex)
        {
            Buffer.BlockCopy(recvSendArgs.Buffer,
                recvStartBufferOffset,
                destBuffer,
                destIndex, BytesTransferred);
        }
        public byte[] ToArray()
        {
            byte[] buffer = new byte[this.BytesTransferred];
            CopyTo(buffer, 0);
            return buffer;
        }
        public byte ReadByte(int index)
        {
            return recvSendArgs.Buffer[this.recvStartBufferOffset + index];
        }
        public void ReadBytes(byte[] output, int start, int count)
        {
            Buffer.BlockCopy(recvSendArgs.Buffer,
                 recvStartBufferOffset + start,
                 output, 0, count);
        }
    }
}