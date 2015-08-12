//2010, CPOL, Stan Kirk 
 
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace SharpConnect.Sockets
{
    class BufferManager
    {

        int totalBytesInBufferBlock;
        byte[] bufferBlock;
        Stack<int> freeIndexPool;
        int currentIndex;
        int totalBufferBytesInEachSocketAsyncEventArgs;

        public BufferManager(int totalBytes, int totalBufferBytesInEachSocketAsyncEventArgs)
        {
            totalBytesInBufferBlock = totalBytes;
            this.currentIndex = 0;
            this.totalBufferBytesInEachSocketAsyncEventArgs = totalBufferBytesInEachSocketAsyncEventArgs;
            this.freeIndexPool = new Stack<int>();

            // Allocate one large byte buffer block, which all I/O operations will 
            //use a piece of. This gaurds against memory fragmentation.
            InitBuffer();
        }

        // Allocates buffer space used by the buffer pool
        void InitBuffer()
        {
            // Create one large buffer block.
            this.bufferBlock = new byte[totalBytesInBufferBlock];
        }


        internal bool SetBufferTo(SocketAsyncEventArgs args)
        {

            if (this.freeIndexPool.Count > 0)
            {
                //This if-statement is only true if you have called the FreeBuffer
                //method previously, which would put an offset for a buffer space 
                //back into this stack.
                args.SetBuffer(this.bufferBlock, this.freeIndexPool.Pop(), this.totalBufferBytesInEachSocketAsyncEventArgs);
            }
            else
            {
                //Inside this else-statement is the code that is used to set the 
                //buffer for each SAEA object when the pool of SAEA objects is built
                //in the Init method.
                if ((totalBytesInBufferBlock - this.totalBufferBytesInEachSocketAsyncEventArgs) < this.currentIndex)
                {
                    return false;
                }
                args.SetBuffer(this.bufferBlock, this.currentIndex, this.totalBufferBytesInEachSocketAsyncEventArgs);
                this.currentIndex += this.totalBufferBytesInEachSocketAsyncEventArgs;
            }
            return true;
        }

        // Removes the buffer from a SocketAsyncEventArg object.   This frees the
        // buffer back to the buffer pool. Try NOT to use the FreeBuffer method,
        // unless you need to destroy the SAEA object, or maybe in the case
        // of some exception handling. Instead, on the server
        // keep the same buffer space assigned to one SAEA object for the duration of
        // this app's running.
        internal void FreeBuffer(SocketAsyncEventArgs args)
        {
            this.freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }

    }
}
