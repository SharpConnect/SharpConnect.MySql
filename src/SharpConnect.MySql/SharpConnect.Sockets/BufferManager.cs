//2010, CPOL, Stan Kirk 


using System.Collections.Generic;
using System.Net.Sockets;
namespace SharpConnect.Sockets
{
    class BufferManager
    {
        int _totalBytesInBufferBlock;
        byte[] _bufferBlock;
        Stack<int> _freeIndexPool;
        int _currentIndex;
        int _totalBufferBytesInEachSocketAsyncEventArgs;
        public BufferManager(int totalBytes, int totalBufferBytesInEachSocketAsyncEventArgs)
        {
            _totalBytesInBufferBlock = totalBytes;
            _currentIndex = 0;
            _totalBufferBytesInEachSocketAsyncEventArgs = totalBufferBytesInEachSocketAsyncEventArgs;
            _freeIndexPool = new Stack<int>();
            // Allocate one large byte buffer block, which all I/O operations will 
            //use a piece of. This gaurds against memory fragmentation.
            InitBuffer();
        }

        // Allocates buffer space used by the buffer pool
        void InitBuffer()
        {
            // Create one large buffer block.
            _bufferBlock = new byte[_totalBytesInBufferBlock];
        }


        internal bool SetBufferTo(SocketAsyncEventArgs args)
        {
            if (_freeIndexPool.Count > 0)
            {
                //This if-statement is only true if you have called the FreeBuffer
                //method previously, which would put an offset for a buffer space 
                //back into this stack.
                args.SetBuffer(_bufferBlock, _freeIndexPool.Pop(), _totalBufferBytesInEachSocketAsyncEventArgs);
            }
            else
            {
                //Inside this else-statement is the code that is used to set the 
                //buffer for each SAEA object when the pool of SAEA objects is built
                //in the Init method.
                if ((_totalBytesInBufferBlock - _totalBufferBytesInEachSocketAsyncEventArgs) < _currentIndex)
                {
                    return false;
                }
                args.SetBuffer(_bufferBlock, _currentIndex, _totalBufferBytesInEachSocketAsyncEventArgs);
                _currentIndex += _totalBufferBytesInEachSocketAsyncEventArgs;
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
            _freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
