//MIT, 2020-present, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect.MySql.SyncPatt;
using System.Text;
using System.Text.Json;

namespace MySqlTest
{

    static class TestSet_JsonHelper
    {
        public static T GetObjectFromJson<T>(this MySqlDataReader r, int index) => JsonSerializer.Deserialize<T>(r.GetString(index));
        public static void GetObjectFromJson<T>(this MySqlDataReader r, int index, out T output)
        {
            output = JsonSerializer.Deserialize<T>(r.GetString(index));
        }
    }

    public class TestSet_Json9_1 : MySqlTestSet
    {
        [Test]
        public static void T_TestJson2()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();
            TestJson2(conn);
            conn.Close();
        }


        class MyDataObject
        {
            public string mascot { get; set; }
        }

        static string EscapeMore(string jsonstr)
        {
            //why we need this see => https://dev.mysql.com/doc/refman/5.7/en/json.html
            StringBuilder sb = new StringBuilder();
            char[] buff = jsonstr.ToCharArray();
            for (int i = 0; i < buff.Length; ++i)
            {
                char c0 = buff[i];
                if (c0 == '\\')
                {
                    //check next char
                    sb.Append("\\\\");
                }
                else
                {
                    sb.Append(c0);
                }
            }
            return sb.ToString();
        }

        static void TestJson2(MySqlConnection conn)
        {
            new MySqlCommand("drop table if exists facts", conn).ExecuteNonQuery();
            new MySqlCommand(" CREATE TABLE facts (sentence JSON)", conn).ExecuteNonQuery();
            //--------------
            //new MySqlCommand("INSERT INTO facts VALUES  (JSON_OBJECT('mascot', 'Our mascot is a dolphin named \"Sakila\".'))", conn).ExecuteNonQuery();


            //from https://dev.mysql.com/doc/refman/5.7/en/json.html
            //mysql> INSERT INTO facts VALUES
            //> ('{"mascot": "Our mascot is a dolphin named \\"Sakila\\"."}');

            //C#, note that \=> \\ ,"=> \"
            //so ...

            MyDataObject mascot1 = new MyDataObject { mascot = "Our mascot is a dolphin named \"A\"" };
            var options = new JsonSerializerOptions
            {
                //SOME WARNING: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to?view=netcore-3.1#customize-character-encoding
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };



            //original
            //new MySqlCommand("INSERT INTO facts VALUES  ('{\"mascot\": \"Our mascot is a dolphin named  \\\\\"A\\\\\"\"}')", conn).ExecuteNonQuery(); 
            //----------------------
            {
                string json_str = EscapeMore(JsonSerializer.Serialize(mascot1, options));
                //1. insert 
                new MySqlCommand($"INSERT INTO facts VALUES  ('{json_str}')", conn).ExecuteNonQuery();

                //2.
                var cmd = new MySqlCommand("SELECT sentence FROM facts", conn);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string data = reader.GetString(0);
                    System.Diagnostics.Debug.WriteLine(data);

                    var my_obj = JsonSerializer.Deserialize<MyDataObject>(data);
                }
                reader.Close();
            }


            //----------------------
            {
                //1. insert
                var cmd = new MySqlCommand("INSERT INTO facts VALUES  (?data)", conn);
                cmd.Parameters.AddWithValue("?data", EscapeMore(JsonSerializer.Serialize(mascot1, options)));
                cmd.ExecuteNonQuery();

                //2. select
                var cmd2 = new MySqlCommand("SELECT sentence FROM facts", conn);
                MySqlDataReader reader = cmd2.ExecuteReader();
                while (reader.Read())
                {
                    //
                    string data = reader.GetString(0);
                    System.Diagnostics.Debug.WriteLine(data);
                    var my_obj = JsonSerializer.Deserialize<MyDataObject>(data);
                    //
                    var my_obj1 = reader.GetObjectFromJson<MyDataObject>(0);
                    //
                    reader.GetObjectFromJson(0, out MyDataObject my_obj2);
                }
                reader.Close();
            }
        }
    }
}