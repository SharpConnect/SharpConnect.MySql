//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect.MySql.AsyncPatt;
using SharpConnect.MySql.Mapper;

//These examples are for .NET2.0 , it use 'TaskChain'.
//(since .net2.0 dose not have System.Threading.Task)

namespace MySqlTest
{
    public class TestSet3_AsyncSocket : MySqlTestSet
    {
        [Test]
        public static void T_AsyncSocket1()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open(() =>
            {
                conn.UpdateMaxAllowPacket();
                conn.Close();
            });

        }
        [Test]
        public static void T_AsyncSocket2()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            var tc = new TaskChain();
            conn.AsyncOpen(tc);
            conn.AsyncClose(tc);
            tc.Start();

        }
    }
    public class TestSet3_1_AsyncSocket : MySqlTestSet
    {
        [Test]
        public static void T_DropCreateInsert()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            var tc = new TaskChain();
            conn.AsyncOpen(tc);
            {
                //1. drop tabled
                var cmd = new MySqlCommand("drop table if exists user_info2", conn);
                cmd.AsyncExecuteNonQuery(tc);
            }
            {
                var cmd = new MySqlCommand("drop table if exists user_info2", conn);
                cmd.AsyncExecuteNonQuery(tc);
            }
            //2. create new one
            {
                var cmd = new MySqlCommand("create table user_info2(uid int(10),u_name varchar(45));", conn);
                cmd.AsyncExecuteNonQuery(tc);
            }
            //3. add some data
            {
                var cmd = new MySqlCommand("insert into user_info2(uid, u_name) values(?uid, 'abc')", conn);
                cmd.Parameters.AddWithValue("?uid", 10);
                cmd.AsyncExecuteNonQuery(tc);
            }

            Report.WriteLine("ok");
            conn.AsyncClose(tc);
            tc.Start();
        }

        [Test]
        public static void T_Select_sysdate2()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open(() =>
            {
                var cmd = new MySqlCommand("select sysdate()", conn);
                cmd.ExecuteReader(r =>
                {
                    //reader is ready  
                    var dtm = r.GetDateTime(0);
                });


            }, () => conn.Close());

        }
        [Test]
        public static void T_Select_sysdate3()
        {
            //prefer this

            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            var tc = new TaskChain();
            conn.AsyncOpen(tc);
            var cmd = new MySqlCommand("select sysdate()", conn);
            cmd.AsyncExecuteReader(tc, reader =>
            { 
                //this example we read each row asynchronously
                //read as 
                var dtm = reader.GetDateTime(0);

            });
            conn.AsyncClose(tc);
            tc.WhenFinish(() =>
            {

            });
            tc.Start();
        }
    }
    public class TestSet2_SimpleMapperAsync : MySqlTestSet
    {
        [Test]
        public static void T_InsertAndSelect()
        {

            try
            {
                var tc = new TaskChain();

                var connStr = GetMySqlConnString();
                var conn = new MySqlConnection(connStr);
                conn.UseConnectionPool = true;
                conn.AsyncOpen(tc);

                DropTableIfExists(conn, tc);
                CreateTable(conn, tc);
                for (int i = 0; i < 1; ++i)
                {
                    InsertData(conn, tc);
                }
                SelectDataBack(conn, tc);

                conn.AsyncClose(tc);
                //
                tc.WhenFinish(() =>
                {

                });
                tc.BeforeEachTaskBegin(() =>
                {

                });
                tc.Start();

            }
            catch (Exception ex)
            {

            }
        }
        static void DropTableIfExists(MySqlConnection conn, TaskChain tc)
        {
            string sql = "drop table if exists test001";
            var cmd = new MySqlCommand(sql, conn);
            cmd.AsyncExecuteNonQuery(tc);
        }

        static void CreateTable(MySqlConnection conn, TaskChain tc)
        {
            string sql = "create table test001(col_id  int(10) unsigned not null auto_increment, col1 int(10)," +
                "col2 char(2),col3 varchar(255),col4 datetime, primary key(col_id) )";
            var cmd = new MySqlCommand(sql, conn);
            cmd.AsyncExecuteNonQuery(tc);
        }
        static void InsertData(MySqlConnection conn, TaskChain tc)
        {
            string sql = "insert into test001(col1,col2,col3,col4) values(10,'AA','123456789','0001-01-01')";
            var cmd = new MySqlCommand(sql, conn);
            cmd.AsyncExecuteNonQuery(tc);
            tc.AddTask(() =>
            {
                var lastInsertId = cmd.LastInsertedId;
            });

        }
        static void SelectDataBack(MySqlConnection conn, TaskChain tc)
        {


            string sql = "select * from test001";
            var cmd = new MySqlCommand(sql, conn);

            tc.AddTask(() =>
            {
#if DEBUG
                conn.dbugPleaseBreak = true;
#endif
            });

            //this is very basic mapper***
            var mapper = Mapper.Map((SimpleInfo t, int col_id, string col2, string col3) =>
            {
                t.col1 = col_id;
                t.col2 = col2;
            });


            cmd.AsyncExecuteSubTableReader(tc, reader =>
            {
                mapper.DataReader = reader;
                while (SharpConnect.MySql.SyncPatt.MySqlSyncPattExtension.Read(reader))
                {
                    var simpleInfo = mapper.Map(new SimpleInfo());
                }
                tc.AutoCallNext = reader.CurrentSubTable.IsLastTable;

            });
        }

        class SimpleInfo
        {
            public int col1;
            public string col2;
        }
    }
}