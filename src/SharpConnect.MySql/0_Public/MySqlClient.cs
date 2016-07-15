//MIT, 2015-2016, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
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



    public class MySqlCommand
    {
        Query _query;
        bool _isPreparedStmt;

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
        public string CommandText { get; private set; }
        public MySqlConnection Connection { get; private set; }
        public void Prepare()
        {
            //prepare sql command;
            _isPreparedStmt = true;
            _query = new Query(Connection.Conn, CommandText, Parameters);
            _query.Prepare();
        }
        public MySqlDataReader ExecuteReader()
        {
            if (_isPreparedStmt)
            {
                var reader = new MySqlDataReader(_query);
                _query.Execute();
                return reader;
            }
            else
            {
                _query = new Query(this.Connection.Conn, this.CommandText, Parameters);
                var reader = new MySqlDataReader(_query);
                _query.Execute();
                return reader;
            }
        }
        public void ExecuteNonQuery(Action nextAction = null)
        {
            if (_isPreparedStmt)
            {
                _query.Execute(nextAction);
            }
            else
            {
                _query = new Query(Connection.Conn, CommandText, Parameters);
                _query.Execute(nextAction);
            }
        }

        public uint LastInsertedId
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

    public class MySqlDataReader
    {
        Query _query;
        Queue<MySqlTableResult> subTables = new Queue<MySqlTableResult>();
        MySqlTableResult currentTableResult = null;
        List<DataRowPacket> currentTableRows;
        int currentTableRowCount = 0;
        int currentRowIndex = 0;
        bool isPartialTable;
        //
        DataRowPacket currentRow;
        internal MySqlDataReader(Query query)
        {
            _query = query;
            //start 
            query.SetResultListener(subtable =>
            {
                //we need the subtable must arrive in correct order ***
                lock (subTables)
                {
                    subTables.Enqueue(subtable);
                    isPartialTable = subtable.IsPartialTable; //***
                }
            });
        }
        public bool Read()
        {
            TRY_AGAIN:
            if (currentTableResult == null)
            {
                //no current table
                currentRowIndex = 0;
                currentTableRows = null;
                bool hasSomeSubTables = false;
                lock (subTables)
                {
                    if (subTables.Count > 0)
                    {
                        currentTableResult = subTables.Dequeue();
                        hasSomeSubTables = true;
                    }
                }
                if (!hasSomeSubTables)
                {
                    if (isPartialTable)
                    {
                        //we are in isPartial table mode (not complete)
                        //so must wait until the table arrive **
                        //------------------                    
                        //wait ***
                        //------------------
                        do
                        {
                            //do 
                            System.Threading.Thread.Sleep(1);
                        } while (isPartialTable);

                        goto TRY_AGAIN;
                    }
                    else
                    {
                        //not in partial table mode
                        return false;
                    }
                }
                //
                currentTableRows = currentTableResult.rows;
                currentTableRowCount = currentTableRows.Count;
            }
            //
            if (currentRowIndex < currentTableRowCount)
            {
                //------
                //Console.WriteLine(currentRowIndex.ToString());
                //------
                currentRow = currentTableResult.rows[currentRowIndex];
                currentRowIndex++;
                return true;
            }
            else
            {
                currentTableResult = null;
                goto TRY_AGAIN;
            }
        }
        public void Close()
        {
            _query.Close();
            currentTableResult = null;
            currentRowIndex = 0;
            currentRow = null;
            subTables.Clear();

        }
        public sbyte GetInt8(int colIndex)
        {

            //TODO: check match type and check index here
            return (sbyte)currentRow.Cells[colIndex].myInt32;
        }
        public byte GetUInt8(int colIndex)
        {
            //TODO: check match type and check index here
            return (byte)currentRow.Cells[colIndex].myInt32;
        }
        public short GetInt16(int colIndex)
        {   //TODO: check match type and check index here
            return (short)currentRow.Cells[colIndex].myInt32;
        }
        public ushort GetUInt16(int colIndex)
        {
            //TODO: check match type and check index here
            return (ushort)currentRow.Cells[colIndex].myInt32;
        }

        public int GetInt32(int colIndex)
        {
            //TODO: check match type and check index here
            return currentRow.Cells[colIndex].myInt32;
        }
        public uint GetUInt32(int colIndex)
        {
            //TODO: check match type and check index here
            return currentRow.Cells[colIndex].myUInt32;
        }
        public long GetLong(int colIndex)
        {
            //TODO: check match type and check index here
            return currentRow.Cells[colIndex].myInt64;
        }
        public ulong GetULong(int colIndex)
        {
            //TODO: check match type and check index here
            return currentRow.Cells[colIndex].myUInt64;
        }
        public decimal GetDecimal(int colIndex)
        {
            //TODO: check match type and index here
            return currentRow.Cells[colIndex].myDecimal;
        }
        public string GetString(int colIndex)
        {
            //TODO: check match type and index here
            return currentRow.Cells[colIndex].myString;
        }
        public byte[] GetBuffer(int colIndex)
        {
            //TODO: check match type and index here
            return currentRow.Cells[colIndex].myBuffer;
        }

        public DateTime GetDateTime(int colIndex)
        {
            //TODO: check match type and check index here
            return currentRow.Cells[colIndex].myDateTime;
        }

    }
}