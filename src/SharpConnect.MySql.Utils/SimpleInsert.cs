//MIT 2015, brezza92, EngineKit and contributors

using System.Text;
namespace SharpConnect.MySql.Utils
{
    public class SimpleInsert : IHasParameters
    {
        MySqlCommand _sqlCommand;
        bool _isPrepared;
        public SimpleInsert(string targetTableName)
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
        StringBuilder CreateSqlText()
        {
            CommandParams pars = Pars;
            string[] valueKeys = pars.GetAttachedValueKeys();
            var stBuilder = new StringBuilder();
            stBuilder.Append("insert into ");
            stBuilder.Append(TargetTableName);
            stBuilder.Append('(');
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
                if (i < j - 1)
                {
                    stBuilder.Append(',');
                }
            }
            stBuilder.Append(")  values(");
            for (int i = 0; i < j; ++i)
            {
                stBuilder.Append(valueKeys[i]);
                if (i < j - 1)
                {
                    stBuilder.Append(',');
                }
            }
            stBuilder.Append(")");
            return stBuilder;
        }
    }
}
