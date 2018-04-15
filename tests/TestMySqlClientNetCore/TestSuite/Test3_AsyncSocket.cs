//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect.MySql.AsyncPatt;
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