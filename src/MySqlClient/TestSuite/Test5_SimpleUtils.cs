//MIT 2015, brezza27, EngineKit and contributors

using System;
using System.Collections.Generic;

using SharpConnect.MySql;
using SharpConnect.MySql.Utils;

namespace MySqlTest
{


    public class TestSet5_SimpleUtils : MySqlTestSet
    {
        [Test]
        public static void T_SimpleInsert()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();

            {
                string sql = "drop table if exists test001";
                var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }

            {
                string sql = "create table test001(first_name varchar(100),last_name varchar(100))";
                var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
            //---------------------------------------------------
            {
                var insert = new SimpleInsert("test001");
                insert.AddWithValue("?first_name", "test1_firstname");
                insert.AddWithValue("?last_name", "test1_last_name");
                insert.ExecuteNonQuery(conn);
            }

            //---------------------------------------------------
            {
                //prepare
                var insert = new SimpleInsert("test001");
                insert.AddWithValue("?first_name", "");
                insert.AddWithValue("?last_name", "");
                insert.Prepare(conn);
                for (int i = 0; i < 10; ++i)
                {
                    insert.ClearValues();
                    insert.AddWithValue("?first_name", "first" + i);
                    insert.AddWithValue("?last_name", "last" + i);
                    insert.ExecuteNonQuery();
                }
            }
            //--------------------------------------------------- 
            conn.Close();
        }

    }
}