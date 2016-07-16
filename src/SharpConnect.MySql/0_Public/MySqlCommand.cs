//MIT, 2015-2016, brezza92, EngineKit and contributors

using System;
using SharpConnect.MySql.Internal;
namespace SharpConnect.MySql
{   
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

}