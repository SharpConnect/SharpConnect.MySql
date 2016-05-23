//MIT 2015, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
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

            uint lastInsertId = 0;
            int testdata_crc32 = 0;
            {
                string sql = "insert into test001(mydata) values(?mydata)";
                var cmd = new MySqlCommand(sql, conn);
                cmd.Prepare();
                //create sample blob
                byte[] data = CreateTestData();
                testdata_crc32 = SharpConnect.CRC32Calculator.CalculateCrc32(data);
                cmd.Parameters.AddWithValue("?mydata", data);
                cmd.ExecuteNonQuery();
                lastInsertId = cmd.LastInsertId;
            }

            {
                if (lastInsertId > 0)
                {
                    //test download back
                    string sql = "select mydata from test001 where col_id=?col_id";
                    var cmd = new MySqlCommand(sql, conn);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("?col_id", lastInsertId);
                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        byte[] dataBuffer = reader.GetBuffer(0);
                        //test return back check sum
                        if (testdata_crc32 != SharpConnect.CRC32Calculator.CalculateCrc32(dataBuffer))
                        {
                        }
                    }
                    reader.Close();

                }
            }
            conn.Close();
            Report.WriteLine("ok");
        }
        static byte[] CreateTestData()
        {
            int datasize = 1024 * 1000 * 45;
            byte[] data = new byte[1024 * 1000 * 45];
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