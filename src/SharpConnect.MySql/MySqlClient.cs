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

        public MySqlCommand()
        {
        }
        public MySqlCommand(string sql, MySqlConnection conn)
        {
            CommandText = sql;
            Connection = conn;
        }
        public string CommandText { get; set; }
        public MySqlConnection Connection { get; set; }
        public MySqlDataReader ExecuteReader()
        {

            var prepareStatement = new PrepareStatement();
            Query query = Connection.Conn.CreateQuery(this.CommandText, prepareStatement);
            var reader = new MySqlDataReader(query);
            query.ExecuteQuery();

            return reader;
        }
    }

    public class MySqlDataReader
    {
        Query query;
        internal MySqlDataReader(Query query)
        {
            this.query = query;
            if (query.loadError != null)
            {

                //Console.WriteLine("Error : " + query.loadError.message);
            }
            else if (query.okPacket != null)
            {
                //Console.WriteLine("i : " + i + ", OkPacket : [affectedRow] >> " + query.okPacket.affectedRows);
                //Console.WriteLine("i : " + i + ", OkPacket : [insertId] >> " + query.okPacket.insertId);
            }
            else
            {
                //while (query.ReadRow())
                //{
                //    //Console.WriteLine(query.GetFieldData("idsaveImage"));
                //    //Console.WriteLine(query.GetFieldData("saveImagecol"));
                //    //Console.WriteLine(query.GetFieldData("myusercol1"));
                //    //j++;
                //}
            }

        }
        public bool Read()
        {
            return query.ReadRow();
        }
        public DateTime GetDateTime(int colIndex)
        {
            return query.GetFieldData(colIndex).myDateTime;
        }
        public void Close()
        {
            query.Close();

        }
    }
}