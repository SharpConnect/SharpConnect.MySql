//2010, CPOL, Stan Kirk 
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace SharpConnect.Sockets
{
    sealed class SocketAsyncEventArgsPool
    {
        //just for assigning an ID so we can watch our objects while testing.
        int nextTokenId = 0;
        Stack<SocketAsyncEventArgs> pool;

        public SocketAsyncEventArgsPool(int capacity)
        {

            //#if DEBUG
            //            if (dbugLOG.watchProgramFlow)   //for testing
            //            {
            //                dbugLOG.WriteLine("SocketAsyncEventArgsPool constructor");
            //            }
            //#endif

            this.pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        // The number of SocketAsyncEventArgs instances in the pool.         
        internal int Count
        {
            get { return this.pool.Count; }
        }

        internal int GetNewTokenId()
        {
            return Interlocked.Increment(ref nextTokenId);
        }

        // Removes a SocketAsyncEventArgs instance from the pool.
        // returns SocketAsyncEventArgs removed from the pool.
        internal SocketAsyncEventArgs Pop()
        {
            lock (this.pool)
            {
                return this.pool.Pop();
            }
        }

        // Add a SocketAsyncEventArg instance to the pool. 
        // "item" = SocketAsyncEventArgs instance to add to the pool.
        internal void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null");
            }
            lock (this.pool)
            {
                this.pool.Push(item);
            }
        }
    }
}
