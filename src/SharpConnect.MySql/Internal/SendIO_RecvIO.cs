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
        readonly int _recvStartOffset;
        readonly int _recvBufferSize;
        readonly SocketAsyncEventArgs _recvArgs;
        Action<RecvEventCode> _recvNotify;
        public RecvIO(SocketAsyncEventArgs recvArgs, int recvStartOffset, int recvBufferSize, Action<RecvEventCode> recvNotify)
        {
            _recvArgs = recvArgs;
            _recvStartOffset = recvStartOffset;
            _recvBufferSize = recvBufferSize;
            _recvNotify = recvNotify;
        }

        public byte ReadByte(int index) => _recvArgs.Buffer[_recvStartOffset + index];

        public void CopyTo(int srcIndex, byte[] destBuffer, int destIndex, int count)
        {
            Buffer.BlockCopy(_recvArgs.Buffer,
                _recvStartOffset + srcIndex,
                destBuffer,
                destIndex, count);
        }
        public void CopyTo(int srcIndex, byte[] destBuffer, int count)
        {
            Buffer.BlockCopy(_recvArgs.Buffer,
                _recvStartOffset + srcIndex,
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

            ms.Write(_recvArgs.Buffer,
                _recvStartOffset + srcIndex,
                count);
        }


#if DEBUG
        public byte[] dbugReadToBytes()
        {
            int bytesTransfer = _recvArgs.BytesTransferred;
            byte[] destBuffer = new byte[bytesTransfer];
            Buffer.BlockCopy(_recvArgs.Buffer,
                _recvStartOffset,
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
            if (_recvArgs.SocketError != SocketError.Success)
            {
                _recvNotify(RecvEventCode.SocketError);
                return;
            }
            //2. no more receive 
            if (_recvArgs.BytesTransferred == 0)
            {
                _recvNotify(RecvEventCode.NoMoreReceiveData);
                return;
            }
            _recvNotify(RecvEventCode.HasSomeData);
        }

        /// <summary>
        /// start new receive
        /// </summary>
        public void StartReceive()
        {
            _recvArgs.SetBuffer(_recvStartOffset, _recvBufferSize);
            _recvArgs.AcceptSocket.ReceiveAsync(_recvArgs);
        }
        /// <summary>
        /// start new receive
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="len"></param>
        public void StartReceive(byte[] buffer, int len)
        {
            _recvArgs.SetBuffer(buffer, 0, len);
            _recvArgs.AcceptSocket.ReceiveAsync(_recvArgs);
        }
        public int BytesTransferred => _recvArgs.BytesTransferred;

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
        readonly int _sendStartOffset;
        readonly int _sendBufferSize;
        readonly SocketAsyncEventArgs _sendArgs;
        int _sendingTargetBytes; //target to send
        int _sendingTransferredBytes; //has transfered bytes
        byte[] _currentSendingData = null;
        Queue<byte[]> _sendingQueue = new Queue<byte[]>();
        Action<SendIOEventCode> _notify;
        object _stateLock = new object();
        SendIOState _sendingState = SendIOState.ReadyNextSend;
        public SendIO(SocketAsyncEventArgs sendArgs,
            int sendStartOffset,
            int sendBufferSize,
            Action<SendIOEventCode> notify)
        {
            _sendArgs = sendArgs;
            _sendStartOffset = sendStartOffset;
            _sendBufferSize = sendBufferSize;
            _notify = notify;
        }
        SendIOState sendingState
        {
            get => _sendingState;
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
            _currentSendingData = null;
            _sendingTransferredBytes = 0;
            _sendingTargetBytes = 0;
        }
        public void Reset()
        {
            //TODO: review reset
            _sendingTargetBytes = _sendingTransferredBytes = 0;
            _currentSendingData = null;
            _sendingQueue.Clear();
        }
        public void EnqueueOutputData(byte[] dataToSend, int count)
        {
            _sendingQueue.Enqueue(dataToSend);
        }
        public void StartSendAsync()
        {
            lock (_stateLock)
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

            int remaining = _sendingTargetBytes - _sendingTransferredBytes;
            if (remaining == 0)
            {
                if (_sendingQueue.Count > 0)
                {
                    _currentSendingData = _sendingQueue.Dequeue();
                    remaining = _sendingTargetBytes = _currentSendingData.Length;
                    _sendingTransferredBytes = 0;
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


            if (remaining <= _sendBufferSize)
            {
                _sendArgs.SetBuffer(_sendStartOffset, remaining);
                //*** copy from src to dest
                if (_currentSendingData != null)
                {
                    Buffer.BlockCopy(_currentSendingData, //src
                        _sendingTransferredBytes,
                        _sendArgs.Buffer, //dest
                        _sendStartOffset,
                        remaining);
                }
            }
            else
            {
                //We cannot try to set the buffer any larger than its size.
                //So since receiveSendToken.sendBytesRemainingCount > BufferSize, we just
                //set it to the maximum size, to send the most data possible.
                _sendArgs.SetBuffer(_sendStartOffset, _sendBufferSize);
                //Copy the bytes to the buffer associated with this SAEA object.
                Buffer.BlockCopy(_currentSendingData,
                    _sendingTransferredBytes,
                    _sendArgs.Buffer,
                    _sendStartOffset,
                    _sendBufferSize);
            }


            if (!_sendArgs.AcceptSocket.SendAsync(_sendArgs))
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
            if (_sendArgs.SocketError == SocketError.Success)
            {
                _sendingTransferredBytes += _sendArgs.BytesTransferred;
                int remainingBytes = _sendingTargetBytes - _sendingTransferredBytes;
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
                    if (_sendingQueue.Count > 0)
                    {
                        //move new chunck to current Sending data
                        _currentSendingData = _sendingQueue.Dequeue();
                        if (_currentSendingData == null)
                        {
                        }
                        _sendingTargetBytes = _currentSendingData.Length;
                        _sendingTransferredBytes = 0;
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
                        _notify(SendIOEventCode.SendComplete);
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
                _notify(SendIOEventCode.SocketError);
                //manage socket errors here
            }
        }
    }


    class SimpleBufferReader
    {
        //TODO: check endian  ***
        byte[] _originalBuffer;
        int _bufferStartIndex;
        int _readIndex;
        int _bufferSize;
        byte[] _buffer = new byte[16];
        public SimpleBufferReader(byte[] originalBuffer, int bufferStartIndex, int bufferSize)
        {
            _bufferSize = bufferSize;
            _originalBuffer = originalBuffer;
            _bufferStartIndex = bufferStartIndex;
#if DEBUG

            if (dbug_EnableLog)
            {
                dbugInit();
            }
#endif
        }
        public int Position
        {
            get => _readIndex;
            set => _readIndex = value;
        }
        public void Close()
        {
        }

        public bool EndOfStream => _readIndex == _bufferSize;

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
                dbugWriteInfo(Position - 1 + " (byte) " + _originalBuffer[_readIndex + 1]);
            }
#endif

            return _originalBuffer[_readIndex++];
        }
        public uint ReadUInt32()
        {
            byte[] mybuffer = _originalBuffer;
            int s = _bufferStartIndex + _readIndex;
            _readIndex += 4;
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
            byte[] mybuffer = _originalBuffer;
            int s = _bufferStartIndex + _readIndex;
            _readIndex += 8;

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

            byte[] mybuffer = _originalBuffer;
            int s = _bufferStartIndex + _readIndex;
            _readIndex += 4;

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
            byte[] mybuffer = _originalBuffer;
            int s = _bufferStartIndex + _readIndex;
            _readIndex += 4;
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
            byte[] mybuffer = _originalBuffer;
            int s = _bufferStartIndex + _readIndex;
            _readIndex += 2;
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
            byte[] mybuffer = _originalBuffer;
            int s = _bufferStartIndex + _readIndex;
            _readIndex += 2;
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
            byte[] mybuffer = _originalBuffer;
            int s = _bufferStartIndex + _readIndex;
            _readIndex += 8;
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
            byte[] mybuffer = _originalBuffer;
            int s = _bufferStartIndex + _readIndex;
            _readIndex += 8;
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
            byte[] mybuffer = _originalBuffer;
            int s = _bufferStartIndex + _readIndex;
            _readIndex += num;
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
            Buffer.BlockCopy(_originalBuffer, s, buffer, 0, num);
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