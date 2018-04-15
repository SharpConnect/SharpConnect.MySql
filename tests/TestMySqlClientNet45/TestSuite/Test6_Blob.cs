//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect.MySql.SyncPatt;
namespace MySqlTest
{
    public class TestSet_Blob : MySqlTestSet
    {
        [Test]
        public static void T_InsertBlobData()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();
            conn.UpdateMaxAllowPacket();
            //DropIfExist(conn);
            //CreateNewTable(conn);
            //InsertMore(conn);
            //if (ReadAll(conn))
            //{
            //    return;
            //}
            {
                string sql = "drop table if exists test001";
                var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }

            {
                string sql = "create table test001(col_id int(10) unsigned not null auto_increment, mydata longblob,primary key(col_id)) ENGINE=MyISAM DEFAULT CHARSET=latin1";
                var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
            //create sample blob
            byte[] data = CreateTestData();
            uint lastInsertId = 0;
            //int testdata_crc32 = 0;
            {
                string sql = "insert into test001(mydata) values(?mydata)";
                var cmd = new MySqlCommand(sql, conn);
                cmd.Prepare();
                //testdata_crc32 = SharpConnect.CRC32Calculator.CalculateCrc32(data);
                cmd.Parameters.AddWithValue("?mydata", data);
                cmd.ExecuteNonQuery();
                //for (int i = 0; i < 5; i++)
                //{
                //    cmd.ExecuteNonQuery();
                //}
                lastInsertId = cmd.LastInsertedId;
            }

            {
                if (lastInsertId > 0)
                {
                    //test download back
                    string sql = "select mydata from test001 where col_id<=?col_id";
                    var cmd = new MySqlCommand(sql, conn);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("?col_id", lastInsertId);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        byte[] dataBuffer = reader.GetBuffer(0);
                        if (Match(data, dataBuffer))
                        {
                            Console.WriteLine("All Matching!!!");
                        }
                        else
                        {
                            Console.WriteLine("Some byte not match!!");
                        }
                    }
                    reader.Close();

                }
            }
            conn.Close();
            Report.WriteLine("ok");
        }

        static void DropIfExist(MySqlConnection conn)
        {
            string sql = "drop table if exists testmore";
            var cmd = new MySqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        static void CreateNewTable(MySqlConnection conn)
        {
            string sql = "create table testmore(col_id int(10) unsigned not null auto_increment, mydata text,primary key(col_id)) ENGINE=MyISAM DEFAULT CHARSET=latin1";
            var cmd = new MySqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        static void InsertMore(MySqlConnection conn)
        {
            string data = "a";//1 char
            data = "aaaaaaaaaa";//10 char
            data += data + data + data + data;//50 char
            data += data;//100 char
            uint lastInsertId = 0;
            {
                string sql = "insert into testmore(mydata) values(?mydata)";
                var cmd = new MySqlCommand(sql, conn);
                cmd.Prepare();
                for (int i = 0; i < 50; i++)
                {
                    cmd.Parameters.AddWithValue("?mydata", data);
                    //cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                    data += "a";
                    //sql = "insert into testmore(mydata) values(\"" + data + "\")";
                }
                lastInsertId = cmd.LastInsertedId;
            }
        }

        static bool ReadAll(MySqlConnection conn)
        {
            string sql = "select mydata from testmore";
            var cmd = new MySqlCommand(sql, conn);
            //cmd.Prepare();
            var reader = cmd.ExecuteReader();
            string data = "";
            int count = 0;
            while (reader.Read())
            {
                data = reader.GetString(0);
                Console.WriteLine("data[" + (++count) + "] : " + data);
            }
            reader.Close();
            return true;
        }

        static bool Match(byte[] input1, byte[] input2)
        {
            if (input1.Length != input2.Length)
            {
                return false;
            }
            int errCount = 0;
            bool result = true;
            for (int i = 0; i < input1.Length; i++)
            {
                if (input1[i] != input2[i])
                {
                    ++errCount;
                    result = false;
                    Console.WriteLine("Error at index " + i + " input1=" + input1[i] + ", input2=" + input2[i]);
                    if (errCount > 20)
                    {
                        break;
                    }
                }
            }
            return result;
        }

        static byte[] CreateTestData()
        {
            int datasize = 1024 * 1000 * 18;
            byte[] data = new byte[datasize];
            int count = 0;
            for (int i = datasize - 1; i >= 0; --i)
            {
                data[i] = (byte)++count;
                if (count > 253)
                {
                    count = 0;//reset
                }
            }
            return data;
        }
    }
}