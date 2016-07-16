﻿//MIT, 2015-2016, brezza92, EngineKit and contributors

using System;
using SharpConnect.MySql.Internal;
namespace SharpConnect.MySql
{
    public class MySqlCommand
    {
        Query _query;
        bool _isPreparedStmt;
        SqlStringTemplate sqlStringTemplate;
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
            sqlStringTemplate = sql;
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
            get { return this.sqlStringTemplate.UserRawSql; }
        }
        public MySqlConnection Connection { get; private set; }
        public void Prepare(Action nextAction = null)
        {
            //prepare sql command;
            _isPreparedStmt = true;
            _query = new Query(Connection.Conn, sqlStringTemplate, Parameters);
            _query.Prepare(nextAction);
        }
        public MySqlDataReader ExecuteReader(Action nextAction = null)
        {
            if (_isPreparedStmt)
            {
                var reader = new MySqlDataReader(_query);
                _query.Execute(nextAction);
                return reader;
            }
            else
            {
                _query = new Query(this.Connection.Conn, sqlStringTemplate, Parameters);
                var reader = new MySqlDataReader(_query);
                _query.Execute(nextAction);
                return reader;
            }
        }
        internal void ExecuteReader(SharpConnect.MySql.Internal.Action<MySqlDataReader> nextAction)
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
                _query.Execute(() => { nextAction(reader); });
            }
            else
            {
                _query = new Query(this.Connection.Conn, sqlStringTemplate, Parameters);
                var reader = new MySqlDataReader(_query);
                _query.Execute(() => { nextAction(reader); });
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
                _query = new Query(Connection.Conn, sqlStringTemplate, Parameters);
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