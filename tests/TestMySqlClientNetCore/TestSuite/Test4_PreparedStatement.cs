//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect.MySql.SyncPatt;
namespace MySqlTest
{
    public class TestSet4_PreparedStatement : MySqlTestSet
    {
        [Test]
        public static void T_PrepareStatement()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();
            {
                string sql = "drop table if exists test001";
                var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }

            {
                string sql = "create table test001(col_id  int(10) unsigned not null auto_increment, col1 int(10)," +
                "col2 char(2),col3 varchar(255),col4 datetime, primary key(col_id) )";
                var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }

            {
                string sql = "insert into test001(col1,col2,col3,col4) values(?col1,?col2,?col3,?col4)";
                var cmd = new MySqlCommand(sql, conn);
                cmd.Prepare();
                for (int i = 0; i < 100; ++i)
                {
                    var pars = cmd.Parameters;
                    pars.AddWithValue("?col1", 10);
                    pars.AddWithValue("?col2", "AA");
                    pars.AddWithValue("?col3", "0123456789");
                    pars.AddWithValue("?col4", "0001-01-01");
                    cmd.ExecuteNonQuery();
                }
            }
            {
                string sql = "select col1,col2 from test001 where col1>?col1_v";
                var cmd = new MySqlCommand(sql, conn);
                cmd.Prepare();
                cmd.Parameters.AddWithValue("?col1_v", 0);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {

                }
                reader.Close();
            }
            conn.Close();
            Report.WriteLine("ok");
        }
    }
}