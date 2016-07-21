//MIT, 2015-2016, brezza92, EngineKit and contributors

using System;
using SharpConnect.MySql.Internal;
namespace SharpConnect.MySql
{
    public class MySqlCommand
    {
        Query _query;
        bool _isPreparedStmt;
        SqlStringTemplate _sqlStringTemplate;
        public MySqlCommand(string sql)
            : this(new SqlStringTemplate(sql), null)
        {
        }
        public MySqlCommand(string sql, MySqlConnection conn)
            : this(new SqlStringTemplate(sql), conn)
        {
        }
        public MySqlCommand(string sql, CommandParams cmds, MySqlConnection conn)
            : this(new SqlStringTemplate(sql), cmds, conn)
        {

        }
        public MySqlCommand(SqlStringTemplate sql, MySqlConnection conn)
            : this(sql, new CommandParams(), conn)
        {
        }
        public MySqlCommand(SqlStringTemplate sql, CommandParams cmds, MySqlConnection conn)
        {
            _sqlStringTemplate = sql;
            Connection = conn;
            Parameters = cmds;
        }
        public CommandParams Parameters
        {
            get;
            private set;
        }
        public string CommandText
        {
            get { return this._sqlStringTemplate.UserRawSql; }
        }
        public MySqlConnection Connection { get; set; }
        public void Prepare(Action nextAction = null)
        {
            //prepare sql command;
            _isPreparedStmt = true;
            _query = new Query(Connection.Conn, _sqlStringTemplate, Parameters);
            _query.Prepare(nextAction);
        }
        public MySqlDataReader ExecuteReader(Action nextAction = null)
        {
            if (_isPreparedStmt)
            {
                var reader = new MySqlDataReader(_query);
                _query.Execute(true, nextAction);
                return reader;
            }
            else
            {
                _query = new Query(this.Connection.Conn, _sqlStringTemplate, Parameters);
                var reader = new MySqlDataReader(_query);
                _query.Execute(true, nextAction);
                return reader;
            }
        }
        internal void ExecuteReader(Internal.Action<MySqlDataReader> nextAction)
        {
            //for internal use only (Task Async Programming)
#if DEBUG
            if (nextAction == null)
            {
                throw new Exception("nextAction must not be null");
            }
#endif
            if (_isPreparedStmt)
            {
                var reader = new MySqlDataReader(_query);
                _query.Execute(true, () => { nextAction(reader); });
            }
            else
            {
                _query = new Query(this.Connection.Conn, _sqlStringTemplate, Parameters);
                var reader = new MySqlDataReader(_query);
                _query.Execute(true, () => { nextAction(reader); });
            }
        }
        public void ExecuteNonQuery(Action nextAction = null)
        {
            if (_isPreparedStmt)
            {
                _query.Execute(false, nextAction);
            }
            else
            {
                _query = new Query(Connection.Conn, _sqlStringTemplate, Parameters);
                _query.Execute(false, nextAction);
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