//2010, CPOL, Stan Kirk
//MIT, 2015-2016, EngineKit and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
namespace SharpConnect.Internal
{
    enum RecvEventCode
    {
        SocketError,
        HasSomeData,
        NoMoreReceiveData,
    }
    class RecvIO
    {
        //receive
        //req 
        readonly int recvStartOffset;
        readonly int recvBufferSize;
        readonly SocketAsyncEventArgs recvArgs;
        Action<RecvEventCode> recvNotify;
        public RecvIO(SocketAsyncEventArgs recvArgs, int recvStartOffset, int recvBufferSize, Action<RecvEventCode> recvNotify)
        {
            this.recvArgs = recvArgs;
            this.recvStartOffset = recvStartOffset;
            this.recvBufferSize = recvBufferSize;
            this.recvNotify = recvNotify;
        }

        public byte ReadByte(int index)
        {
            return recvArgs.Buffer[this.recvStartOffset + index];
        }
        public void ReadTo(int srcIndex, byte[] destBuffer, int destIndex, int count)
        {
            Buffer.BlockCopy(recvArgs.Buffer,
                recvStartOffset + srcIndex,
                destBuffer,
                destIndex, count);
        }
        public void ReadTo(int srcIndex, byte[] destBuffer, int count)
        {
            Buffer.BlockCopy(recvArgs.Buffer,
                recvStartOffset + srcIndex,
                destBuffer,
                0, count);
        }
        public void ReadTo(int srcIndex, MemoryStream ms, int count)
        {
            ms.Write(recvArgs.Buffer,
                recvStartOffset + srcIndex,
                count);
        }


#if DEBUG
        public byte[] dbugReadToBytes()
        {
            int bytesTransfer = recvArgs.BytesTransferred;
            byte[] destBuffer = new byte[bytesTransfer];
            Buffer.BlockCopy(recvArgs.Buffer,
                recvStartOffset,
                destBuffer,
                0, bytesTransfer);
            return destBuffer;
        }
#endif

        /// <summary>
        /// process just received data, called when IO complete
        /// </summary>
        public void ProcessReceivedData()
        {
            //1. socket error
            if (recvArgs.SocketError != SocketError.Success)
            {
                recvNotify(RecvEventCode.SocketError);
                return;
            }
            //2. no more receive 
            if (recvArgs.BytesTransferred == 0)
            {
                recvNotify(RecvEventCode.NoMoreReceiveData);
                return;
            }
            recvNotify(RecvEventCode.HasSomeData);
        }

        /// <summary>
        /// start new receive
        /// </summary>
        public void StartReceive()
        {
            recvArgs.SetBuffer(this.recvStartOffset, this.recvBufferSize);
            recvArgs.AcceptSocket.ReceiveAsync(recvArgs);
        }
        /// <summary>
        /// start new receive
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="len"></param>
        public void StartReceive(byte[] buffer, int len)
        {
            recvArgs.SetBuffer(buffer, 0, len);
            recvArgs.AcceptSocket.ReceiveAsync(recvArgs);
        }
        public int BytesTransferred
        {
            get { return recvArgs.BytesTransferred; }
        }
    }

    enum SendIOEventCode
    {
        SendComplete,
        SocketError,
    }



    enum SendIOState : byte
    {
        ReadyNextSend,
        Sending,
        ProcessSending,
        Error,
    }


    class SendIO
    {
        //send,
        //resp 
        readonly int sendStartOffset;
        readonly int sendBufferSize;
        readonly SocketAsyncEventArgs sendArgs;
        int sendingTargetBytes; //target to send
        int sendingTransferredBytes; //has transfered bytes
        byte[] currentSendingData = null;
        Queue<byte[]> sendingQueue = new Queue<byte[]>();
        Action<SendIOEventCode> notify;
        object stateLock = new object();
        SendIOState _sendingState = SendIOState.ReadyNextSend;
        public SendIO(SocketAsyncEventArgs sendArgs,
            int sendStartOffset,
            int sendBufferSize,
            Action<SendIOEventCode> notify)
        {
            this.sendArgs = sendArgs;
            this.sendStartOffset = sendStartOffset;
            this.sendBufferSize = sendBufferSize;
            this.notify = notify;
        }
        SendIOState sendingState
        {
            get { return _sendingState; }
            set
            {
                switch (_sendingState)
                {
                    case SendIOState.Error:
                        {
                        }
                        break;
                    case SendIOState.ProcessSending:
                        {
                            if (value != SendIOState.ReadyNextSend)
                            {
                            }
                            else
                            {
                            }
                        }
                        break;
                    case SendIOState.ReadyNextSend:
                        {
                            if (value != SendIOState.Sending)
                            {
                            }
                            else
                            {
                            }
                        }
                        break;
                    case SendIOState.Sending:
                        {
                            if (value != SendIOState.ProcessSending)
                            {
                            }
                            else
                            {
                            }
                        }
                        break;
                }
                _sendingState = value;
            }
        }
        void ResetBuffer()
        {
            currentSendingData = null;
            sendingTransferredBytes = 0;
            sendingTargetBytes = 0;
        }
        public void Reset()
        {
            //TODO: review reset
            sendingTargetBytes = sendingTransferredBytes = 0;
            currentSendingData = null;
            sendingQueue.Clear();
        }
        public void EnqueueOutputData(byte[] dataToSend, int count)
        {
            sendingQueue.Enqueue(dataToSend);
        }
        public void StartSendAsync()
        {
            lock (stateLock)
            {
                if (sendingState != SendIOState.ReadyNextSend)
                {
                    //if in other state then return
                    return;
                }
                sendingState = SendIOState.Sending;
            }

            //------------------------------------------------------------------------
            //send this data first

            int remaining = this.sendingTargetBytes - this.sendingTransferredBytes;
            if (remaining == 0)
            {
                if (this.sendingQueue.Count > 0)
                {
                    this.currentSendingData = sendingQueue.Dequeue();
                    remaining = this.sendingTargetBytes = currentSendingData.Length;
                    this.sendingTransferredBytes = 0;
                }
                else
                {   //no data to send ?
                    sendingState = SendIOState.ReadyNextSend;
                    return;
                }
            }
            else if (remaining < 0)
            {
                //?
                throw new NotSupportedException();
            }


            if (remaining <= this.sendBufferSize)
            {
                sendArgs.SetBuffer(this.sendStartOffset, remaining);
                //*** copy from src to dest
                if (currentSendingData != null)
                {
                    Buffer.BlockCopy(this.currentSendingData, //src
                        this.sendingTransferredBytes,
                        sendArgs.Buffer, //dest
                        this.sendStartOffset,
                        remaining);
                }
            }
            else
            {
                //We cannot try to set the buffer any larger than its size.
                //So since receiveSendToken.sendBytesRemainingCount > BufferSize, we just
                //set it to the maximum size, to send the most data possible.
                sendArgs.SetBuffer(this.sendStartOffset, this.sendBufferSize);
                //Copy the bytes to the buffer associated with this SAEA object.
                Buffer.BlockCopy(this.currentSendingData,
                    this.sendingTransferredBytes,
                    sendArgs.Buffer,
                    this.sendStartOffset,
                    this.sendBufferSize);
            }


            if (!sendArgs.AcceptSocket.SendAsync(sendArgs))
            {
                //when SendAsync return false 
                //this means the socket can't do async send     
                ProcessWaitingData();
            }
        }
        /// <summary>
        /// send next data, after prev IO complete
        /// </summary>
        public void ProcessWaitingData()
        {
            // This method is called by I/O Completed() when an asynchronous send completes.   
            //after IO completed, what to do next....

            sendingState = SendIOState.ProcessSending;
            if (sendArgs.SocketError == SocketError.Success)
            {
                this.sendingTransferredBytes += sendArgs.BytesTransferred;
                int remainingBytes = this.sendingTargetBytes - sendingTransferredBytes;
                if (remainingBytes > 0)
                {
                    //no complete!, 
                    //start next send ...
                    //****
                    sendingState = SendIOState.ReadyNextSend;
                    StartSendAsync();
                    //****
                }
                else if (remainingBytes == 0)
                {
                    //complete sending  
                    //check the queue again ...
                    if (sendingQueue.Count > 0)
                    {
                        //move new chunck to current Sending data
                        this.currentSendingData = sendingQueue.Dequeue();
                        if (this.currentSendingData == null)
                        {
                        }
                        this.sendingTargetBytes = currentSendingData.Length;
                        this.sendingTransferredBytes = 0;
                        //****
                        sendingState = SendIOState.ReadyNextSend;
                        StartSendAsync();
                        //****
                    }
                    else
                    {
                        //no data
                        ResetBuffer();
                        //notify no more data
                        //****
                        sendingState = SendIOState.ReadyNextSend;
                        notify(SendIOEventCode.SendComplete);
                        //****   
                    }
                }
                else
                {   //< 0 ????
                    throw new NotSupportedException();
                }
            }
            else
            {
                //error, socket error

                ResetBuffer();
                sendingState = SendIOState.Error;
                notify(SendIOEventCode.SocketError);
                //manage socket errors here
            }
        }
    }
}