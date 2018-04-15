//2010, CPOL, Stan Kirk
//MIT, 2015-2018, EngineKit and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
namespace SharpConnect.Internal
{
#if DEBUG
    static class dbugConsole
    {

        static LogWriter logWriter;
        static dbugConsole()
        {
            //set
            logWriter = new LogWriter(null);//not write anything to disk
            //logWriter = new LogWriter("d:\\WImageTest\\log1.txt");
        }
        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteLine(string str)
        {
            logWriter.Write(str);
            logWriter.Write("\r\n");
        }
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Write(string str)
        {
            logWriter.Write(str);
        }
        class LogWriter : IDisposable
        {
            string filename;
            FileStream fs;
            StreamWriter writer;
            public LogWriter(string logFilename)
            {
                filename = logFilename;
                if (!string.IsNullOrEmpty(logFilename))
                {
                    fs = new FileStream(logFilename, FileMode.Create);
                    writer = new StreamWriter(fs);
                }
            }
            public void Dispose()
            {
                if (writer != null)
                {
                    writer.Flush();
                    writer.Dispose();
                    writer = null;
                }
                if (fs != null)
                {
                    fs.Dispose();
                    fs = null;
                }
            }
            public void Write(string data)
            {
                if (writer != null)
                {
                    writer.Write(data);
                    writer.Flush();
                }
            }
        }

    }
#endif
    //--------------------------------------------------
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
        public void CopyTo(int srcIndex, byte[] destBuffer, int destIndex, int count)
        {
            Buffer.BlockCopy(recvArgs.Buffer,
                recvStartOffset + srcIndex,
                destBuffer,
                destIndex, count);
        }
        public void CopyTo(int srcIndex, byte[] destBuffer, int count)
        {
            Buffer.BlockCopy(recvArgs.Buffer,
                recvStartOffset + srcIndex,
                destBuffer,
                0, count);
        }
        public void CopyTo(int srcIndex, MemoryStream ms, int count)
        {
            //copy 
            //dump data 
#if DEBUG
            //byte[] buffer = recvArgs.Buffer;
            //for (int i = 0; i < count; ++i)
            //{
            //    dbugConsole.WriteLine("[" + i + "]>>b>>" + buffer[recvStartOffset + srcIndex + i]);
            //}
#endif

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


    class SimpleBufferReader
    {
        //TODO: check endian  ***
        byte[] originalBuffer;
        int bufferStartIndex;
        int readIndex;
        int bufferSize;
        byte[] buffer = new byte[16];
        public SimpleBufferReader(byte[] originalBuffer, int bufferStartIndex, int bufferSize)
        {
            this.bufferSize = bufferSize;
            this.originalBuffer = originalBuffer;
            this.bufferStartIndex = bufferStartIndex;
#if DEBUG

            if (dbug_EnableLog)
            {
                dbugInit();
            }
#endif
        }
        public int Position
        {
            get
            {
                return readIndex;
            }
            set
            {
                readIndex = value;
            }
        }
        public void Close()
        {
        }
        public bool EndOfStream
        {
            get
            {
                return readIndex == bufferSize;
            }
        }
        public byte ReadByte()
        {

#if DEBUG
            if (dbug_enableBreak)
            {
                dbugCheckBreakPoint();
            }
            if (dbug_EnableLog)
            {
                //read from current index 
                //and advanced the readIndex to next***
                dbugWriteInfo(Position - 1 + " (byte) " + originalBuffer[readIndex + 1]);
            }
#endif          

            return originalBuffer[readIndex++];
        }
        public uint ReadUInt32()
        {
            byte[] mybuffer = originalBuffer;
            int s = bufferStartIndex + readIndex;
            readIndex += 4;
            uint u = (uint)(mybuffer[s] | mybuffer[s + 1] << 8 |
                mybuffer[s + 2] << 16 | mybuffer[s + 3] << 24);

#if DEBUG
            if (dbug_enableBreak)
            {
                dbugCheckBreakPoint();
            }
            if (dbug_EnableLog)
            {
                dbugWriteInfo(Position - 4 + " (uint32) " + u);
            }
#endif

            return u;
        }
        public unsafe double ReadDouble()
        {
            byte[] mybuffer = originalBuffer;
            int s = bufferStartIndex + readIndex;
            readIndex += 8;

            uint num = (uint)(((mybuffer[s] | (mybuffer[s + 1] << 8)) | (mybuffer[s + 2] << 0x10)) | (mybuffer[s + 3] << 0x18));
            uint num2 = (uint)(((mybuffer[s + 4] | (mybuffer[s + 5] << 8)) | (mybuffer[s + 6] << 0x10)) | (mybuffer[s + 7] << 0x18));
            ulong num3 = (num2 << 0x20) | num;

#if DEBUG
            if (dbug_enableBreak)
            {
                dbugCheckBreakPoint();
            }
            if (dbug_EnableLog)
            {
                dbugWriteInfo(Position - 8 + " (double) " + *(((double*)&num3)));
            }
#endif

            return *(((double*)&num3));
        }
        public unsafe float ReadFloat()
        {

            byte[] mybuffer = originalBuffer;
            int s = bufferStartIndex + readIndex;
            readIndex += 4;

            uint num = (uint)(((mybuffer[s] | (mybuffer[s + 1] << 8)) | (mybuffer[s + 2] << 0x10)) | (mybuffer[s + 3] << 0x18));
#if DEBUG


            if (dbug_enableBreak)
            {
                dbugCheckBreakPoint();
            }
            if (dbug_EnableLog)
            {
                dbugWriteInfo(Position - 4 + " (float)");
            }
#endif

            return *(((float*)&num));
        }
        public int ReadInt32()
        {
            byte[] mybuffer = originalBuffer;
            int s = bufferStartIndex + readIndex;
            readIndex += 4;
            int i32 = (mybuffer[s] | mybuffer[s + 1] << 8 |
                    mybuffer[s + 2] << 16 | mybuffer[s + 3] << 24);

#if DEBUG
            if (dbug_enableBreak)
            {
                dbugCheckBreakPoint();
            }
            if (dbug_EnableLog)
            {
                dbugWriteInfo(Position - 4 + " (int32) " + i32);
            }

#endif          
            return i32;

        }
        public short ReadInt16()
        {
            byte[] mybuffer = originalBuffer;
            int s = bufferStartIndex + readIndex;
            readIndex += 2;
            short i16 = (Int16)(mybuffer[s] | mybuffer[s + 1] << 8);

#if DEBUG
            if (dbug_enableBreak)
            {
                dbugCheckBreakPoint();
            }

            if (dbug_EnableLog)
            {

                dbugWriteInfo(Position - 2 + " (int16) " + i16);
            }
#endif

            return i16;
        }
        public ushort ReadUInt16()
        {
            byte[] mybuffer = originalBuffer;
            int s = bufferStartIndex + readIndex;
            readIndex += 2;
            ushort ui16 = (ushort)(mybuffer[s + 0] | mybuffer[s + 1] << 8);
#if DEBUG
            if (dbug_enableBreak)
            {
                dbugCheckBreakPoint();
            }
            if (dbug_EnableLog)
            {
                dbugWriteInfo(Position - 2 + " (uint16) " + ui16);
            }

#endif
            return ui16;
        }
        public long ReadInt64()
        {
            byte[] mybuffer = originalBuffer;
            int s = bufferStartIndex + readIndex;
            readIndex += 8;
            //
            uint num = (uint)(((mybuffer[s] | (mybuffer[s + 1] << 8)) | (mybuffer[s + 2] << 0x10)) | (mybuffer[s + 3] << 0x18));
            uint num2 = (uint)(((mybuffer[s + 4] | (mybuffer[s + 5] << 8)) | (mybuffer[s + 6] << 0x10)) | (mybuffer[s + 7] << 0x18));
            long i64 = ((long)num2 << 0x20) | num;
#if DEBUG
            if (dbug_enableBreak)
            {
                dbugCheckBreakPoint();
            }
            if (dbug_EnableLog)
            {

                dbugWriteInfo(Position - 8 + " (int64) " + i64);

            }
#endif
            return i64;
        }
        public ulong ReadUInt64()
        {
            byte[] mybuffer = originalBuffer;
            int s = bufferStartIndex + readIndex;
            readIndex += 8;
            //
            uint num = (uint)(((mybuffer[s] | (mybuffer[s + 1] << 8)) | (mybuffer[s + 2] << 0x10)) | (mybuffer[s + 3] << 0x18));
            uint num2 = (uint)(((mybuffer[s + 4] | (mybuffer[s + 5] << 8)) | (mybuffer[s + 6] << 0x10)) | (mybuffer[s + 7] << 0x18));
            ulong ui64 = ((ulong)num2 << 0x20) | num;

#if DEBUG
            if (dbug_enableBreak)
            {
                dbugCheckBreakPoint();
            }
            if (dbug_EnableLog)
            {
                dbugWriteInfo(Position - 8 + " (int64) " + ui64);
            }
#endif

            return ui64;
        }

        public byte[] ReadBytes(int num)
        {
            byte[] mybuffer = originalBuffer;
            int s = bufferStartIndex + readIndex;
            readIndex += num;
            byte[] buffer = new byte[num];

#if DEBUG
            if (dbug_enableBreak)
            {
                dbugCheckBreakPoint();
            }
            if (dbug_EnableLog)
            {
                dbugWriteInfo(Position - num + " (buffer:" + num + ")");
            }
#endif
            Buffer.BlockCopy(originalBuffer, s, buffer, 0, num);
            return buffer;
        }

#if DEBUG
        void dbugCheckBreakPoint()
        {
            if (dbug_enableBreak)
            {
                //if (Position == 35)
                //{
                //}
            }
        }

        bool dbug_EnableLog = false;
        bool dbug_enableBreak = false;
        FileStream dbug_fs;
        StreamWriter dbug_fsWriter;


        void dbugWriteInfo(string info)
        {
            if (dbug_EnableLog)
            {
                dbug_fsWriter.WriteLine(info);
                dbug_fsWriter.Flush();
            }
        }
        void dbugInit()
        {
            if (dbug_EnableLog)
            {
                //if (this.stream.Position > 0)
                //{

                //    dbug_fs = new FileStream(((FileStream)stream).Name + ".r_bin_debug", FileMode.Append);
                //    dbug_fsWriter = new StreamWriter(dbug_fs);
                //}
                //else
                //{
                //    dbug_fs = new FileStream(((FileStream)stream).Name + ".r_bin_debug", FileMode.Create);
                //    dbug_fsWriter = new StreamWriter(dbug_fs);
                //} 
            }
        }
        void dbugClose()
        {
            if (dbug_EnableLog)
            {

                dbug_fs.Dispose();
                dbug_fsWriter = null;
                dbug_fs = null;
            }

        }

#endif
    }


}