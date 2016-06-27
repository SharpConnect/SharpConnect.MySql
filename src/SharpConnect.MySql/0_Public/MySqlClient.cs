//MIT 2015, brezza92, EngineKit and contributors

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
        public void UpdateMaxAllowPacket()
        {
            _conn.GetMaxAllowedPacket();
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
                return this._conn;
            }
        }

        //----------------------------------------------------
        //test  ...
        //async socket APIs
    }




    public class MySqlCommand
    {
        Query _query;
        bool _isPreparedStmt;
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
        public MySqlCommand(string sql, CommandParams cmds, MySqlConnection conn)
        {
            CommandText = sql;
            Connection = conn;
            Parameters = cmds;
        }
        public CommandParams Parameters
        {
            get;
            private set;
        }
        public string CommandText { get; set; }
        public MySqlConnection Connection { get; set; }
        public MySqlDataReader ExecuteReader()
        {
            if (_isPreparedStmt)
            {
                _query.Execute();
                return new MySqlDataReader(_query);
            }
            else
            {
                _query = Connection.Conn.CreateQuery(this.CommandText, Parameters);
                var reader = new MySqlDataReader(_query);
                _query.Execute();
                return reader;
            }
        }
        public void ExecuteNonQuery()
        {
            if (_isPreparedStmt)
            {
                _query.Execute();
            }
            else
            {
                _query = Connection.Conn.CreateQuery(CommandText, Parameters);
                _query.Execute();
            }
        }
        public uint LastInsertId
        {
            get
            {
                return _query.OkPacket.insertId;
            }
        }
        public uint AffectedRows
        {
            get
            {
                return _query.OkPacket.affectedRows;
            }
        }
        public void Prepare()
        {
            //prepare sql command;
            _isPreparedStmt = true;
            _query = Connection.Conn.CreateQuery(CommandText, Parameters);
            _query.Prepare();
        }
    }




    public class MySqlDataReader
    {
        Query _query;
        internal MySqlDataReader(Query query)
        {
            _query = query;
            //if (query.loadError != null)
            //{

            //    //Console.WriteLine("Error : " + query.loadError.message);
            //}
            //else if (query.okPacket != null)
            //{
            //    //Console.WriteLine("i : " + i + ", OkPacket : [affectedRow] >> " + query.okPacket.affectedRows);
            //    //Console.WriteLine("i : " + i + ", OkPacket : [insertId] >> " + query.okPacket.insertId);
            //}
            //else
            //{
            //    //while (query.ReadRow())
            //    //{
            //    //    //Console.WriteLine(query.GetFieldData("idsaveImage"));
            //    //    //Console.WriteLine(query.GetFieldData("saveImagecol"));
            //    //    //Console.WriteLine(query.GetFieldData("myusercol1"));
            //    //    //j++;
            //    //}
            //}

        }
        public bool Read()
        {
            return _query.ReadRow();
        }

        public sbyte GetInt8(int colIndex)
        {
            //TODO: check match type and check index here
            return (sbyte)_query.Cells[colIndex].myInt32;
        }
        public byte GetUInt8(int colIndex)
        {
            //TODO: check match type and check index here
            return (byte)_query.Cells[colIndex].myInt32;
        }
        public short GetInt16(int colIndex)
        {   //TODO: check match type and check index here
            return (short)_query.Cells[colIndex].myInt32;
        }
        public ushort GetUInt16(int colIndex)
        {
            //TODO: check match type and check index here
            return (ushort)_query.Cells[colIndex].myInt32;
        }

        public int GetInt32(int colIndex)
        {
            //TODO: check match type and check index here
            return _query.Cells[colIndex].myInt32;
        }
        public uint GetUInt32(int colIndex)
        {
            //TODO: check match type and check index here
            return _query.Cells[colIndex].myUInt32;
        }
        public long GetLong(int colIndex)
        {
            //TODO: check match type and check index here
            return _query.Cells[colIndex].myInt64;
        }
        public ulong GetULong(int colIndex)
        {
            //TODO: check match type and check index here
            return _query.Cells[colIndex].myUInt64;
        }
        public decimal GetDecimal(int colIndex)
        {
            //TODO: check match type and index here
            return _query.Cells[colIndex].myDecimal;
        }
        public string GetString(int colIndex)
        {
            //TODO: check match type and index here
            return _query.Cells[colIndex].myString;
        }
        public byte[] GetBuffer(int colIndex)
        {
            //TODO: check match type and index here
            return _query.Cells[colIndex].myBuffer;
        }

        public DateTime GetDateTime(int colIndex)
        {
            //TODO: check match type and check index here
            return _query.Cells[colIndex].myDateTime;
        }


        public void Close()
        {
            _query.Close();
        }
    }
}