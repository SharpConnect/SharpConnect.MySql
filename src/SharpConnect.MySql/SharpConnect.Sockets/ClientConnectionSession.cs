//2010, CPOL, Stan Kirk  

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
namespace SharpConnect.Sockets
{
    //--------------------------------------------------
    ////from http://www.codeproject.com/Articles/83102/C-SocketAsyncEventArgs-High-Performance-Socket-Cod
    //and from
    //https://msdn.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs%28v=vs.90%29.aspx
    //--------------------------------------------------

    enum EndReceiveState
    {
        Error,
        NoMoreData,
        ContinueRead,
        Complete
    }
    enum EndSendState
    {
        Error,
        Continue,
        Complete
    }



    static class GlobalSessionNumber
    {
        internal static int s_mainTransMissionId = 10000;
        internal static int s_mainSessionId = 1000000000;
        internal static int s_maxSimultaneousClientsThatWereConnected;
    }

    abstract class ClientConnectionSession
    {
        //The session ID correlates with all the data sent in a connected session.
        //It is different from the transmission ID in the DataHolder, which relates
        //to one TCP message. A connected session could have many messages, if you
        //set up your app to allow it.
        int _sessionId;
        readonly SocketAsyncEventArgs _recvSendArgs;
        //recv
        ReceiveCarrier _recvCarrier;
        readonly int _startBufferOffset;
        readonly int _recvBufferSize;
        //send
        readonly int _initSentOffset;
        readonly int _sendBufferSize;
        int _sendingTargetBytes;
        int _sendingTransferredBytes;
        byte[] _currentSendingData = null;
        Queue<byte[]> _sendingQueue = new Queue<byte[]>();
        protected Func<ReceiveCarrier, EndReceiveState> _recvHandler;
        protected Func<ReceiveCarrier, EndSendState> _sendHandler;
        public ClientConnectionSession(SocketAsyncEventArgs recvSendArgs,
            int recvBufferSize, int sendBufferSize)
        {
            _recvCarrier = new ReceiveCarrier(recvSendArgs);
            _startBufferOffset = recvSendArgs.Offset;
            _recvSendArgs = recvSendArgs;
            _recvBufferSize = recvBufferSize;
            _initSentOffset = _startBufferOffset + recvBufferSize;
            _sendBufferSize = sendBufferSize;
            //this.KeepAlive = true;
            //Attach the SocketAsyncEventArgs object
            //to its event handler. Since this SocketAsyncEventArgs object is 
            //used for both receive and send operations, whenever either of those 
            //completes, the IO_Completed method will be called.
            recvSendArgs.Completed += ReceiveSendIO_Completed;
        }

        protected abstract void ResetRecvBuffer();
        /// <summary>
        /// receive data
        /// </summary>
        /// <param name="recvCarrier"></param>
        /// <returns>return true if finished</returns>
        protected abstract EndReceiveState ProtocolRecvBuffer(ReceiveCarrier recvCarrier);
        EndReceiveState EndReceive()
        {
            if (_recvSendArgs.SocketError != SocketError.Success)
            {
                this.ResetRecvBuffer();
                //Jump out of the ProcessReceive method.
                return EndReceiveState.Error;
            }
            if (_recvSendArgs.BytesTransferred == 0)
            {
                // If no data was received, close the connection. This is a NORMAL
                // situation that shows when the client has finished sending data.
                this.ResetRecvBuffer();
                return EndReceiveState.NoMoreData;
            }

            //--------------------
            return this.ProtocolRecvBuffer(this._recvCarrier);
        }


        internal void StartReceive(Func<ReceiveCarrier, EndReceiveState> recvHandler)
        {
            this._recvHandler = recvHandler;
            this._recvSendArgs.SetBuffer(this._startBufferOffset, this._recvBufferSize);
            if (!_recvSendArgs.AcceptSocket.ReceiveAsync(_recvSendArgs))
            {
                ProcessReceive();
            }
        }

        void CloseClientSocket()
        {
            //release SAEA
            //close socket both side
            try
            {
                _recvSendArgs.AcceptSocket.Shutdown(SocketShutdown.Both);
            }
            // throws if socket was already closed
            catch (Exception)
            {
                //
                //dbugSendLog(e, "CloseClientSocket, Shutdown catch");
            }
            _recvSendArgs.AcceptSocket.Close();
        }
        void ProcessReceive()
        {
            switch (this.EndReceive())
            {
                case EndReceiveState.Error:
                    //dbugRecvLog(recvSendArg, "ProcessReceive ERROR, receiveSendToken");
                    CloseClientSocket();
                    return;
                case EndReceiveState.NoMoreData:
                    //dbugRecvLog(recvSendArg, "ProcessReceive NO DATA");
                    CloseClientSocket();
                    return;
                case EndReceiveState.ContinueRead:
                    //continue read
                    StartReceive(this._recvHandler);  //again
                    return;
                case EndReceiveState.Complete:

                    var conCompleteAction = this._recvCarrier._recvAction;
                    if (conCompleteAction != null)
                    {
                        this._recvCarrier._recvAction = null;//clear
                        conCompleteAction();
                    }
                    return;
            }
        }

        //--------------------------------------------------------------------------------
        internal void SetDataToSend(byte[] dataToSend, int count)
        {
            if (_currentSendingData == null)
            {
                _currentSendingData = dataToSend;
                _sendingTargetBytes = count;
            }
            else
            {
                //add to queue
                _sendingQueue.Enqueue(dataToSend);
            }
        }
        internal void StartSend(Func<ReceiveCarrier, EndSendState> sendHandler)
        {
            this._sendHandler = sendHandler;
            StartSend();
        }
        internal void StartSend()
        {
            int remaining = _sendingTargetBytes - _sendingTransferredBytes;
            if (remaining <= _sendBufferSize)
            {
                _recvSendArgs.SetBuffer(_initSentOffset, remaining);
                //*** copy from src to dest
                Buffer.BlockCopy(_currentSendingData, //src
                     _sendingTransferredBytes,
                     _recvSendArgs.Buffer, //dest
                     _initSentOffset,
                     remaining);
            }
            else
            {
                //We cannot try to set the buffer any larger than its size.
                //So since receiveSendToken.sendBytesRemainingCount > BufferSize, we just
                //set it to the maximum size, to send the most data possible.
                _recvSendArgs.SetBuffer(_initSentOffset, _sendBufferSize);
                //Copy the bytes to the buffer associated with this SAEA object.
                Buffer.BlockCopy(_currentSendingData,
                    _sendingTransferredBytes,
                    _recvSendArgs.Buffer,
                    _initSentOffset,
                    _sendBufferSize);
                //We'll change the value of sendUserToken.sendBytesRemainingCount
                //in the ProcessSend method.
            }


            if (!_recvSendArgs.AcceptSocket.SendAsync(_recvSendArgs))
            {
                //dbugSendLog(connSession.dbugGetAsyncSocketEventArgs(), "start send( not async)");
                ProcessSend();
            }
        }

        void ProcessSend()
        {
            switch (EndSend())
            {
                case EndSendState.Error:
                    CloseClientSocket();
                    return;
                case EndSendState.Continue:
                    // So let's loop back to StartSend().
                    StartSend();
                    return;
                case EndSendState.Complete:
                    //finished send
                    if (_sendHandler != null)
                    {
                        _sendHandler(this._recvCarrier);
                    }
                    return;
            }
        }
        EndSendState EndSend()
        {
            if (_recvSendArgs.SocketError == SocketError.Success)
            {
                //success !                 

                _sendingTransferredBytes += _recvSendArgs.BytesTransferred;
                if ((_sendingTargetBytes - _sendingTransferredBytes) <= 0)
                {
                    //check if no other data in chuck 
                    if (_sendingQueue.Count > 0)
                    {
                        //move new chunck to current Sending data
                        _currentSendingData = _sendingQueue.Dequeue();
                        _sendingTargetBytes = _currentSendingData.Length;
                        _sendingTransferredBytes = 0;
                        return EndSendState.Continue;
                    }
                    else
                    {
                        //no data
                        ResetSentBuffer();
                        ResetRecvBuffer();
                        return EndSendState.Complete;
                    }
                }
                else
                {
                    return EndSendState.Continue;
                }
            }
            else
            {
                //error, socket error
                ResetSentBuffer();
                ResetRecvBuffer();
                return EndSendState.Error;
            }
        }
        void ResetSentBuffer()
        {
            _currentSendingData = null;
            _sendingTransferredBytes = 0;
            _sendingTargetBytes = 0;
        }
        void ReceiveSendIO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive: //receive data from client  
                    //dbugRecvLog(e, "ReceiveSendIO_Completed , Recv");
                    ProcessReceive();
                    break;
                case SocketAsyncOperation.Send: //send data to client
                    //dbugRecvLog(e, "ReceiveSendIO_Completed , Send");
                    ProcessSend();
                    break;
                default:
                    //This exception will occur if you code the Completed event of some
                    //operation to come to this method, by mistake.
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }
        internal void CreateSessionId()
        {
            _sessionId = Interlocked.Increment(ref GlobalSessionNumber.s_mainSessionId);
        }
        public int SessionId
        {
            get
            {
                return _sessionId;
            }
        }
        internal static int ReceivedTransMissionIdGetter()
        {
            return Interlocked.Increment(ref GlobalSessionNumber.s_mainTransMissionId);
        }
        public void Reset()
        {
            //TODO: review here!
        }


#if DEBUG
        public void dbugSetInfo(int tokenId)
        {
            _dbugTokenId = tokenId;
        }
        public int dbugTokenId
        {
            get
            {
                return _dbugTokenId;
            }
        }

        int _dbugTokenId; //for testing only     
        public abstract string dbugGetDataInHolder();
        internal System.Net.EndPoint dbugGetRemoteEndpoint()
        {
            return _recvSendArgs.AcceptSocket.RemoteEndPoint;
        }
        internal SocketAsyncEventArgs dbugGetAsyncSocketEventArgs()
        {
            return _recvSendArgs;
        }

#endif 
    }
}