//MIT 2015, brezza27, EngineKit and contributors
using MySqlPacket;
using System;

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

    public class MySqlConnection
    {
        MySqlConnectionString _connStr;
        Connection _conn;
        public MySqlConnection(string host, string uid, string psw, string db)
        {
            _connStr = new MySqlConnectionString(host, uid, psw, db);
        }
        public MySqlConnection(MySqlConnectionString connStr)
        {
            _connStr = connStr;
        }
        public bool UseConnectionPool
        {
            get; set;
        }

        public void Open()
        {
            //get connection from pool
            if (UseConnectionPool)
            {
                _conn = ConnectionPool.GetConnection(_connStr);
                if (_conn == null)
                {
                    //create new 
                    _conn = new Connection(new ConnectionConfig(_connStr.Host, _connStr.Username, _connStr.Password, _connStr.Database));
                    _conn.Connect();
                }
            }
            else
            {
                //new connection
                _conn = new Connection(new ConnectionConfig(_connStr.Host, _connStr.Username, _connStr.Password, _connStr.Database));
                _conn.Connect();
            }
        }
        public void Close()
        {
            if (UseConnectionPool)
            {
                ConnectionPool.ReleaseConnection(_connStr, _conn);
            }
            else
            {
                _conn.Disconnect();
            }
        }

        internal Connection Conn
        {
            get
            {
                return _conn;
            }
        }
    }

    public class MySqlCommand
    {
        public CommandParams Parameters;
        Query _query;
        public MySqlCommand()
        {
            Parameters = new CommandParams();
        }
        public MySqlCommand(string sql, MySqlConnection conn)
        {
            CommandText = sql;
            Connection = conn;
            Parameters = new CommandParams();
        }
        public string CommandText { get; set; }
        public MySqlConnection Connection { get; set; }
        public MySqlDataReader ExecuteReader()
        {
            _query = Connection.Conn.CreateQuery(CommandText, Parameters);
            var reader = new MySqlDataReader(_query);
            _query.Execute();
            return reader;
        }
        public void ExecuteNonQuery()
        {
            _query = Connection.Conn.CreateQuery(CommandText, Parameters);
            _query.Execute();
        }
    }

    public class MySqlDataReader
    {
        Query _query;
        internal MySqlDataReader(Query query)
        {
            _query = query;
        }
        public bool Read()
        {
            return _query.ReadRow();
        }

        public sbyte GetInt8(int colIndex)
        {
            //TODO: check match type and check index here
            return (sbyte)_query._Cells[colIndex].myInt32;
        }
        public byte GetUInt8(int colIndex)
        {
            //TODO: check match type and check index here
            return (byte)_query._Cells[colIndex].myInt32;
        }
        public short GetInt16(int colIndex)
        {   //TODO: check match type and check index here
            return (short)_query._Cells[colIndex].myInt32;
        }
        public ushort GetUInt16(int colIndex)
        {
            //TODO: check match type and check index here
            return (ushort)_query._Cells[colIndex].myInt32;
        }

        public int GetInt32(int colIndex)
        {
            //TODO: check match type and check index here
            return _query._Cells[colIndex].myInt32;
        }
        public uint GetUInt32(int colIndex)
        {
            //TODO: check match type and check index here
            return _query._Cells[colIndex].myUInt32;
        }
        public long GetLong(int colIndex)
        {
            //TODO: check match type and check index here
            return _query._Cells[colIndex].myInt64;
        }
        public ulong GetULong(int colIndex)
        {
            //TODO: check match type and check index here
            return _query._Cells[colIndex].myUInt64;
        }
        public decimal GetDecimal(int colIndex)
        {
            //TODO: check match type and index here
            return _query._Cells[colIndex].myDecimal;
        }
        public string GetString(int colIndex)
        {
            //TODO: check match type and index here
            return _query._Cells[colIndex].myString;
        }
        public byte[] GetBuffer(int colIndex)
        {
            //TODO: check match type and index here
            return _query._Cells[colIndex].myBuffer;
        }

        public DateTime GetDateTime(int colIndex)
        {
            //TODO: check match type and check index here
            return _query._Cells[colIndex].myDateTime;
        }

        public void Close()
        {
            _query.Close();
        }
    }
}