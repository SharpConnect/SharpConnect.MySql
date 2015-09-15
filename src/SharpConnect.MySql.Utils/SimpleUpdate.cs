//MIT 2015, brezza27, EngineKit and contributors
using System.Text;

namespace SharpConnect.MySql.Utils
{
    public class SimpleUpdate : IHasParameters
    {
        MySqlCommand _sqlCommand;
        bool _isPrepared;
        string _whereClause;

        public SimpleUpdate(string targetTableName)
        {
            TargetTableName = targetTableName;
            Pars = new CommandParams();
        }
        public CommandParams Pars
        {
            get;
            private set;
        }
        public string TargetTableName { get; private set; }
        public MySqlConnection Connection { get; set; }
        public void ExecuteNonQuery(MySqlConnection conn)
        {
            Connection = conn;
            ExecuteNonQuery();
        }
        public void ExecuteNonQuery()
        {
            //create insert sql
            //then exec
            if (_isPrepared)
            {

                _sqlCommand.ExecuteNonQuery();
            }
            else
            {
                StringBuilder sql = CreateSqlText();
                _sqlCommand = new MySqlCommand(sql.ToString(), Pars, Connection);
                _sqlCommand.ExecuteNonQuery();
            }
        }
        public void Prepare()
        {
            if (_isPrepared)
            {
                throw new System.NotSupportedException("double prepare");
            }
            _isPrepared = true;
            StringBuilder sql = CreateSqlText();
            _sqlCommand = new MySqlCommand(sql.ToString(), Pars, Connection);
            _sqlCommand.Prepare();
        }
        public void Prepare(MySqlConnection conn)
        {
            if (_isPrepared)
            {
                throw new System.NotSupportedException("double prepare");
            }
            Connection = conn;
            Prepare();
        }


        public void Where(string sqlWhere)
        {
            _whereClause = sqlWhere;
        }
        
        public bool ConfirmNoWhereClause { get; set; }
        public string Limit { get; set; }

        StringBuilder CreateSqlText()
        {
            CommandParams pars = Pars;
            string[] valueKeys = pars.GetAttachedValueKeys();
            var stBuilder = new StringBuilder();
            stBuilder.Append("update ");
            stBuilder.Append(TargetTableName);
            stBuilder.Append(" set ");

            int j = valueKeys.Length;
            for (int i = 0; i < j; ++i)
            {
                string k = valueKeys[i];
                //sub string
                if (k[0] != '?')
                {
                    throw new System.NotSupportedException();
                }
                stBuilder.Append(k.Substring(1)); //remove ?
                stBuilder.Append('=');
                stBuilder.Append(k);

                if (i < j - 1)
                {
                    stBuilder.Append(',');
                }
            }

            //this version update must specific where 
            //if not user must confirm that no where

            if (_whereClause == null)
            {
                if (!ConfirmNoWhereClause)
                {
                    throw new System.NotSupportedException("no where clause, or not confirm no where clause");
                }
            }
            else
            {
                stBuilder.Append(" where ");
                stBuilder.Append(_whereClause);
            }


            if (Limit != null)
            {
                stBuilder.Append(" limit " + Limit);
            }
            return stBuilder;
        }

    }
}
