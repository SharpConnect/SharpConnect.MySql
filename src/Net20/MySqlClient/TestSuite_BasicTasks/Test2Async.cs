//MIT, 2015-2016, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect;
namespace MySqlTest
{
    //------------------------------------
    //this sample is designed for .net2.0 
    //that dose not have Task library
    //------------------------------------

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
            var tasks = new TaskChain();

            tasks.AddTask(ch =>
            {
                conn.Open(ch.Next);
            });
            tasks.AddTask(ch =>
            {
                //drop table if exist
                string sql = "drop table if exists test001";
                var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery(ch.Next);
            });

            tasks.AddTask(ch =>
            {
                string sql = "create table test001(col_id  int(10) unsigned not null auto_increment, col1 int(10)," +
                   "col2 char(2),col3 varchar(255),col4 datetime, primary key(col_id) )";
                var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery(ch.Next);
            });

            for (int i = 0; i < 2000; ++i)
            {
                tasks.AddTask(ch =>
                {
                    string sql = "insert into test001(col1,col2,col3,col4) values(10,'AA','123456789','0001-01-01')";
                    var cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery(ch.Next);
                });
            }

            tasks.AddTask(ch =>
            {
                conn.Close(ch.Next);
            });

            //----------------------------------------
            tasks.Finish(() =>
            {
                stopW.Stop();
                Report.WriteLine("avg:" + stopW.ElapsedTicks);
            });

            tasks.Start();
        }


        [Test]
        public static void T_InsertAndSelect_Async2()
        {
            System.Diagnostics.Stopwatch stopW = new System.Diagnostics.Stopwatch();
            stopW.Start();
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.UseConnectionPool = true;

            var tasks = new TaskChain();
            //add task chain too connection object
            
            tasks.AddTask(conn.OpenAsync());
            //-----------------------------------------
            {
                //drop table if exist
                string sql = "drop table if exists test001";
                var cmd = new MySqlCommand(sql, conn);
                tasks.AddTask(cmd.ExecuteNonQueryAsync());
            }
            //----------------------------------------- 
            {
                //drop table if exist
                string sql = "drop table if exists test001";
                var cmd = new MySqlCommand(sql, conn);
                tasks.AddTask(cmd.ExecuteNonQueryAsync());
            }
            //----------------------------------------- 
            {
                string sql = "create table test001(col_id  int(10) unsigned not null auto_increment, col1 int(10)," +
                   "col2 char(2),col3 varchar(255),col4 datetime, primary key(col_id) )";
                var cmd = new MySqlCommand(sql, conn);
                tasks.AddTask(cmd.ExecuteNonQueryAsync());
            }
            //-----------------------------------------
            for (int i = 0; i < 2000; ++i)
            {

                string sql = "insert into test001(col1,col2,col3,col4) values(10,'AA','123456789','0001-01-01')";
                var cmd = new MySqlCommand(sql, conn);
                tasks.AddTask(cmd.ExecuteNonQueryAsync());
            }
            tasks.AddTask(conn.CloseAsync());


            tasks.Finish(() =>
            {
                stopW.Stop();
                Report.WriteLine("avg:" + stopW.ElapsedTicks);
            });
            //----------------------------------------
            tasks.Start();
        }

        [Test] //with operator overloading
        public static void T_InsertAndSelect_Async3()
        {
            System.Diagnostics.Stopwatch stopW = new System.Diagnostics.Stopwatch();
            stopW.Start();
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.UseConnectionPool = true;

            var tasks = new TaskChain();
            //add task chain too connection object 
            tasks += conn.OpenAsync();
            //-----------------------------------------
            {
                //drop table if exist
                string sql = "drop table if exists test001";
                var cmd = new MySqlCommand(sql, conn);
                tasks += cmd.ExecuteNonQueryAsync();
            }
            //----------------------------------------- 
            {
                //drop table if exist
                string sql = "drop table if exists test001";
                var cmd = new MySqlCommand(sql, conn);
                tasks += cmd.ExecuteNonQueryAsync();
            }
            //----------------------------------------- 
            {
                string sql = "create table test001(col_id  int(10) unsigned not null auto_increment, col1 int(10)," +
                   "col2 char(2),col3 varchar(255),col4 datetime, primary key(col_id) )";
                var cmd = new MySqlCommand(sql, conn);
                tasks += cmd.ExecuteNonQueryAsync();
            }
            //-----------------------------------------
            for (int i = 0; i < 2000; ++i)
            {
                string sql = "insert into test001(col1,col2,col3,col4) values(10,'AA','123456789','0001-01-01')";
                var cmd = new MySqlCommand(sql, conn);
                tasks += cmd.ExecuteNonQueryAsync();
            }
            tasks += conn.CloseAsync();

            tasks.Finish(() =>
            {
                stopW.Stop();
                Report.WriteLine("avg:" + stopW.ElapsedTicks);
            });
            //----------------------------------------
            tasks.Start();
        }
    }
}