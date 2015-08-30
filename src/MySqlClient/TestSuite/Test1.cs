//MIT 2015, brezza27, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;

namespace MySqlTest
{
    public class TestSet1 : MySqlTestSet
    {
        [Test]
        public static void T_OpenAndClose()
        {
            int n = 100;
            long total;
            long avg;
            var connStr = GetMySqlConnString();
            Test(n, TimeUnit.Ticks, out total, out avg, () =>
            {

                var conn = new MySqlConnection(connStr);
                conn.Open();
                //connList.Add(conn);
                conn.Close();
            });
            Report.WriteLine("avg:" + avg);
        }
        [Test]
        public static void T_OpenNotClose()
        {
            int n = 100;
            long total;
            long avg;
            var connStr = GetMySqlConnString();
            List<MySqlConnection> connList = new List<MySqlConnection>();
            Test(n, TimeUnit.Ticks, out total, out avg, () =>
            {

                var conn = new MySqlConnection(connStr);
                conn.Open();
                connList.Add(conn);
            });
            Report.WriteLine("avg:" + avg);

            //clear
            foreach (var conn in connList)
            {
                conn.Close();
            }
            connList.Clear();
        }
        [Test]
        public static void T_OpenAndCloseWithConnectionPool()
        {
            int n = 100;
            long total;
            long avg;
            var connStr = GetMySqlConnString();
            Test(n, TimeUnit.Ticks, out total, out avg, () =>
            {
                var conn = new MySqlConnection(connStr);
                conn.UseConnectionPool = true;
                conn.Open();
                conn.Close();
            });

            Report.WriteLine("avg:" + avg);
        }


        [Test]
        public static void T_Select_sysdate()
        {
            int n = 100;
            long total;
            long avg;
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();

            Test(n, TimeUnit.Ticks, out total, out avg, () =>
            {
                var cmd = new MySqlCommand("select sysdate()", conn);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var dtm = reader.GetDateTime(0);
                }
                reader.Close();
            });

            Report.WriteLine("avg:" + avg);

            conn.Close();
        }

        [Test]
        public static void T_CreateTable()
        {


            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();
            var cmd = new MySqlCommand("create table user_info2(uid int(10),u_name varchar(45));", conn);
            cmd.ExecuteNonQuery();
            Report.WriteLine("ok");
            conn.Close();
        }


        [Test]
        public static void T_StringEscape()
        {


            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();
            //1. drop table
            {
                var cmd = new MySqlCommand("drop table if exists user_info2", conn);
                cmd.ExecuteNonQuery();
            }
            //2. create new one
            {
                var cmd = new MySqlCommand("create table user_info2(uid int(10),u_name varchar(45));", conn);
                cmd.ExecuteNonQuery();
            }
            //3. add some data
            {
                var cmd = new MySqlCommand("insert into user_info2(uid, u_name) values(?uid, 'abc')", conn);
                cmd.Parameters.AddWithValue("uid", 10);
                cmd.ExecuteNonQuery();
            }

            Report.WriteLine("ok");
            conn.Close();
        }

    }
}