//MIT 2015, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
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
            });
            //conn.Close();
        }
    }
}