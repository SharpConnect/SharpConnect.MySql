//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect.MySql.SyncPatt;
namespace MySqlTest
{
    public class TestSet_Json : MySqlTestSet
    {
        [Test]
        public static void T_CreateDatabaseInfo_Manual()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();
            TestJson1(conn);
            TestJson2(conn);
            TestJson3(conn);
            TestJson4(conn);
            TestJson5(conn);
            conn.Close();
        }
        /// <summary>
        /// ''=> ', '=> ", `=> '
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static string SwapQuote(string input)
        {
            char[] buff = input.ToCharArray();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < buff.Length; ++i)
            {
                char c = buff[i];
                //2 consecutive '' => replace with 1 quote '
                //1 quote => replaced with double quote
                //` => replace with '

                if (c == '\'')
                {
                    //check next 
                    if (i + 1 < buff.Length)
                    {
                        //2 consecutive '' => replace with 1 quote '
                        char c_next = buff[i + 1];
                        if (c_next == '\'')
                        {
                            sb.Append('\'');
                            i++;
                        }
                        else
                        {
                            //1 quote => replaced with double quote
                            sb.Append('"');
                        }
                    }
                    else
                    {
                        sb.Append('"');
                    }
                }
                else if (c == '`')
                {
                    sb.Append('\'');
                }
                else
                {
                    sb.Append(c);
                }

            }
            return sb.ToString();
        }
        static void TestJson1(MySqlConnection conn)
        {
            //from https://dev.mysql.com/doc/refman/5.7/en/json.html

            string[] test_some_json_func = new string[] {
               
                //
                SwapQuote("SELECT JSON_KEYS(`{'a': 1, 'b': {'c': 30}}`, '$.b')"),
                SwapQuote("SELECT JSON_KEYS(''{'a': 1, 'b': {'c': 30}}'')"),
               
                //
                "SELECT JSON_EXTRACT('{\"id\": 14, \"name\": \"Aztalan\"}', '$.name')",
                "SELECT JSON_SET('\"x\"', '$[0]', 'a')",

                "SELECT JSON_MERGE('[\"a\", 1]', '{\"key\": \"value\"}')",
                "SELECT JSON_TYPE('[\"a\", \"b\", 1]')",
                "SELECT JSON_ARRAY('a', 1, NOW())",
                "SELECT JSON_OBJECT('key1', 1, 'key2', 'abc', 'key1', 'def')",

            };


            for (int i = 0; i < test_some_json_func.Length; ++i)
            {
                MySqlCommand cmd = new MySqlCommand(test_some_json_func[i], conn);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string data = reader.GetString(0);
                    System.Diagnostics.Debug.WriteLine(data);
                }
                reader.Close();
            }
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
            new MySqlCommand("INSERT INTO facts VALUES  ('{\"mascot\": \"Our mascot is a dolphin named  \\\\\"A\\\\\"\"}')", conn).ExecuteNonQuery();
            //SELECT sentence->"$.mascot" FROM facts;
            {
                //To look up this particular sentence employing mascot as the key, you can use the column-path operator ->, as shown here: 
                var cmd = new MySqlCommand("SELECT sentence->\"$.mascot\" FROM facts", conn);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string data = reader.GetString(0);
                    System.Diagnostics.Debug.WriteLine(data);
                }
                reader.Close();
            }
            {
                //This leaves the backslashes intact, along with the surrounding quote marks.
                //To display the desired value using mascot as the key, but without including the surrounding quote marks or any escapes, 
                //use the inline path operator ->>, like this: 

                //mysql> SELECT sentence->>"$.mascot" FROM facts;

                var cmd = new MySqlCommand("SELECT sentence->>\"$.mascot\" FROM facts", conn);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string data = reader.GetString(0);
                    System.Diagnostics.Debug.WriteLine(data);
                }
                reader.Close();
            }
        }
        static void TestJson3(MySqlConnection conn)
        {
            //       mysql> INSERT INTO t1 VALUES
            //>     ('{"x": 17, "x": "red"}'),
            //>     ('{"x": 17, "x": "red", "x": [3, 5, 7]}');

            //expected output
            //+-----------+
            //| c1        |
            //+-----------+
            //| {"x": 17} |
            //| {"x": 17} |
            //+-----------+
            {
                var cmd = new MySqlCommand("INSERT INTO facts VALUES"
                   + " ('{\"x\": 17, \"x\": \"red\"}')," + //1st row
                      "('{\"x\": 17, \"x\": \"red\", \"x\": [3, 5, 7]}');" //2nd row
                    , conn).ExecuteNonQuery();
            }

        }
        static void TestJson4(MySqlConnection conn)
        {
            //------------
            //    mysql> SET @j = '{"a": 1, "b": 2, "c": {"d": 4}}';
            //mysql > SET @j2 = '1';
            //mysql > SELECT JSON_CONTAINS(@j, @j2, '$.a');
            //+-------------------------------+
            //| JSON_CONTAINS(@j, @j2, '$.a') |
            //+-------------------------------+
            //| 1 |
            //+-------------------------------+

            //,
            //    "SET @j2 = '1'",
            {
                string[] test_some_json_func = new string[] {
                  SwapQuote("SET @j = ''{'a': 1, 'b': 2, 'c': {'d': 4}}'';"),
                  "SET @j2 = '1';"
                };

                for (int i = 0; i < test_some_json_func.Length; ++i)
                {
                    SqlStringTemplate sqlTemplate = new SqlStringTemplate(test_some_json_func[i], false);
                    new MySqlCommand(sqlTemplate, conn).ExecuteNonQuery();
                }
            }

            {
                SqlStringTemplate sqlTemplate = new SqlStringTemplate("SELECT JSON_CONTAINS(@j, @j2, '$.a')", false);
                var cmd = new MySqlCommand(sqlTemplate, conn);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string data = reader.GetString(0);
                    System.Diagnostics.Debug.WriteLine(data);
                }
                reader.Close();
            }
        }
        static void TestJson5(MySqlConnection conn)
        {
            //from https://dev.mysql.com/doc/refman/5.7/en/json-search-functions.html
            {
                //mysql > SET @j = '{"a": 1, "b": 2, "c": {"d": 4}}';
                //mysql > SELECT JSON_CONTAINS_PATH(@j, 'one', '$.a', '$.e');
                //+---------------------------------------------+
                //| JSON_CONTAINS_PATH(@j, 'one', '$.a', '$.e') |
                //+---------------------------------------------+
                //|                                           1 |
                //+---------------------------------------------+
                //mysql > SELECT JSON_CONTAINS_PATH(@j, 'all', '$.a', '$.e');
                //+---------------------------------------------+
                //| JSON_CONTAINS_PATH(@j, 'all', '$.a', '$.e') |
                //+---------------------------------------------+
                //| 0 |
                //+---------------------------------------------+
                //mysql > SELECT JSON_CONTAINS_PATH(@j, 'one', '$.c.d');
                //+----------------------------------------+
                //| JSON_CONTAINS_PATH(@j, 'one', '$.c.d') |
                //+----------------------------------------+
                //| 1 |
                //+----------------------------------------+
                //mysql > SELECT JSON_CONTAINS_PATH(@j, 'one', '$.a.d');
                //+----------------------------------------+
                //| JSON_CONTAINS_PATH(@j, 'one', '$.a.d') |
                //+----------------------------------------+
                //| 0 |
                //+----------------------------------------+


                string[] test_some_json_func = new string[] {
                    SwapQuote("SET @j = ''{'a': 1, 'b': 2, 'c': {'d': 4}}'';")
                };

                for (int i = 0; i < test_some_json_func.Length; ++i)
                {
                    SqlStringTemplate sqlTemplate = new SqlStringTemplate(test_some_json_func[i], false);
                    new MySqlCommand(sqlTemplate, conn).ExecuteNonQuery();
                }
            }

            {
                string[] select_sqls = new string[]
                {
                    "SELECT JSON_CONTAINS_PATH(@j, 'one', '$.a', '$.e');",
                    "SELECT JSON_CONTAINS_PATH(@j, 'all', '$.a', '$.e');",
                    "SELECT JSON_CONTAINS_PATH(@j, 'one', '$.c.d');",
                    "SELECT JSON_CONTAINS_PATH(@j, 'one', '$.a.d');"
                };

                for (int i = 0; i < select_sqls.Length; ++i)
                {
                    SqlStringTemplate sqlTemplate = new SqlStringTemplate(select_sqls[i], false);
                    var cmd = new MySqlCommand(sqlTemplate, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string data = reader.GetString(0);
                        System.Diagnostics.Debug.WriteLine(data);
                    }
                    reader.Close();
                }
            }
        }

    }
}