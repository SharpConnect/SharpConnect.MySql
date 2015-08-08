//MIT 2015, brezza27, EngineKit and contributors

using System;
using MySqlPacket;
namespace SharpConnect.MySql
{

    public class MySqlConnectionString
    {

        public MySqlConnectionString()
        {
        }
        public MySqlConnectionString(string h, string u, string p, string d)
        {
            Host = h;
            Username = u;
            Password = p;
            Database = d;
        }

        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }
    }

    public class MySqlConnection
    {
        MySqlConnectionString connStr;
        Connection conn;
        public MySqlConnection(string host, string uid, string psw, string db)
        {
            connStr = new MySqlConnectionString()
            {
                Host = host,
                Username = uid,
                Password = psw,
                Database = db
            };
            conn = new Connection(new ConnectionConfig(host, uid, psw, db));
        }
        public MySqlConnection(MySqlConnectionString connStr)
        {
            this.connStr = connStr;
            conn = new Connection(new ConnectionConfig(connStr.Host, connStr.Username, connStr.Password, connStr.Database));

        }
        public void Open()
        {
            conn.Connect();

        }
        public void Close()
        {
            conn.Disconnect();
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