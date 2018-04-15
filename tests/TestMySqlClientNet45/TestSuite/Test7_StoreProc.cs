//MIT, 2015-2018, brezza92, EngineKit and contributors
using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect.MySql.SyncPatt;
using SharpConnect.MySql.AsyncPatt;

namespace MySqlTest
{
    public class Test_StoreProc_MultiResultSet : MySqlTestSet
    {
        [Test]
        public static void T_StoreProcMultiResultSet()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);

            //Console.WriteLine("1");
            conn.Open();
            // Console.WriteLine("2");
            {
                string createStoreProcSql = @"DROP PROCEDURE IF EXISTS multi;";
                var cmd = new MySqlCommand(createStoreProcSql, conn);
                // Console.WriteLine("3");
                cmd.ExecuteNonQuery();
            }
            {
                string createStoreProcSql = @"CREATE PROCEDURE multi() BEGIN
                              SELECT 1 as A;
                              SELECT 2 as B;
                              END";
                //Console.WriteLine("4");
                var cmd = new MySqlCommand(createStoreProcSql, conn);
                cmd.ExecuteNonQuery();
            }
            {
                // Console.WriteLine("5");
                string callProc = "call multi();";
                var cmd = new MySqlCommand(callProc, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {

                    // Console.WriteLine("6");
                    //we read each row from 
                    int data1 = reader.GetInt32(0);
                }
                //Console.WriteLine("7");
                reader.Close();
            }
            //--------------------------
            conn.Close();
            // Report.WriteLine("ok");

        }

        [Test]
        public static void T_StoreProcMultiResultSet2()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);

            //Console.WriteLine("1");
            conn.Open();
            PrepareTable1(conn);
            // Console.WriteLine("2");
            {
                string createStoreProcSql = @"DROP PROCEDURE IF EXISTS multi;";
                var cmd = new MySqlCommand(createStoreProcSql, conn);
                // Console.WriteLine("3");
                cmd.ExecuteNonQuery();
            }
            {
                string createStoreProcSql = @"CREATE PROCEDURE multi() BEGIN
                              SELECT 1011 as A;
                              SELECT 1022 as B;
                              select col_id from test001;
                              END";
                //Console.WriteLine("4");
                var cmd = new MySqlCommand(createStoreProcSql, conn);
                cmd.ExecuteNonQuery();
            }
            {
                // Console.WriteLine("5");
                string callProc = "call multi();";
                var cmd = new MySqlCommand(callProc, conn);
                var reader = cmd.ExecuteReader();
                //access to sub table 
                MySqlSubTable currentSubTable;
                while (reader.Read())
                {
                    MySqlSubTable subTable = reader.CurrentSubTable;
                    if (subTable != currentSubTable)
                    {
                        //change to new table
                        currentSubTable = subTable;

                    }
                    // Console.WriteLine("6");
                    //we read each row from 
                    int data1 = reader.GetInt32(0);
                    Console.WriteLine(data1);
                }
                //Console.WriteLine("7");
                reader.Close();
            }
            //--------------------------
            conn.Close();
            // Report.WriteLine("ok"); 
        }

        [Test]
        public static void T_StoreProcMultiResultSet3()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);

            //Console.WriteLine("1");
            conn.Open();
            PrepareTable1(conn);
            // Console.WriteLine("2");
            {
                string createStoreProcSql = @"DROP PROCEDURE IF EXISTS multi;";
                var cmd = new MySqlCommand(createStoreProcSql, conn);
                // Console.WriteLine("3");
                cmd.ExecuteNonQuery();
            }
            {
                string createStoreProcSql = @"CREATE PROCEDURE multi() BEGIN
                              SELECT 1011 as A;
                              SELECT 1022 as B;
                              select col_id from test001;
                              END";
                //Console.WriteLine("4");
                var cmd = new MySqlCommand(createStoreProcSql, conn);
                cmd.ExecuteNonQuery();
            }
            {
                // Console.WriteLine("5");
                string callProc = "call multi();";
                var cmd = new MySqlCommand(callProc, conn);

                //access to sub table
                var currentSubTable = MySqlSubTable.Empty;
                
                cmd.ExecuteSubTableReader(reader =>
                {
                    if (reader.CurrentSubTable.Header != currentSubTable.Header)
                    {
                        //change main table
                        //some table may split into many sub table
                    }
                    currentSubTable = reader.CurrentSubTable;
                    //on each subtable
                    //create data reader for the subtable
                    while (reader.Read())
                    {
                        Console.WriteLine(reader.GetInt32(0));
                    }

                    //last table

                    if (currentSubTable.IsLastTable)
                    {
                        conn.Close();
                    }
                });

            }

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
            uint lastInsertId = cmd.LastInsertedId;
        }
        static void SelectDataBack(MySqlConnection conn)
        {
            string sql = "select * from test001";
            var cmd = new MySqlCommand(sql, conn);
#if DEBUG
            conn.dbugPleaseBreak = true;
#endif
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                //test immediate close
                reader.Close();
            }
            reader.Close();
        }
        static void PrepareTable1(MySqlConnection conn)
        {
            DropTableIfExists(conn);
            CreateTable(conn);
            for (int i = 0; i < 100; ++i)
            {
                InsertData(conn);
            }

        }
    }
}