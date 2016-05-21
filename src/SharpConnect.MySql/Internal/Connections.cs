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

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SharpConnect.MySql.Internal
{
    static class dbugConsole
    {
        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteLine(string str)
        {
            //Console.WriteLine(str);
        }
    }

    enum ConnectionState
    {
        Disconnected,
        Connected
    }

    partial class Connection
    {
        public ConnectionConfig config;
        public bool connectionCall;
        public ConnectionState State
        {
            get {
                return socket.Connected ? ConnectionState.Connected : ConnectionState.Disconnected;
            }
        }
        public uint threadId;
        public Socket socket;

        HandshakePacket _handshake;
        Query _query;
        PacketParser _parser;
        PacketWriter _writer;
        
        //TODO: review how to clear remaining buffer again
        byte[] _tmpForClearRecvBuffer; //for clear buffer 

        /// <summary>
        /// max allowed packet size
        /// </summary>
        long _maxPacketSize = 0;

        public Connection(ConnectionConfig userConfig)
        {
            config = userConfig;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //protocol = null;
            connectionCall = false; 

            //this.config = options.config;
            //this._socket        = options.socket;
            //this._protocol      = new Protocol({config: this.config, connection: this});
            //this._connectCalled = false;
            //this.state          = "disconnected";
            //this.threadId       = null;
            switch ((CharSets)config.charsetNumber)
            {
                case CharSets.UTF8_GENERAL_CI:
                    _parser = new PacketParser(Encoding.UTF8);
                    _writer = new PacketWriter(Encoding.UTF8);
                    break;
                case CharSets.ASCII:
                    _parser = new PacketParser(Encoding.ASCII);
                    _writer = new PacketWriter(Encoding.ASCII);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void Connect()
        {
            if (State == ConnectionState.Connected)
            {
                throw new NotSupportedException("already connected");
            }

            var endpoint = new IPEndPoint(IPAddress.Parse(config.host), config.port);
            socket.Connect(endpoint); 

            byte[] buffer = new byte[512];
            int count = socket.Receive(buffer);
            if (count > 0)
            {
                _writer.Reset();
                _parser.LoadNewBuffer(buffer, count);
                _handshake = new HandshakePacket();
                _handshake.ParsePacket(_parser);
                threadId = _handshake.threadId;

                byte[] token = MakeToken(config.password,
                    GetScrollbleBuffer(_handshake.scrambleBuff1, _handshake.scrambleBuff2));

                _writer.IncrementPacketNumber();

                //------------------------------------------
                var authPacket = new ClientAuthenticationPacket();
                authPacket.SetValues(config.user, token, config.database, _handshake.protocol41);
                authPacket.WritePacket(_writer);

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
                    return;
                }
                else
                {
                    OkPacket okPacket = new OkPacket(_handshake.protocol41);
                    okPacket.ParsePacket(_parser);
                }
                _writer.Reset();
                GetMaxAllowedPacket();
                _writer.SetMaxAllowedPacket(_maxPacketSize);
            }
        }

        void GetMaxAllowedPacket()
        {
            _query = CreateQuery("SELECT @@global.max_allowed_packet", null);
            _query.Execute();
            //query = CreateQuery();
            //query.ExecuteQuerySql("SELECT @@global.max_allowed_packet");
            if (_query.LoadError != null)
            {
                dbugConsole.WriteLine("Error Message : " + _query.LoadError.message);
            }
            else if (_query.OkPacket != null)
            {
                dbugConsole.WriteLine("OkPacket : " + _query.OkPacket.affectedRows);
            }
            else
            {
                if (_query.ReadRow())
                {
                    _maxPacketSize = _query.Cells[0].myInt64;
                }
            }
        }

        public Query CreateQuery(string sql, CommandParams command)//testing
        {
            return new Query(this, sql, command);
        }


        void CreateNewSocket()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Connect();
        }

        public void Disconnect()
        {
            _writer.Reset();
            ComQuitPacket quitPacket = new ComQuitPacket();
            quitPacket.WritePacket(_writer);

            int send = socket.Send(_writer.ToArray());
            socket.Disconnect(true);
        }
        public bool IsStoredInConnPool { get; set; }
        public bool IsInUsed { get; set; }

        internal PacketParser PacketParser
        {
            get { return _parser; }
        }
        internal PacketWriter PacketWriter
        {
            get { return _writer; }
        }
        internal bool IsProtocol41 { get { return _handshake.protocol41; } }

        internal void ClearRemainingInputBuffer()
        {
            //TODO: review here again

            int lastReceive = 0;
            long allReceive = 0;
            int i = 0;
            if (socket.Available > 0)
            {
                if (_tmpForClearRecvBuffer == null)
                {
                    _tmpForClearRecvBuffer = new byte[300000];//in test case socket recieve lower than 300,000 bytes
                }

                while (socket.Available > 0)
                {
                    lastReceive = socket.Receive(_tmpForClearRecvBuffer);
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

            return xor(stage3, stage1);
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
            int j = a.Length;
            var result = new byte[j];
            for (int i = 0; i < j; ++i)
            {
                result[i] = (byte)(a[i] ^ b[i]);
            }
            return result;
        }


    }

    class ConnectionConfig
    {
        public string host;
        public int port;
        public string localAddress;//unknowed type
        public string socketPath;//unknowed type
        public string user;
        public string password;
        public string database;
        public int connectionTimeout;
        public bool insecureAuth;
        public bool supportBigNumbers;
        public bool bigNumberStrings;
        public bool dateStrings;
        public bool debug;
        public bool trace;
        public bool stringifyObjects;
        public string timezone;
        public string flags;
        public string queryFormat;
        public string pool;//unknowed type
        public string ssl;//string or bool
        public bool multipleStatements;
        public bool typeCast;
        public long maxPacketSize;
        public int charsetNumber;
        public int defaultFlags;
        public int clientFlags;

        public ConnectionConfig()
        {
            SetDefault();
        }

        public ConnectionConfig(string username, string password)
        {
            SetDefault();
            this.user = username;
            this.password = password;
        }
        public ConnectionConfig(string host, string username, string password, string database)
        {
            SetDefault();
            this.user = username;
            this.password = password;
            this.host = host;
            this.database = database;
        }
        void SetDefault()
        {
            //if (typeof options === 'string') {
            //  options = ConnectionConfig.parseUrl(options);
            //}
            host = "127.0.0.1";//this.host = options.host || 'localhost';
            port = 3306;//this.port = options.port || 3306;
            //this.localAddress       = options.localAddress;
            //this.socketPath         = options.socketPath;
            //this.user               = options.user || undefined;
            //this.password           = options.password || undefined;
            //this.database           = options.database;
            database = "";
            connectionTimeout = 10 * 1000;
            //this.connectTimeout     = (options.connectTimeout === undefined)
            //  ? (10 * 1000)
            //  : options.connectTimeout;
            insecureAuth = false;//this.insecureAuth = options.insecureAuth || false;
            supportBigNumbers = false;//this.supportBigNumbers = options.supportBigNumbers || false;
            bigNumberStrings = false;//this.bigNumberStrings = options.bigNumberStrings || false;
            dateStrings = false;//this.dateStrings = options.dateStrings || false;
            debug = false;//this.debug = options.debug || true;
            trace = false;//this.trace = options.trace !== false;
            stringifyObjects = false;//this.stringifyObjects = options.stringifyObjects || false;
            timezone = "local";//this.timezone = options.timezone || 'local';
            flags = "";//this.flags = options.flags || '';
            //this.queryFormat        = options.queryFormat;
            //this.pool               = options.pool || undefined;

            //this.ssl                = (typeof options.ssl === 'string')
            //  ? ConnectionConfig.getSSLProfile(options.ssl)
            //  : (options.ssl || false);
            multipleStatements = false;//this.multipleStatements = options.multipleStatements || false; 
            typeCast = true;
            //this.typeCast = (options.typeCast === undefined)
            //  ? true
            //  : options.typeCast;

            //if (this.timezone[0] == " ") {
            //  // "+" is a url encoded char for space so it
            //  // gets translated to space when giving a
            //  // connection string..
            //  this.timezone = "+" + this.timezone.substr(1);
            //}

            //if (this.ssl) {
            //  // Default rejectUnauthorized to true
            //  this.ssl.rejectUnauthorized = this.ssl.rejectUnauthorized !== false;
            //}

            maxPacketSize = 0;//this.maxPacketSize = 0;
            charsetNumber = (int)CharSets.UTF8_GENERAL_CI;
            //this.charsetNumber = (options.charset)
            //  ? ConnectionConfig.getCharsetNumber(options.charset)
            //  : options.charsetNumber||Charsets.UTF8_GENERAL_CI;

            //// Set the client flags
            //var defaultFlags = ConnectionConfig.getDefaultFlags(options);
            //this.clientFlags = ConnectionConfig.mergeFlags(defaultFlags, options.flags)
        }

        public void SetConfig(string host, int port, string username, string password, string database)
        {
            this.host = host;
            this.port = port;
            this.user = username;
            this.password = password;
            this.database = database;
        }
    }

}