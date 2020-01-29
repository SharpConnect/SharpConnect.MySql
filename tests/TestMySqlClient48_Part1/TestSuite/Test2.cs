//MIT, 2015-present, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect.MySql.SyncPatt;
namespace MySqlTest
{

    public class TestSet2 : MySqlTestSet
    {
        [Test]
        public static void T_Ping()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();
            bool ping = conn.Ping();
            conn.Close();
        }
        [Test]
        public static void T_ResetConnection()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();

            {
                var cmd = new MySqlCommand(new SqlStringTemplate("set @x=20;", false), conn);
                cmd.ExecuteNonQuery();
            }
            {
                var cmd = new MySqlCommand(new SqlStringTemplate("set @y=@x+10;", false), conn);
                cmd.ExecuteNonQuery();
            }
            {
                var cmd = new MySqlCommand(new SqlStringTemplate("select @x,@y", false), conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int x = reader.GetInt32(0);
                    int y = reader.GetInt32(1);

                    //test values
                    if (x != 20 || y != x + 10)
                    {
                        throw new NotSupportedException();
                    }
                }
                reader.Close();
            }

            //
            conn.ResetConnection();

            {
                var cmd = new MySqlCommand(new SqlStringTemplate("select @x,@y", false), conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int x = reader.GetInt32(0);
                    int y = reader.GetInt32(1);

                    //test values after reset conn
                    if (x != 0 || y != 0)
                    {
                        throw new NotSupportedException();
                    }
                }
                reader.Close();
            }


            conn.Close();
        }
        [Test]
        public static void T_InsertAndSelect()
        {
            try
            {
                int n = 1;
                Test(n, TimeUnit.Ticks, out long total, out long avg, () =>
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
                    SelectDataBack2(conn);//test select decimal back
                    conn.Close();
                });
                Report.WriteLine("avg:" + avg);
            }
            catch (Exception ex)
            {

            }

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
                "col2 char(2),col3 varchar(255),col4 datetime,col5 decimal(5,2), col6 decimal(16,8), primary key(col_id) )";
            var cmd = new MySqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
        static void InsertData(MySqlConnection conn)
        {
            string sql = "insert into test001(col1,col2,col3,col4,col5,col6) values(10,'AA','123456789','0001-01-01',5.1,10.2857948)";
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
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                //test immediate close
                //reader.Close();
                object o0 = reader.GetValue(0);
                object o1 = reader.GetValue(1);
                object o2 = reader.GetValue("col3");
                object o3 = reader.GetValue("col4");
                object o4 = reader.GetValue("col5");
                object o5 = reader.GetValue("col6");
            }
            reader.Close();
        }
        static void SelectDataBack2(MySqlConnection conn)
        {
            string sql = "select 1.0/3.0;";
            var cmd = new MySqlCommand(sql, conn);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                object value = reader.GetValue(0);
            }
            reader.Close();
        }
    }
}