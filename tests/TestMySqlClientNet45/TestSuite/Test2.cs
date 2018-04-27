//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect.MySql.SyncPatt;
namespace MySqlTest
{
    public class TestSet2 : MySqlTestSet
    {
        [Test]
        public static void T_InsertAndSelect()
        {
            int n = 1;
            long total;
            long avg;
            try
            {
                Test(n, TimeUnit.Ticks, out total, out avg, () =>
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
        [Test]
        public static void T_InsertAndSelect2()
        {
            int n = 1;
            long total;
            long avg;

            Test(n, TimeUnit.Ticks, out total, out avg, () =>
            {
                var connStr = GetMySqlConnString();
                var conn = new MySqlConnection(connStr);
                conn.UseConnectionPool = true;
                conn.Open();

                DropTableIfExists(conn);

                //-----------------------
                {
                    //create table
                    string sql = "create table test001(col_id int(10) unsigned not null auto_increment, col1 int(10)," +
                "col2 int(11) unsigned, primary key(col_id) )";
                    var cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                }
                //-----------------------
                for (int i = 0; i < 100; ++i)
                {
                    string sql = "insert into test001(col1,col2) values(?col1, ?col2);";

                    var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("?col1", -10);
                    cmd.Parameters.AddWithValue("?col2", 10);

                    cmd.ExecuteNonQuery();
                    uint lastInsertId = cmd.LastInsertedId;
                }
                //-----------------------
                {

                    string sql = "select col1,col2 from test001";
                    var cmd = new MySqlCommand(sql, conn);
#if DEBUG
                    conn.dbugPleaseBreak = true;
#endif
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        object o0 = reader.GetValue(0);
                        object o1 = reader.GetValue(1);
                        //
                        object o0_1 = reader.GetString(0);
                        object o1_1 = reader.GetString(1);

                        object o0_2 = reader.GetDouble(0);
                        object o1_2 = reader.GetDouble(1);

                        object o0_3 = reader.GetDecimal(0);
                        object o1_3 = reader.GetDecimal(1);

                        object o0_4 = reader.GetInt16(0);
                        object o1_4 = reader.GetUInt8(1);
                    }
                    reader.Close();
                }
                conn.Close();
            });
            Report.WriteLine("avg:" + avg);


        }


        static void DropTableIfExists(MySqlConnection conn)
        {
            string sql = "drop table if exists test001";
            var cmd = new MySqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        static void CreateTable(MySqlConnection conn)
        {
            string sql = "create table test001(col_id int(10) unsigned not null auto_increment, col1 int(10)," +
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