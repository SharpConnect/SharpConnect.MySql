//LICENSE: MIT
//Copyright(c) 2012 Felix Geisendörfer(felix @debuggable.com) and contributors 
//Copyright(c) 2015 brezza92, EngineKit and contributors

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE. 



//!!!!!!!!!!
// experiment 
//use socket async event arg, 
//!!!!!!!!!!

using System.Net;
using System.Net.Sockets;
using SharpConnect.Sockets;
namespace SharpConnect.MySql.Internal
{
    partial class Connection
    {
        byte[] _sockBuffer = new byte[1024 * 2];
        SocketAsyncEventArgs _saea = new SocketAsyncEventArgs();
        MySqlConnectionSession _connSession;
        public void ConnectAsync(Action connHandler)
        {
            //1. socket
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _saea.SetBuffer(_sockBuffer, 0, _sockBuffer.Length);
            //2. buffer 
            _connSession = new MySqlConnectionSession(_saea, 1024, 1024);
            _saea.UserToken = _connSession;
            _saea.AcceptSocket = socket;
            var endPoint = new IPEndPoint(IPAddress.Parse(config.host), config.port);
            //first connect 
            socket.Connect(endPoint);
            _connSession.StartReceive(recv =>
            {
                //TODO: review here, don't copy, 
                //we should use shared sockBuffer 

                byte[] buffer = new byte[512];
                int count = recv.BytesTransferred;
                recv.CopyTo(0, buffer, 0, recv.BytesTransferred);
                _parser.LoadNewBuffer(buffer, count);
                _handshake = new HandshakePacket();
                _handshake.ParsePacket(_parser);
                this.threadId = _handshake.threadId;
                byte[] token = MakeToken(config.password,
                    GetScrollbleBuffer(_handshake.scrambleBuff1, _handshake.scrambleBuff2));
                _writer.Reset();
                _writer.IncrementPacketNumber();
                //------------------------------------------
                var authPacket = new ClientAuthenticationPacket();
                authPacket.SetValues(config.user, token, config.database, _handshake.protocol41);
                authPacket.WritePacket(_writer);
                //send  
                //do authen 
                //handle  
                recv._recvAction = () =>
                {
                    byte[] sendBuff = _writer.ToArray();
                    byte[] receiveBuff = new byte[512];
                    //-------------------------------------------

                    //send data
                    int sendNum = socket.Send(sendBuff);
                    int receiveNum = socket.Receive(receiveBuff);
                    _parser.LoadNewBuffer(receiveBuff, receiveNum);
                    if (receiveBuff[4] == 255)
                    {
                        ErrPacket errPacket = new ErrPacket();
                        errPacket.ParsePacket(_parser);
                    }
                    else
                    {
                        OkPacket okPacket = new OkPacket(_handshake.protocol41);
                        okPacket.ParsePacket(_parser);
                    }
                    _writer.Reset();
                    GetMaxAllowedPacket();
                    if (_maxPacketSize > 0)
                    {
                        _writer.SetMaxAllowedPacket(_maxPacketSize);
                    }

                    if (connHandler != null)
                    {
                        connHandler();
                    }
                };
                return EndReceiveState.Complete;
            });
        }


        public void DisconnectAsync(Action onClosed)
        {
            _writer.Reset();
            ComQuitPacket quitPacket = new ComQuitPacket();
            quitPacket.WritePacket(_writer);
            byte[] dataToSend = _writer.ToArray();
            _connSession.SetDataToSend(dataToSend, dataToSend.Length);
            _connSession.StartSend(recv =>
            {
                socket.Disconnect(true);
                return EndSendState.Complete;
            });
        }
    }

    class MySqlConnectionSession : ClientConnectionSession
    {
        public MySqlConnectionSession(SocketAsyncEventArgs recvSendArgs, int recvBufferSize, int sendBufferSize)
            : base(recvSendArgs, recvBufferSize, sendBufferSize)
        {
        }
        protected override void ResetRecvBuffer()
        {
            //reset recv state
            //...
        }
        protected override EndReceiveState ProtocolRecvBuffer(ReceiveCarrier recvCarrier)
        {
            if (this._recvHandler != null)
            {
                return _recvHandler(recvCarrier);
            }

            //else
            return EndReceiveState.Complete;
        }
#if DEBUG
        public override string dbugGetDataInHolder()
        {
            return "";
        }
#endif
    }
}