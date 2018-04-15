////MIT, 2015-2018, brezza92, EngineKit and contributors

//using System;
//using System.Collections.Generic;
//using SharpConnect.MySql;
//using SharpConnect.MySql.Utils;
//namespace MySqlTest
//{
//    public class TestSet5_SimpleUtils : MySqlTestSet
//    {
//        [Test]
//        public static void T_SimpleInsert_Update()
//        {
//            var connStr = GetMySqlConnString();
//            var conn = new MySqlConnection(connStr);
//            conn.Open();
//            {
//                string sql = "drop table if exists test001";
//                var cmd = new MySqlCommand(sql, conn);
//                cmd.ExecuteNonQuery();
//            }

//            {
//                string sql = "create table test001(first_name varchar(100),last_name varchar(100))";
//                var cmd = new MySqlCommand(sql, conn);
//                cmd.ExecuteNonQuery();
//            }
//            //---------------------------------------------------
//            {
//                var insert = new SimpleInsert("test001");
//                insert.AddWithValue("?first_name", "test1_firstname");
//                insert.AddWithValue("?last_name", "test1_last_name");
//                insert.ExecuteNonQuery(conn);
//            }

//            //---------------------------------------------------
//            {
//                //prepare
//                var insert = new SimpleInsert("test001");
//                insert.AddWithValue("?first_name", "");
//                insert.AddWithValue("?last_name", "");
//                insert.Prepare(conn);
//                for (int i = 0; i < 10; ++i)
//                {
//                    insert.ClearValues();
//                    insert.AddWithValue("?first_name", "first" + i);
//                    insert.AddWithValue("?last_name", "last" + i);
//                    insert.ExecuteNonQuery();
//                }
//            }
//            //--------------------------------------------------- 



//            //test update
//            //---------------------------------------------------
//            {
//                var update = new SimpleUpdate("test001");
//                update.AddWithValue("?first_name", "update_name");
//                update.Where("first_name = 'first0'");
//                update.Connection = conn;
//                update.ExecuteNonQuery();
//            }

//            //---------------------------------------------------

//            conn.Close();
//        }

//        [Test]
//        public static void T_SimpleSelect()
//        {
//            //-----------------------------
//            //prepare data 
//            T_SimpleInsert_Update();
//            //-----------------------------

//            var connStr = GetMySqlConnString();
//            var conn = new MySqlConnection(connStr);
//            conn.Open();
//            {
//                var select = new SimpleSelect("test001");
//                select.Connection = conn;
//                //------------------------------
//                //for anonyomous type
//                foreach (var d in select.ExecRecordIter(
//                    (r) => new { first_name = r.str(), last_name = r.str() }))
//                {
//                }
//                //------------------------------

//                foreach (var d in select.ExecRecordIter(() => new UserInfo()))
//                {
//                }
//                foreach (var d in select.ExecRecordIter(() => new UserInfo2()))
//                {
//                }
//            }

//            conn.Close();
//        }

//        sealed class UserInfo
//        {
//            public string first_name;
//            public string last_name;
//        }

//        sealed class UserInfo2
//        {
//            public string first_name { get; set; }
//            public string last_name { get; set; }
//        }
//    }
//}