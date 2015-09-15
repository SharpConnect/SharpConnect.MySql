//MIT 2015, brezza27, EngineKit and contributors
using System.Text;

namespace SharpConnect.MySql.Utils
{
    public class SimpleInsert : IHasParameters
    {
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

        public void ExecuteNonQuery(MySqlConnection conn)
        {
            //create insert sql
            //then exec

            //create sql command

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

            var sqlcmd = new MySqlCommand(stBuilder.ToString(), pars, conn);
            sqlcmd.ExecuteNonQuery();

        }
        public void Prepare(MySqlConnection conn)
        {

        }
    }
}
