//MIT 2015, brezza27, EngineKit and contributors

using System;

using System.Collections.Generic;
using SharpConnect.MySql.Internal;

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

        //----------------------------------------------------
        //test  ...
        //async socket APIs


    }


    public class CommandParams
    {
        Dictionary<string, MyStructData> prepareValues;
        Dictionary<string, string> fieldValues;
        MyStructData reuseData;


        public CommandParams()
        {
            prepareValues = new Dictionary<string, MyStructData>();
            fieldValues = new Dictionary<string, string>();
            reuseData = new MyStructData();
            reuseData.type = Types.NULL;
        }
        public CommandParams(string sql)
        {
            prepareValues = new Dictionary<string, MyStructData>();
            fieldValues = new Dictionary<string, string>();
            reuseData = new MyStructData();
            reuseData.type = Types.NULL;
        }

        public void AddTable(string key, string tablename)
        {
            key = "?" + key;
            fieldValues[key] = "`" + tablename + "`";
        }
        public void AddField(string key, string fieldname)
        {
            key = "?" + key;
            fieldValues[key] = "`" + fieldname + "`";
        }
        public void AddValue(string key, string value)
        {
            if (value != null)
            {
                reuseData.myString = value;
                reuseData.type = Types.VAR_STRING;
            }
            else
            {
                reuseData.myString = null;
                reuseData.type = Types.NULL;
            }
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, byte value)
        {
            reuseData.myByte = value;
            reuseData.type = Types.BIT;
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, int value)
        {
            reuseData.myInt32 = value;
            reuseData.type = Types.LONG;//Types.LONG = int32
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, long value)
        {
            reuseData.myInt64 = value;
            reuseData.type = Types.LONGLONG;
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, float value)
        {
            reuseData.myFloat = value;
            reuseData.type = Types.FLOAT;
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, double value)
        {
            reuseData.myDouble = value;
            reuseData.type = Types.DOUBLE;
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, decimal value)
        {
            reuseData.myDecimal = value;
            reuseData.type = Types.DECIMAL;
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, byte[] value)
        {
            if (value != null)
            {
                reuseData.myBuffer = value;
                reuseData.type = Types.LONG_BLOB;
            }
            else
            {
                reuseData.myBuffer = null;
                reuseData.type = Types.NULL;
            }
            AddKeyWithReuseData(key);
        }
        public void AddValue(string key, DateTime value)
        {
            reuseData.myDateTime = value;
            reuseData.type = Types.DATETIME;
            AddKeyWithReuseData(key);
        }
        void AddKeyWithReuseData(string key)
        {
            key = "?" + key;
            prepareValues[key] = reuseData;
        }

        internal MyStructData GetData(string key)
        {
            MyStructData value = new MyStructData();
            string temp;
            if (prepareValues.TryGetValue(key, out value))
            {
                return value;
            }
            else if (fieldValues.TryGetValue(key, out temp))
            {
                throw new Exception("Error : This key is table or field key. Please use value key and try again.");
            }
            else
            {
                throw new Exception("Error : Key not found '" + key + "' or value not assigned. Please re-check and try again.");
            }
        }
        internal string GetFieldName(string key)
        {
            MyStructData value = new MyStructData();
            string temp;
            if (prepareValues.TryGetValue(key, out value))
            {
                return null;
            }
            else if (fieldValues.TryGetValue(key, out temp))
            {
                return temp;
            }
            else
            {
                return null;
            }
        }
        internal bool IsValueKeys(string key)
        {
            return prepareValues.ContainsKey(key);

        }
    }
    public class MySqlCommand
    {

        Query query;
        bool _isPreparedStmt;
        MySqlDataReader _preparedDataReader;

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
                query.Execute();
                return _preparedDataReader;
            }
            else
            {
                query = Connection.Conn.CreateQuery(this.CommandText, Parameters);
                var reader = new MySqlDataReader(query);
                query.Execute();
                return reader;
            }
        }
        public void ExecuteNonQuery()
        {
            if (_isPreparedStmt)
            {
                query.Execute();
            }
            else
            {
                query = Connection.Conn.CreateQuery(CommandText, Parameters);
                query.Execute();
            }
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
        public void Prepare()
        {

            //prepare sql command;
            query = Connection.Conn.CreatePreparedQuery(CommandText, Parameters);
            query.Prepare();
            _preparedDataReader = new MySqlDataReader(query);
            _isPreparedStmt = true;
            //-----------------



            //-----------------
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