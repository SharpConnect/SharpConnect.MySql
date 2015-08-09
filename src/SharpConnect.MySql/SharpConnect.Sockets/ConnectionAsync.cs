//LICENSE: MIT
//Copyright(c) 2012 Felix Geisendörfer(felix @debuggable.com) and contributors 
//Copyright(c) 2015 brezza27, EngineKit and contributors

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


using System.Net;
using System.Net.Sockets;

using SharpConnect;
using SharpConnect.Sockets; 

namespace MySqlPacket
{

    partial class Connection
    {

        MySqlConnectionSession connSession;
        public void ConnectAsync(Action connHandler)
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            //1. socket
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //2. buffer
            byte[] sockBuffer = new byte[1024 * 2];
            args.SetBuffer(sockBuffer, 0, 1024 * 2);
            connSession = new MySqlConnectionSession(args, 1024, 1024);
            args.UserToken = connSession;
            args.AcceptSocket = socket;

            var endPoint = new IPEndPoint(IPAddress.Parse(config.host), config.port);
            //first connect 
            socket.Connect(endPoint);
            connSession.StartReceive(recv =>
            {

                //parse input
                writer.Rewrite();
                byte[] buffer = new byte[512];
                int count = recv.BytesTransferred;
                recv.CopyTo(0, buffer, 0, recv.BytesTransferred);

                parser.LoadNewBuffer(buffer, count);
                handshake = new HandshakePacket();
                handshake.ParsePacket(parser);
                this.threadId = handshake.threadId;

                byte[] token = MakeToken(config.password,
                    GetScrollbleBuffer(handshake.scrambleBuff1, handshake.scrambleBuff2));

                writer.IncrementPacketNumber();
                //------------------------------------------
                authPacket = new ClientAuthenticationPacket();
                authPacket.SetValues(config.user, token, config.database, handshake.protocol41);
                authPacket.WritePacket(writer);

                //send  
                //do authen 
                //handle  
                recv.recvAction = () =>
                {
                    byte[] sendBuff = writer.ToArray();
                    byte[] receiveBuff = new byte[512];
                    //-------------------------------------------

                    //send data
                    int sendNum = socket.Send(sendBuff);
                    int receiveNum = socket.Receive(receiveBuff);

                    parser.LoadNewBuffer(receiveBuff, receiveNum);
                    if (receiveBuff[4] == 255)
                    {
                        ErrPacket errPacket = new ErrPacket();
                        errPacket.ParsePacket(parser);

                    }
                    else
                    {
                        OkPacket okPacket = new OkPacket(handshake.protocol41);
                        okPacket.ParsePacket(parser);
                    }
                    writer.Rewrite();
                    GetMaxAllowedPacket();
                    if (MAX_ALLOWED_PACKET > 0)
                    {
                        writer.SetMaxAllowedPacket(MAX_ALLOWED_PACKET);
                    }

                    if (connHandler != null)
                    {
                        connHandler();
                    }
                };
                return EndReceiveState.Complete;
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
            if (this.recvHandler != null)
            {
                return recvHandler(recvCarrier);
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