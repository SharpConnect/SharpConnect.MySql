//MIT, 2015-2018, brezza92, EngineKit and contributors
using System;
using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace MySqlTest
{
    public class Test_StoreProc_MultiResultSet : MySqlTestSet
    {
        [Test]
        public static void T_StoreProcMultiResultSet()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);

            Console.WriteLine("1");
            conn.Open();
            Console.WriteLine("2");
            {
                string createStoreProcSql = @"DROP PROCEDURE IF EXISTS multi;";
                var cmd = new MySqlCommand(createStoreProcSql, conn);
                Console.WriteLine("3");
                cmd.ExecuteNonQuery();
            }
            {
                string createStoreProcSql = @"CREATE PROCEDURE multi() BEGIN
                              SELECT 1 as A;
                              SELECT 2 as B;
                              END";
                Console.WriteLine("4");
                var cmd = new MySqlCommand(createStoreProcSql, conn);
                cmd.ExecuteNonQuery();
            }
            {
                Console.WriteLine("5");
                string callProc = "call multi();";
                var cmd = new MySqlCommand(callProc, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine("6");
                    //we read each row from 
                    int data1 = reader.GetInt32(0);
                }
                Console.WriteLine("7");
                reader.Close();
            }
            //--------------------------
            conn.Close();
            Report.WriteLine("ok");

        }
    }
}