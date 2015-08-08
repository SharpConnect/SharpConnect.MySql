using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using MySqlPacket;
namespace SharpConnect.MySql
{

    public class MySqlConnectionString
    {

        public MySqlConnectionString()
        {
        }
        public MySqlConnectionString(string h,string u,string p,string d)
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
    }

}