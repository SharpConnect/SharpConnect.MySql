//MIT, 2015-2016, brezza92, EngineKit and contributors

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
                conn.Close(() => { });
            });
            //conn.Close();
        }
    }
    public class TestSet3_1_AsyncSocket : MySqlTestSet
    {
        [Test]
        public static void T_Select_sysdate2()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open(() =>
            {
                var cmd = new MySqlCommand("select sysdate()", conn);
                cmd.ExecuteReader(reader =>
                {
                    if (reader.Read())
                    {
                        var dtm = reader.GetDateTime(0);
                    }
                    reader.Close(() =>
                    {
                        conn.Close(() => { });
                    });
                });
            });

        }
    }
}