//MIT, 2015-2016, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect.MySql.Mapper;
using SharpConnect.MySql.BasicAsyncTasks;

namespace MySqlTest
{
    public class TestSet2_SimpleMapper : MySqlTestSet
    {
        [Test]
        public static void T_InsertAndSelect()
        {



            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.UseConnectionPool = true;
            conn.Open();

            DropTableIfExists(conn);
            CreateTable(conn);
            for (int i = 0; i < 100; ++i)
            {
                InsertData(conn);
            }
            SelectDataBack(conn);
            conn.Close();


        }
        static void DropTableIfExists(MySqlConnection conn)
        {
            string sql = "drop table if exists test001";
            var cmd = new MySqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        static void CreateTable(MySqlConnection conn)
        {
            string sql = "create table test001(col_id  int(10) unsigned not null auto_increment, col1 int(10)," +
                "col2 char(2),col3 varchar(255),col4 datetime, primary key(col_id) )";
            var cmd = new MySqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
        static void InsertData(MySqlConnection conn)
        {
            string sql = "insert into test001(col1,col2,col3,col4) values(10,'AA','123456789','0001-01-01')";
            var cmd = new MySqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
            uint lastInsertId = cmd.LastInsertedId;
        }
        static void SelectDataBack(MySqlConnection conn)
        {


            string sql = "select * from test001";
            var cmd = new MySqlCommand(sql, conn);
#if DEBUG
            conn.dbugPleaseBreak = true;
#endif
            //this is very basic mapper***
            var reader = cmd.ExecuteReader();
            var mapper = Mapper.Map((SimpleInfo t, int col_id, string col2, string col3) =>
            {
                t.col1 = col_id;
                t.col2 = col2;
            });
            mapper.DataReader = reader;
            while (reader.Read())
            {
                //simple map query result to member of the target object  
                //we create simpleinfo and use mapper to map field 
                var simpleInfo = mapper.Map(new SimpleInfo());
            }
            reader.Close();
        }

        class SimpleInfo
        {
            public int col1;
            public string col2;
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

            cmd.AsyncExecuteReadEachSubTable(tc, subt =>
            {
                MySqlDataReader reader = subt.CreateDataReader();
                mapper.DataReader = reader;
                int j = subt.RowCount;
                for (int i = 0; i < j; ++i)
                {
                    //then read
                    reader.SetCurrentRowIndex(i);
                    var simpleInfo = mapper.Map(new SimpleInfo());
                    
                }
                ////simple map query result to member of the target object  
                ////we create simpleinfo and use mapper to map field 
               

                tc.AutoCallNext = subt.IsLastTable;

            });
        }

        class SimpleInfo
        {
            public int col1;
            public string col2;
        }
    }
}