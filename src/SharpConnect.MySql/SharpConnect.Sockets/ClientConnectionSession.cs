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

        internal static int mainTransMissionId = 10000;
        internal static int mainSessionId = 1000000000;
        internal static int maxSimultaneousClientsThatWereConnected;

    }

    abstract class ClientConnectionSession
    {

        //The session ID correlates with all the data sent in a connected session.
        //It is different from the transmission ID in the DataHolder, which relates
        //to one TCP message. A connected session could have many messages, if you
        //set up your app to allow it.
        int sessionId;
        readonly SocketAsyncEventArgs recvSendArgs;
        //recv
        ReceiveCarrier recvCarrier;
        readonly int startBufferOffset;
        readonly int recvBufferSize;

        //send
        readonly int initSentOffset;
        readonly int sendBufferSize;

        int sendingTargetBytes;
        int sendingTransferredBytes;
        byte[] currentSendingData = null;

        Queue<byte[]> sendingQueue = new Queue<byte[]>();
        protected Func<ReceiveCarrier, EndReceiveState> recvHandler;

        public ClientConnectionSession(SocketAsyncEventArgs recvSendArgs,
            int recvBufferSize, int sendBufferSize)
        {

            recvCarrier = new ReceiveCarrier(recvSendArgs);

            this.startBufferOffset = recvSendArgs.Offset;
            this.recvSendArgs = recvSendArgs;
            this.recvBufferSize = recvBufferSize;

            this.initSentOffset = startBufferOffset + recvBufferSize;
            this.sendBufferSize = sendBufferSize;


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
            if (recvSendArgs.SocketError != SocketError.Success)
            {
                this.ResetRecvBuffer();
                //Jump out of the ProcessReceive method.
                return EndReceiveState.Error;
            }
            if (recvSendArgs.BytesTransferred == 0)
            {
                // If no data was received, close the connection. This is a NORMAL
                // situation that shows when the client has finished sending data.
                this.ResetRecvBuffer();
                return EndReceiveState.NoMoreData;
            }

            //--------------------
            return this.ProtocolRecvBuffer(this.recvCarrier);
        }


        internal void StartReceive(Func<ReceiveCarrier, EndReceiveState> recvHandler)
        {
            this.recvHandler = recvHandler;
            this.recvSendArgs.SetBuffer(this.startBufferOffset, this.recvBufferSize);
            if (!recvSendArgs.AcceptSocket.ReceiveAsync(recvSendArgs))
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
                recvSendArgs.AcceptSocket.Shutdown(SocketShutdown.Both);
            }
            // throws if socket was already closed
            catch (Exception)
            {
                //
                //dbugSendLog(e, "CloseClientSocket, Shutdown catch");
            }
            recvSendArgs.AcceptSocket.Close();
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
                    StartReceive(this.recvHandler);  //again
                    return;
                case EndReceiveState.Complete:

                    var conCompleteAction = this.recvCarrier.recvAction;

                    if (conCompleteAction != null)
                    {
                        this.recvCarrier.recvAction = null;//clear
                        conCompleteAction();
                    }
                    return;
            }
        }

        //--------------------------------------------------------------------------------
        internal void SetDataToSend(byte[] dataToSend, int count)
        {
            if (currentSendingData == null)
            {
                currentSendingData = dataToSend;
                sendingTargetBytes = count;
            }
            else
            {
                //add to queue
                sendingQueue.Enqueue(dataToSend);
            }
        }

        internal void StartSend()
        {

            int remaining = this.sendingTargetBytes - this.sendingTransferredBytes;
            if (remaining <= this.sendBufferSize)
            {
                recvSendArgs.SetBuffer(this.initSentOffset, remaining);
                //*** copy from src to dest
                Buffer.BlockCopy(this.currentSendingData, //src
                    this.sendingTransferredBytes,
                    recvSendArgs.Buffer, //dest
                    this.initSentOffset,
                    remaining);
            }
            else
            {
                //We cannot try to set the buffer any larger than its size.
                //So since receiveSendToken.sendBytesRemainingCount > BufferSize, we just
                //set it to the maximum size, to send the most data possible.
                recvSendArgs.SetBuffer(this.initSentOffset, this.sendBufferSize);
                //Copy the bytes to the buffer associated with this SAEA object.
                Buffer.BlockCopy(this.currentSendingData,
                    this.sendingTransferredBytes,
                    recvSendArgs.Buffer,
                    this.initSentOffset,
                    this.sendBufferSize);

                //We'll change the value of sendUserToken.sendBytesRemainingCount
                //in the ProcessSend method.
            }


            if (!recvSendArgs.AcceptSocket.SendAsync(recvSendArgs))
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
                    //
                    return;
            }
        }
        EndSendState EndSend()
        {
            if (recvSendArgs.SocketError == SocketError.Success)
            {
                //success !                 

                this.sendingTransferredBytes += recvSendArgs.BytesTransferred;
                if ((this.sendingTargetBytes - sendingTransferredBytes) <= 0)
                {
                    //check if no other data in chuck 
                    if (sendingQueue.Count > 0)
                    {
                        //move new chunck to current Sending data
                        this.currentSendingData = sendingQueue.Dequeue();
                        this.sendingTargetBytes = currentSendingData.Length;
                        this.sendingTransferredBytes = 0;
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
            currentSendingData = null;
            sendingTransferredBytes = 0;
            sendingTargetBytes = 0;

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
            sessionId = Interlocked.Increment(ref GlobalSessionNumber.mainSessionId);
        }
        public int SessionId
        {
            get
            {
                return this.sessionId;
            }
        }
        internal static int ReceivedTransMissionIdGetter()
        {
            return Interlocked.Increment(ref GlobalSessionNumber.mainTransMissionId);
        }
        public void Reset()
        {
            //TODO: review here!
        }
#if DEBUG
        public void dbugSetInfo(int tokenId)
        {
            this._dbugTokenId = tokenId;
        }
        public int dbugTokenId
        {

            get
            {
                return this._dbugTokenId;
            }
        }

        int _dbugTokenId; //for testing only     
        public abstract string dbugGetDataInHolder();
        internal System.Net.EndPoint dbugGetRemoteEndpoint()
        {
            return recvSendArgs.AcceptSocket.RemoteEndPoint;
        }
        internal SocketAsyncEventArgs dbugGetAsyncSocketEventArgs()
        {
            return recvSendArgs;
        }

#endif 
    }




}