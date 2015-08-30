//MIT 2015, brezza27, EngineKit and contributors

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
        MySqlConnectionString connStr;
        Connection conn;
        public MySqlConnectionAsync(string host, string uid, string psw, string db)
        {
            connStr = new MySqlConnectionString(host, uid, psw, db);
        }
        public MySqlConnectionAsync(MySqlConnectionString connStr)
        {
            this.connStr = connStr;
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
                conn = ConnectionPool.GetConnection(connStr);
                if (conn == null)
                {
                    //create new 
                    conn = new Connection(new ConnectionConfig(connStr.Host, connStr.Username, connStr.Password, connStr.Database));
                    conn.ConnectAsync(onOpen);
                }
            }
            else
            {
                //new connection
                conn = new Connection(new ConnectionConfig(connStr.Host, connStr.Username, connStr.Password, connStr.Database));
                conn.ConnectAsync(onOpen);
            }

        }
        public void Close(Action onClosed)
        {
            if (UseConnectionPool)
            {
                ConnectionPool.ReleaseConnection(connStr, conn);
                if (onClosed != null)
                {
                    onClosed();
                }
            }
            else
            {
                conn.DisconnectAsync(onClosed);
            }
        }
        internal Connection Conn
        {
            get
            {
                return this.conn;
            }
        }

    }

    public class MySqlCommandAsync
    {
        public CommandParams Parameters;
        Query query;
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

            query = Connection.Conn.CreateQuery(this.CommandText, Parameters);
            var reader = new MySqlDataReaderAsync(query);
            query.Execute();
            return reader;
        }
        public void ExecuteNonQuery()
        {
            query = Connection.Conn.CreateQuery(this.CommandText, Parameters);
            query.Execute();
        }
        public uint LastInsertId
        {
            get
            {
                return query.okPacket.insertId;
            }
        }
        public uint AffectedRows
        {
            get
            {
                return query.okPacket.affectedRows;
            }
        }
    }

    public class MySqlDataReaderAsync
    {
        Query query;
        internal MySqlDataReaderAsync(Query query)
        {
            this.query = query;
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
            return query.ReadRow();
        }

        public sbyte GetInt8(int colIndex)
        {
            //TODO: check match type and check index here
            return (sbyte)query.Cells[colIndex].myInt32;
        }
        public byte GetUInt8(int colIndex)
        {
            //TODO: check match type and check index here
            return (byte)query.Cells[colIndex].myInt32;
        }
        public short GetInt16(int colIndex)
        {   //TODO: check match type and check index here
            return (short)query.Cells[colIndex].myInt32;
        }
        public ushort GetUInt16(int colIndex)
        {
            //TODO: check match type and check index here
            return (ushort)query.Cells[colIndex].myInt32;
        }

        public int GetInt32(int colIndex)
        {
            //TODO: check match type and check index here
            return query.Cells[colIndex].myInt32;
        }
        public uint GetUInt32(int colIndex)
        {
            //TODO: check match type and check index here
            return query.Cells[colIndex].myUInt32;
        }
        public long GetLong(int colIndex)
        {
            //TODO: check match type and check index here
            return query.Cells[colIndex].myInt64;
        }
        public ulong GetULong(int colIndex)
        {
            //TODO: check match type and check index here
            return query.Cells[colIndex].myUInt64;
        }
        public decimal GetDecimal(int colIndex)
        {
            //TODO: check match type and index here
            return query.Cells[colIndex].myDecimal;
        }
        public string GetString(int colIndex)
        {
            //TODO: check match type and index here
            return query.Cells[colIndex].myString;
        }
        public byte[] GetBuffer(int colIndex)
        {
            //TODO: check match type and index here
            return query.Cells[colIndex].myBuffer;
        }

        public DateTime GetDateTime(int colIndex)
        {
            //TODO: check match type and check index here
            return query.Cells[colIndex].myDateTime;
        }


        public void Close()
        {
            query.Close();

        }
    }
}


