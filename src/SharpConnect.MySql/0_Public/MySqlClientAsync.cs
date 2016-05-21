//MIT 2015, brezza92, EngineKit and contributors

//--------------
//experiment only 
//state: API design
//--------------

using System;
using SharpConnect.MySql.Internal;
namespace SharpConnect.MySql
{
    public class MySqlConnectionAsync
    {
        MySqlConnectionString _connStr;
        Connection _conn;
        public MySqlConnectionAsync(string host, string uid, string psw, string db)
        {
            _connStr = new MySqlConnectionString(host, uid, psw, db);
        }
        public MySqlConnectionAsync(MySqlConnectionString connStr)
        {
            _connStr = connStr;
        }
        public bool UseConnectionPool
        {
            get; set;
        }

        public void Open(Action onOpen)
        {
            //get connection from pool
            if (UseConnectionPool)
            {
                _conn = ConnectionPool.GetConnection(_connStr);
                if (_conn == null)
                {
                    //create new 
                    _conn = new Connection(new ConnectionConfig(_connStr.Host, _connStr.Username, _connStr.Password, _connStr.Database));
                    _conn.ConnectAsync(onOpen);
                }
            }
            else
            {
                //new connection
                _conn = new Connection(new ConnectionConfig(_connStr.Host, _connStr.Username, _connStr.Password, _connStr.Database));
                _conn.ConnectAsync(onOpen);
            }
        }
        public void Close(Action onClosed)
        {
            if (UseConnectionPool)
            {
                ConnectionPool.ReleaseConnection(_connStr, _conn);
                if (onClosed != null)
                {
                    onClosed();
                }
            }
            else
            {
                _conn.DisconnectAsync(onClosed);
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

    public class MySqlCommandAsync
    {
        public CommandParams Parameters;
        Query _query;
        public MySqlCommandAsync()
        {
            Parameters = new CommandParams();
        }
        public MySqlCommandAsync(string sql, MySqlConnectionAsync conn)
        {
            CommandText = sql;
            Connection = conn;
            Parameters = new CommandParams();
        }
        public string CommandText { get; set; }
        public MySqlConnectionAsync Connection { get; set; }
        public MySqlDataReaderAsync ExecuteReader()
        {
            _query = Connection.Conn.CreateQuery(CommandText, Parameters);
            var reader = new MySqlDataReaderAsync(_query);
            _query.Execute();
            return reader;
        }
        public void ExecuteNonQuery()
        {
            _query = Connection.Conn.CreateQuery(CommandText, Parameters);
            _query.Execute();
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
    }

    public class MySqlDataReaderAsync
    {
        Query _query;
        internal MySqlDataReaderAsync(Query query)
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


