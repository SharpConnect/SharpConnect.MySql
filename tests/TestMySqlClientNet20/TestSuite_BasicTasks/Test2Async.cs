//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using SharpConnect.MySql;
using SharpConnect.MySql.AsyncPatt;//***
namespace MySqlTest
{
    //------------------------------------
    //this sample is designed for .net2.0 
    //that dose not have Task library
    //------------------------------------

    public class TestSet2Async : MySqlTestSet
    {

        [Test]
        public static void T_InsertAndSelect_Async3()
        {
            System.Diagnostics.Stopwatch stopW = new System.Diagnostics.Stopwatch();
            stopW.Start();
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.UseConnectionPool = true;

            var tc = new TaskChain();
            //add task chain too connection object 
            conn.AsyncOpen(tc);
            //-----------------------------------------
            {
                //drop table if exist
                string sql = "drop table if exists test001";
                var cmd = new MySqlCommand(sql, conn);
                cmd.AsyncExecuteNonQuery(tc);
            }
            //----------------------------------------- 
            {
                //drop table if exist
                string sql = "drop table if exists test001";
                var cmd = new MySqlCommand(sql, conn);
                cmd.AsyncExecuteNonQuery(tc);
            }
            //----------------------------------------- 
            {
                string sql = "create table test001(col_id  int(10) unsigned not null auto_increment, col1 int(10)," +
                   "col2 char(2),col3 varchar(255),col4 datetime, primary key(col_id) )";
                var cmd = new MySqlCommand(sql, conn);
                cmd.AsyncExecuteNonQuery(tc);
            }
            //-----------------------------------------
            for (int i = 0; i < 100; ++i)
            {
                string sql = "insert into test001(col1,col2,col3,col4) values(10,'AA','123456789','0001-01-01')";
                var cmd = new MySqlCommand(sql, conn);
                cmd.AsyncExecuteNonQuery(tc);
            }
            //-----------------------------------------
            {
                string sql = "select * from test001";
                var cmd = new MySqlCommand(sql, conn);

                cmd.AsyncExecuteSubTableReader(tc, subtable =>
                {
                    //when new task is add after tc is started
                    //then this new task is immmediately insert 
                    //after current task

                });
            }
            {
                string sql = "select sysdate()";
                var cmd = new MySqlCommand(sql, conn);
                cmd.AsyncExecuteScalar<DateTime>(tc, dtm =>
                {

                });
            }
            //-----------------------------------------
            conn.AsyncClose(tc);
            tc.WhenFinish(() =>
            {
                stopW.Stop();
                Report.WriteLine("avg:" + stopW.ElapsedTicks);
            });
            tc.BeforeEachTaskBegin(() =>
            {
                Console.WriteLine(tc.CurrentTaskIndex + "/" + tc.TaskCount);
            });
            //----------------------------------------
            tc.Start();
        }
    }
}