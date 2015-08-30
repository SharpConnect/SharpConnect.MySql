//MIT 2015, brezza27, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;

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

            DropTableIfExists(conn);
            CreateTable(conn);
            InsertDataSet(conn);

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
        static void InsertDataSet(MySqlConnection conn)
        {
            string sql = "insert into test001(col1,col2,col3,col4) values(?col1,?col2,?col3,?col4)";
            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddValue("col1", 10);
            cmd.Parameters.AddValue("col2", "AA");
            cmd.Parameters.AddValue("col3", "0123456789");
            cmd.Parameters.AddValue("col4", "0001-01-01");
            cmd.Prepare(); 
            for (int i = 0; i < 100; ++i)
            {
                cmd.Parameters.AddValue("col1", 10);
                cmd.Parameters.AddValue("col2", "AA");
                cmd.Parameters.AddValue("col3", "0123456789");
                cmd.Parameters.AddValue("col4", "0001-01-01");
                cmd.ExecuteNonQuery();
            }  
        }
    }
}