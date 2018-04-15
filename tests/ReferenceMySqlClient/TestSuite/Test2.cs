//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using MySql.Data;
using MySql.Data.MySqlClient;
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

            Test(n, TimeUnit.Ticks, out total, out avg, () =>
            {
                var connStr = GetMySqlConnString();
                var conn = new MySqlConnection(connStr);
                conn.Open();

                DropTableIfExists(conn);
                CreateTable(conn);
                for (int i = 0; i < 2000; ++i)
                {
                    InsertData(conn);
                }
                SelectDataBack(conn);
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
            long lastInsertId = cmd.LastInsertedId;
        }
        static void SelectDataBack(MySqlConnection conn)
        {
            string sql = "select * from test001";
            var cmd = new MySqlCommand(sql, conn);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {

            }
            reader.Close();
        }
    }
}