//MIT, 2015-present, brezza92, EngineKit and contributors

using System;
using SharpConnect.MySql.Internal;
namespace SharpConnect.MySql
{
    namespace SyncPatt
    {
        public static partial class MySqlSyncPattExtension
        {
            public static void Open(this MySqlConnection conn)
            {
                conn.InternalOpen();
            }
            public static bool Ping(this MySqlConnection conn)
            {
                conn.InternalPing();
                return conn.LatestPingResult;
            }
            public static void ChangeDB(this MySqlConnection conn, string newDbName)
            {
                conn.InternalChangeDB(newDbName);
            }
            public static void ResetConnection(this MySqlConnection conn)
            {
                conn.InternalResetConnection();
            }
            public static void Dispose(this MySqlConnection conn)
            {
                //?
            }
        }
    }
    namespace AsyncPatt
    {
        public static partial class MySqlAsyncPattExtension
        {

            public static void Open(this MySqlConnection conn, Action onComplete, Action next = null)
            {
                conn.InternalOpen(onComplete);
                next?.Invoke();
            }
            public static void Ping(this MySqlConnection conn, Action onComplete, Action next = null)
            {
                conn.InternalPing(onComplete);
                next?.Invoke();
            }
            public static void ChangeDB(this MySqlConnection conn, string newDbName, Action onComplete, Action next = null)
            {
                conn.InternalChangeDB(newDbName, onComplete);
                next?.Invoke();
            }
            public static void ResetConnection(this MySqlConnection conn, Action onComplete, Action next = null)
            {
                conn.InternalResetConnection(onComplete);
                next?.Invoke();
            }
            public static void Close(this MySqlConnection conn, Action onComplete)
            {
                conn.InternalClose(onComplete);
            }

            public static void Stop(this MySqlDataReader reader)
            {
                reader.StopReadingNextRow = true;
            }

        }
    }




    public class MySqlConnectionString
    {
        int _signature;
        public MySqlConnectionString(string h, string u, string p, string d)
            : this(h, u, p, d, 3306)
        {
        }
        public MySqlConnectionString(string h, string u, string p, string d, int portNumber)
        {
            Host = h;
            Username = u;
            Password = p;
            Database = d;
            PortNumber = portNumber;//default mysql port  
        }
        public string Host { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string Database { get; private set; }
        public int PortNumber { get; private set; }
        internal int ConnSignature
        {
            get
            {
                return (_signature != 0) ? _signature : _signature = string.Concat(Host, Username, Database, PortNumber).GetHashCode();
            }
        }
        public static MySqlConnectionString Parse(string connString)
        {
            string[] key_values = connString.Split(';');
            int j = key_values.Length;
            string server = null;
            string uid = null;
            string pwd = null;
            string database = null;
            int portNumber = 3306; //default port
            for (int i = 0; i < j; ++i)
            {
                if (key_values[i] == "")
                {
                    continue;
                }
                string[] key_value = key_values[i].Split('=');
                string key = key_value[0].Trim().ToLower();
                string value = key_value[1].Trim();

                switch (key)
                {
                    case "server":
                        {
                            server = value;
                        }
                        break;
                    case "uid":
                        {
                            uid = value;
                        }
                        break;
                    case "pwd":
                        {
                            pwd = value;
                        }
                        break;
                    case "database":
                        {
                            database = value;
                        }
                        break;
                    case "port":
                        {
                            int.TryParse(value, out portNumber);
                        }
                        break;
                    default:
                        throw new Exception("unknown key?");
                }
            }
            return new MySqlConnectionString(server, uid, pwd, database, portNumber);
        }
    }

    public enum ConnectionState
    {
        Unknown,
        Open,
        Closing,
        Closed,
    }
    public class MySqlConnection
    {
        MySqlConnectionString _connStr;
        /// <summary>
        /// internal MySql connection
        /// </summary>
        Connection _conn;
        public MySqlConnection(MySqlConnectionString connStr)
        {
            _connStr = connStr;
            LockWaitingMilliseconds = 1000 * 5; //TODO: review this default valut
        }
        public MySqlConnection(string host, string uid, string psw, string db)
            : this(new MySqlConnectionString(host, uid, psw, db))
        {
        }
        public MySqlConnection(string connStr)
            : this(MySqlConnectionString.Parse(connStr))
        {
        }
        public bool UseConnectionPool { get; set; }
        public bool FromConnectionPool { get; private set; }

        /// <summary>
        /// (approximate) maximum waiting time for some locking operation,set this before open connection
        /// </summary>
        public int LockWaitingMilliseconds { get; set; }

        public ConnectionState State
        {
            get
            {
                switch (_conn.State)
                {
                    case Internal.ConnectionState.Connected:
                        return ConnectionState.Open;
                    case Internal.ConnectionState.Disconnected:
                        return ConnectionState.Closed;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        internal void InternalOpen(Action onComplete = null)
        {
            this.FromConnectionPool = false;//reset
            //get connection from pool
            if (UseConnectionPool)
            {
                _conn = ConnectionPool.GetConnection(_connStr);
                if (_conn != null)
                {
                    _conn.LockWaitingMilliseconds = LockWaitingMilliseconds;
                    FromConnectionPool = true;
                    onComplete?.Invoke();
                }
                else
                {
                    //create new                     
                    _conn = new Connection(
                        new ConnectionConfig(
                            _connStr.Host,
                            _connStr.Username,
                            _connStr.Password,
                            _connStr.Database)
                        { port = _connStr.PortNumber });

                    _conn.LockWaitingMilliseconds = LockWaitingMilliseconds;
                    _conn.Connect(onComplete);
                }
            }
            else
            {
                //new connection
                _conn = new Connection(
                    new ConnectionConfig(
                        _connStr.Host,
                        _connStr.Username,
                        _connStr.Password,
                        _connStr.Database)
                    { port = _connStr.PortNumber });

                _conn.LockWaitingMilliseconds = LockWaitingMilliseconds;
                _conn.Connect(onComplete);
            }
        }

        internal bool LatestPingResult => _conn.LatestCallIsOk;
        internal void InternalPing(Action onComplete = null)
        {
            _conn.Ping(onComplete);
        }
        internal void InternalResetConnection(Action onComplete = null)
        {
            _conn.ResetConnection(onComplete);
        }

        internal void InternalChangeDB(string newDbName, Action onComplete = null)
        {
            _conn.ChangeDB(newDbName, onComplete);
        }
        public void Close()
        {
            this.InternalClose();
        }


        internal void InternalClose(Action onComplete = null)
        {
            if (UseConnectionPool)
            {
                _conn.ForceReleaseBindingQuery();
                ConnectionPool.ReleaseConnection(_connStr, _conn);
                onComplete?.Invoke();
            }
            else
            {
                if (onComplete != null)
                {
                    //not block
                    _conn.Disconnect(() =>
                    {
                        _conn.Dispose();
                        _conn = null;
                    });
                }
                else
                {
                    _conn.Disconnect(); //block
                    _conn.Dispose();
                    _conn = null;
                }

            }
        }
        internal Connection Conn => _conn;

        public IStringConverter StringConv { get; set; }



#if DEBUG
        public bool dbugPleaseBreak
        {
            get { return _conn.dbugPleaseBreak; }
            set { _conn.dbugPleaseBreak = value; }
        }
#endif
    }

    public static class MySqlConnectionExtension
    {

        public static void HardKill(this MySqlConnection tobeKillConn)
        {
            //TODO : review here ?
            //we use another connection to kill current th

            Connection internalConn = tobeKillConn.Conn;
            string realSql = "KILL " + internalConn._threadId;
            //sql = "FLUSH QUERY CACHE;";             
            Connection killConn = new Connection(internalConn._config);
            killConn.Connect();
            var q = new Query(killConn, realSql, null);
            q.Execute(false); //wait  
            q.Close();
            killConn.Disconnect();
        }

    }


}