//MIT, 2015-2016, brezza92, EngineKit and contributors

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
            public static void Close(this MySqlConnection conn)
            {
                conn.InternalClose();
            }
        }
    }
    namespace AsyncPatt
    {
        public static partial class MySqlAsyncPattExtension
        {

            public static void Open(this MySqlConnection conn, Action onComplete)
            {
                conn.InternalOpen(onComplete);
            }
            public static void Close(this MySqlConnection conn, Action onComplete)
            {
                conn.InternalClose(onComplete);
            }
        }
    }




    public class MySqlConnectionString
    {
        string _signature;
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
            PortNumber = 3306;//default mysql port
            _signature = string.Concat(h, u, d, PortNumber);
        }
        public string Host { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string Database { get; private set; }
        public int PortNumber { get; private set; }
        internal string ConnSignature
        {
            get
            {
                return _signature;
            }
        }

        public static MySqlConnectionString Parse(string connString)
        {
            //MySqlConnectionString connString = new MySqlConnectionString();
            string[] key_values = connString.Split(';');
            int j = key_values.Length;
            string server = null;
            string uid = null;
            string pwd = null;
            string database = null;
            int portNumber = 3306;
            for (int i = 0; i < j; ++i)
            {
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

    public class MySqlConnection
    {
        MySqlConnectionString _connStr;
        Connection _conn;
        public MySqlConnection(MySqlConnectionString connStr)
        {
            this._connStr = connStr;
        }
        public MySqlConnection(string host, string uid, string psw, string db)
            : this(new MySqlConnectionString(host, uid, psw, db))
        {
        }
        public MySqlConnection(string connStr)
            : this(MySqlConnectionString.Parse(connStr))
        {
        }
        public bool UseConnectionPool
        {
            get;
            set;
        }
        public bool FromConnectionPool { get; private set; }

        internal void InternalOpen(Action onComplete = null)
        {
            this.FromConnectionPool = false;//reset
            //get connection from pool
            if (UseConnectionPool)
            {
                _conn = ConnectionPool.GetConnection(_connStr);
                if (_conn != null)
                {
                    FromConnectionPool = true;
                    if (onComplete != null)
                    {
                        onComplete();
                    }
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
                _conn.Connect(onComplete);
            }
        }
        internal void InternalClose(Action onComplete = null)
        {
            if (UseConnectionPool)
            {
                _conn.ForceReleaseBindingQuery();
                ConnectionPool.ReleaseConnection(_connStr, _conn);
                if (onComplete != null)
                {
                    onComplete();
                }
            }
            else
            {
                _conn.Disconnect(onComplete);
            }
        }
        internal Connection Conn
        {
            get
            {
                return this._conn;
            }
        }

        internal void SetMaxAllowedPacket(int value)
        {
            this._conn.PacketWriter.SetMaxAllowedPacket(value);
        }

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
            string realSql = "KILL " + internalConn.threadId;
            //sql = "FLUSH QUERY CACHE;";             
            Connection killConn = new Connection(internalConn.config);
            killConn.Connect();
            var q = new Query(killConn, realSql, null);
            q.Execute(false); //wait  
            q.Close();
            killConn.Disconnect();
        }
        public static void UpdateMaxAllowPacket(this MySqlConnection conn)
        {
            var _query = new Query(conn.Conn, "SELECT @@global.max_allowed_packet", null);
            _query.SetResultListener(result =>
            {
                long value = result.rows[0].Cells[0].myInt64;
                if (value > int.MaxValue)
                {
                    throw new NotSupportedException("not support max allowed packet > int.MaxValue");
                }
                conn.SetMaxAllowedPacket((int)value); //cast down
            });
            //wait
            _query.Execute(true);
            _query.Close();
        }
    }


}