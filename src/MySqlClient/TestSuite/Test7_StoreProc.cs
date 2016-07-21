//MIT, 2015-2016, brezza92, EngineKit and contributors
using System;
using System.Collections.Generic;
using SharpConnect.MySql;

namespace MySqlTest
{
    public class Test_StoreProc_MultiResultSet : MySqlTestSet
    {
        [Test]
        public static void T_StoreProcMultiResultSet()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();

            {
                string createStoreProcSql = @"DROP PROCEDURE IF EXISTS multi;";
                var cmd = new MySqlCommand(createStoreProcSql, conn);
                cmd.ExecuteNonQuery();
            }
            {
                string createStoreProcSql = @"CREATE PROCEDURE multi() BEGIN
                              SELECT 1;
                              SELECT 2;
                              END";

                var cmd = new MySqlCommand(createStoreProcSql, conn);
                cmd.ExecuteNonQuery();
            }
            {

                string callProc = "call multi();";
                var cmd = new MySqlCommand(callProc, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    //we read each row from 
                    int data1 = reader.GetInt32(0);
                }
                reader.Close();

            }
            //--------------------------
            conn.Close();
            Report.WriteLine("ok");

        }
    }
}