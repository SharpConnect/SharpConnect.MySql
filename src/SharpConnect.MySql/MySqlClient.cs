//MIT 2015, brezza27, EngineKit and contributors

using System;
using MySqlPacket;
namespace SharpConnect.MySql
{

    public class MySqlConnectionString
    {

        string signature;
        public MySqlConnectionString(string h, string u, string p, string d)
        {
            Host = h;
            Username = u;
            Password = p;
            Database = d;

            signature = string.Concat(h, u, p, d);
        }

        public string Host { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string Database { get; private set; }

        internal string ConnSignature
        {
            get
            {
                return signature;
            }
        }
    }

    public class MySqlConnection
    {
        MySqlConnectionString connStr;
        Connection conn;
        public MySqlConnection(string host, string uid, string psw, string db)
        {
            connStr = new MySqlConnectionString(host, uid, psw, db);
        }
        public MySqlConnection(MySqlConnectionString connStr)
        {
            this.connStr = connStr;
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
                conn = ConnectionPool.GetConnection(connStr);
                if (conn == null)
                {
                    //create new 
                    conn = new Connection(new ConnectionConfig(connStr.Host, connStr.Username, connStr.Password, connStr.Database));
                    conn.Connect();
                }
            }
            else
            {
                //new connection
                conn = new Connection(new ConnectionConfig(connStr.Host, connStr.Username, connStr.Password, connStr.Database));
                conn.Connect();
            }



        }
        public void Close()
        {
            if (UseConnectionPool)
            {
                ConnectionPool.ReleaseConnection(connStr, conn);
            }
            else
            {
                conn.Disconnect();
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

    public class MySqlCommand
    {
        public CommandParams Parameters;
        Query query;
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
            //parameters = new CommandParams();
            query = Connection.Conn.CreateQuery(this.CommandText, Parameters);
            var reader = new MySqlDataReader(query);
            query.Execute();
            return reader;
        }
        public void ExecuteNonQuery()
        {
            //var parameters = new CommandParameters();
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

    public class MySqlDataReader
    {
        Query query;
        internal MySqlDataReader(Query query)
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