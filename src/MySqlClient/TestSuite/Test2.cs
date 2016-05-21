//MIT 2015, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;

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
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();
            Test(n, TimeUnit.Ticks, out total, out avg, () =>
            {
                DropTableIfExists(conn);
                CreateTable(conn);
                InsertData(conn);
                InsertData(conn);
                InsertData(conn);
                InsertData(conn);
            });
            conn.Close();
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

            uint lastInsertId = cmd.LastInsertId;
        }
        
    }
}