using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using SharpConnect.MySql;

namespace MySqlTest
{
    public class TestSet1 : MySqlTester
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
        static MySqlConnectionString GetMySqlConnString()
        {
            string h = "127.0.0.1";
            string u = "root";
            string p = "root";
            string d = "test";
            return new MySqlConnectionString(h, u, p, d);
        }
    }
}