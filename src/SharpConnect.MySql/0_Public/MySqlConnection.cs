//MIT, 2015-2016, brezza92, EngineKit and contributors

using System;
using SharpConnect.MySql.Internal;
namespace SharpConnect.MySql
{
    public class MySqlConnectionString
    {
        string _signature;
        public MySqlConnectionString(string h, string u, string p, string d)
        {
            Host = h;
            Username = u;
            Password = p;
            Database = d;
            _signature = string.Concat(h, u, p, d);
        }

        public string Host { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string Database { get; private set; }

        internal string ConnSignature
        {
            get
            {
                return _signature;
            }
        }
    }

    public partial class MySqlConnection
    {
        MySqlConnectionString _connStr;
        Connection _conn;
        public MySqlConnection(string host, string uid, string psw, string db)
        {
            _connStr = new MySqlConnectionString(host, uid, psw, db);
        }
        public MySqlConnection(MySqlConnectionString connStr)
        {
            this._connStr = connStr;
        }
        public bool UseConnectionPool
        {
            get; set;
        }
        public bool FromConnectionPool { get; private set; }

        public void Open(Action onComplete = null)
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
                    _conn = new Connection(new ConnectionConfig(_connStr.Host, _connStr.Username, _connStr.Password, _connStr.Database));
                    _conn.Connect(onComplete);
                }
            }
            else
            {
                //new connection
                _conn = new Connection(new ConnectionConfig(_connStr.Host, _connStr.Username, _connStr.Password, _connStr.Database));
                _conn.Connect(onComplete);
            }
        }
        public void Close(Action onComplete = null)
        {
            if (UseConnectionPool)
            {
                ConnectionPool.ReleaseConnection(_connStr, _conn);
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
            q.Execute(); //wait  
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
            _query.Execute();
            _query.Close();
        }
    }


}