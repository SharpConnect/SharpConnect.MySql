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
                : this(new SqlStringTemplate(sql), new CommandParams(), null)
        {
        }
        public MySqlCommand(string sql, MySqlConnection conn)
                : this(new SqlStringTemplate(sql), new CommandParams(), conn)
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
        /// <summary>
        /// sync execute reader
        /// </summary>
        /// <returns></returns>
        public MySqlDataReader ExecuteReader()
        {
            if (!_isPreparedStmt)
            {
                _query = new Query(this.Connection.Conn, _sqlStringTemplate, Parameters);
            }
            var reader = new MySqlDataReader(_query);
            _query.Execute(true, null);
            reader.WaitUntilFirstDataArrive();
            //
            //after execute in sync mode (this method)
            //reader will wait unit first result arrive            
            return reader;
        }

        public void ExecuteReader(Action<MySqlDataReader> dataReaderReady)
        {
            if (!_isPreparedStmt)
            {
                _query = new Query(this.Connection.Conn, _sqlStringTemplate, Parameters);
            }
            var reader = new MySqlDataReader(_query);
            //in non bloking mode, set this
            reader.SetFirstDataArriveDelegate(dataReaderReady);

            //after execute in asyn mode( this method)
            //reader just return, not block,
            //
            //and when the first data arrive,
            //in invoke dataReaderReader delegate
            _query.Execute(true, () => { });//send empty lambda for async 

        }

        /// <summary>
        /// blocking, read only single value
        /// </summary>
        /// <param name="nextAction"></param>
        /// <returns></returns>
        public object ExecuteScalar()
        {
            object result = null;
            MySqlDataReader reader = ExecuteReader();
            if (reader.Read())
            {
                result = reader.GetValue(0);
            }
            reader.Close();
            return result;
        }
        /// <summary>
        /// non-blocking, read only single value
        /// </summary>
        /// <param name="nextAction"></param>
        /// <returns></returns>
        public void ExecuteScalar(Action<object> resultReady)
        {
            ExecuteReader(reader =>
            {
                //reader is ready here ***       

                reader.Read(subt =>
                {
                    //table is ready for read***
                    //just read single value
                    var tableReader = subt.CreateDataReader();
                    object result = tableReader.GetValue(0);
                    reader.Close(() =>
                    {   //call user result ready***
                        resultReady(result);
                        //
                    });                  

                });
            });
        }
        public void ExecuteNonQuery(Action nextAction = null)
        {
            if (!_isPreparedStmt)
            {
                _query = new Query(Connection.Conn, _sqlStringTemplate, Parameters);
            }
            _query.Execute(false, nextAction);
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