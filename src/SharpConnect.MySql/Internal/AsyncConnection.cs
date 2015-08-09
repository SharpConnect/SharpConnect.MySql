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

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using System.Threading;
using System.Net;
using System.Net.Sockets;


namespace MySqlPacket
{
    //test async socket
    //see async socket event args (ASEA) on msdn ...
    //https://msdn.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs.socketasynceventargs%28v=vs.110%29.aspx

    // Represents a collection of reusable SocketAsyncEventArgs objects.   
    class SocketAsyncEventArgsPool
    {
        Stack<SocketAsyncEventArgs> m_pool;

        // Initializes the object pool to the specified size 
        // 
        // The "capacity" parameter is the maximum number of 
        // SocketAsyncEventArgs objects the pool can hold 
        public SocketAsyncEventArgsPool(int capacity)
        {
            m_pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        // Add a SocketAsyncEventArg instance to the pool 
        // 
        //The "item" parameter is the SocketAsyncEventArgs instance 
        // to add to the pool 
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null) { throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null"); }
            lock (m_pool)
            {
                m_pool.Push(item);
            }
        }

        // Removes a SocketAsyncEventArgs instance from the pool 
        // and returns the object removed from the pool 
        public SocketAsyncEventArgs Pop()
        {
            lock (m_pool)
            {
                return m_pool.Pop();
            }
        }

        // The number of SocketAsyncEventArgs instances in the pool 
        public int Count
        {
            get { return m_pool.Count; }
        }

    }


    class AsyncConnection
    {
        public ConnectionConfig config;
        public Socket socket;
        public Object protocol;
        public bool connectionCall;
        public string state;
        public uint threadId;
        HandshakePacket handshake;
        ClientAuthenticationPacket authPacket;
        Query query;

        PacketParser parser;
        PacketWriter writer;

        byte[] tmpForClearRecvBuffer; //for clear buffer 


        long MAX_ALLOWED_PACKET = 0;
        public AsyncConnection(ConnectionConfig userConfig)
        {
            this.config = userConfig;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            protocol = null;
            connectionCall = false;
            state = "disconnected";
            //this.config = options.config;
            //this._socket        = options.socket;
            //this._protocol      = new Protocol({config: this.config, connection: this});
            //this._connectCalled = false;
            //this.state          = "disconnected";
            //this.threadId       = null;
            switch ((CharSets)config.charsetNumber)
            {
                case CharSets.UTF8_GENERAL_CI:
                    parser = new PacketParser(Encoding.UTF8);
                    writer = new PacketWriter(Encoding.UTF8);
                    break;
                case CharSets.ASCII:
                    parser = new PacketParser(Encoding.ASCII);
                    writer = new PacketWriter(Encoding.ASCII);
                    break;
            }
        }

        public void Connect()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(config.host), config.port);
            socket.Connect(endpoint);

            byte[] buffer = new byte[512];
            int count = socket.Receive(buffer);
            if (count < 512)
            {
                writer.Rewrite();
                parser.LoadNewBuffer(buffer, count);
                handshake = new HandshakePacket();
                handshake.ParsePacket(parser);
                this.threadId = handshake.threadId;
                byte[] token = MakeToken(config.password, GetScrollbleBuffer(handshake.scrambleBuff1, handshake.scrambleBuff2));
                writer.IncrementPacketNumber();

                //------------------------------------------
                authPacket = new ClientAuthenticationPacket();
                authPacket.SetValues(config.user, token, config.database, handshake.protocol41);
                authPacket.WritePacket(writer);

                byte[] sendBuff = writer.ToArray();
                byte[] receiveBuff = new byte[512];
                int sendNum = socket.Send(sendBuff);
                int receiveNum = socket.Receive(receiveBuff);

                parser.LoadNewBuffer(receiveBuff, receiveNum);
                if (receiveBuff[4] == 255)
                {
                    ErrPacket errPacket = new ErrPacket();
                    errPacket.ParsePacket(parser);
                    return;
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
            }
        }

        void GetMaxAllowedPacket()
        {
            query = CreateQuery("SELECT @@global.max_allowed_packet", null);
            query.ExecuteQuery();
            if (query.loadError != null)
            {
                dbugConsole.WriteLine("Error Message : " + query.loadError.message);
            }
            else if (query.okPacket != null)
            {

                dbugConsole.WriteLine("OkPacket : " + query.okPacket.affectedRows);

            }
            else
            {
                int i = 0;
                if (query.ReadRow())
                {
                    MAX_ALLOWED_PACKET = query.GetFieldData(0).myLong;
                    //MAX_ALLOWED_PACKET = query.resultSet.rows[0].GetDataInField("@@global.max_allowed_packet").myLong;
                    //dbugConsole.WriteLine("Rows Data " + i + " : " + query.resultSet.rows[i++]);
                }

                //while (query.ReadRow())
                //{

                //    MAX_ALLOWED_PACKET = query.resultSet.rows[0].GetDataInField("@@global.max_allowed_packet").myLong;
                //    dbugConsole.WriteLine("Rows Data " + i + " : " + query.resultSet.rows[i++]);
                //}
            }
        }

        public Query CreateQuery(string sql, CommandParameters values)
        {
            throw new NotSupportedException();
            //var query = Connection.createQuery(sql, values, cb);
            //query = new Query(parser, writer, sql, values);
            //query.typeCast = config.typeCast;
            //query.Start(socket, handshake.protocol41, config);
            //if (socket == null)
            //{
            //    CreateNewSocket();
            //}
            //var query = new Query(this, sql, values);
            //if (MAX_ALLOWED_PACKET > 0)
            //{
            //    query.SetMaxSend(MAX_ALLOWED_PACKET);
            //}
            //return query;
        }

        void CreateNewSocket()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Connect();
        }

        public void Disconnect()
        {
            writer.Rewrite();
            ComQuitPacket quitPacket = new ComQuitPacket();
            quitPacket.WritePacket(writer);

            int send = socket.Send(writer.ToArray());
            socket.Disconnect(true);
        }
        public bool IsStoredInConnPool { get; set; }
        public bool IsInUsed { get; set; }

        internal PacketParser PacketParser
        {
            get { return parser; }
        }
        internal PacketWriter PacketWriter
        {
            get { return writer; }
        }
        internal bool IsProtocol41 { get { return handshake.protocol41; } }

        internal void ClearRemainingInputBuffer()
        {
            //TODO: review here again

            int lastReceive = 0;
            long allReceive = 0;
            int i = 0;
            if (socket.Available > 0)
            {
                if (tmpForClearRecvBuffer == null)
                {
                    tmpForClearRecvBuffer = new byte[1024];
                }

                while (socket.Available > 0)
                {
                    lastReceive = socket.Receive(tmpForClearRecvBuffer);
                    allReceive += lastReceive;
                    i++;
                    //TODO: review here again
                    dbugConsole.WriteLine("i : " + i + ", lastReceive : " + lastReceive);
                    Thread.Sleep(100);
                }
                dbugConsole.WriteLine("All Receive bytes : " + allReceive);
            }
        }


        static byte[] GetScrollbleBuffer(byte[] part1, byte[] part2)
        {
            return ConcatBuffer(part1, part2);
        }

        static byte[] MakeToken(string password, byte[] scramble)
        {
            // password must be in binary format, not utf8
            //var stage1 = sha1((new Buffer(password, "utf8")).toString("binary"));
            //var stage2 = sha1(stage1);
            //var stage3 = sha1(scramble.toString('binary') + stage2);
            //return xor(stage3, stage1);
            var buff1 = Encoding.UTF8.GetBytes(password.ToCharArray());

            var sha = new System.Security.Cryptography.SHA1Managed();
            // This is one implementation of the abstract class SHA1.
            //scramble = new byte[] { 52, 78, 110, 96, 117, 75, 85, 75, 87, 83, 121, 44, 106, 82, 62, 123, 113, 73, 84, 77 };
            byte[] stage1 = sha.ComputeHash(buff1);
            byte[] stage2 = sha.ComputeHash(stage1);
            //merge scramble and stage2 again
            byte[] combineFor3 = ConcatBuffer(scramble, stage2);
            byte[] stage3 = sha.ComputeHash(combineFor3);

            var final = xor(stage3, stage1);
            return final;
        }

        static byte[] ConcatBuffer(byte[] a, byte[] b)
        {
            byte[] combine = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, combine, 0, a.Length);
            Buffer.BlockCopy(b, 0, combine, a.Length, b.Length);
            return combine;
        }

        static byte[] xor(byte[] a, byte[] b)
        {
            var result = new byte[a.Length];
            int j = a.Length;
            for (int i = 0; i < j; ++i)
            {
                result[i] = (byte)(a[i] ^ b[i]);
            }
            return result;
        }



    }



}