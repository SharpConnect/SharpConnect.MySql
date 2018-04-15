//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect.MySql.AsyncPatt;

namespace MySqlTest
{

    public class TestSet2Async : MySqlTestSet
    {
        [Test]
        public static void T_InsertAndSelect_Async()
        {


            System.Diagnostics.Stopwatch stopW = new System.Diagnostics.Stopwatch();
            stopW.Start();
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.UseConnectionPool = true;

            var tc = new TaskChain();
            conn.AsyncOpen(tc);

            {
                //drop table if exist
                string sql = "drop table if exists test001";
                var cmd = new MySqlCommand(sql, conn);
                cmd.AsyncExecuteNonQuery(tc);
            }


            {
                string sql = "create table test001(col_id  int(10) unsigned not null auto_increment, col1 int(10)," +
                   "col2 char(2),col3 varchar(255),col4 datetime, primary key(col_id) )";
                var cmd = new MySqlCommand(sql, conn);
                cmd.AsyncExecuteNonQuery(tc);
            }

            for (int i = 0; i < 2000; ++i)
            {

                string sql = "insert into test001(col1,col2,col3,col4) values(10,'AA','123456789','0001-01-01')";
                var cmd = new MySqlCommand(sql, conn);
                cmd.AsyncExecuteNonQuery(tc);

            }

            conn.AsyncClose(tc);
            tc.WhenFinish(() =>
            {

                stopW.Stop();
                Report.WriteLine("avg:" + stopW.ElapsedTicks);
            });
            tc.Start();


        }
    }
}