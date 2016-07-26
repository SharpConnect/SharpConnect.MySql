//MIT, 2015-2016, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect.MySql.Mapper;
namespace MySqlTest
{
    public class TestSet2_SimpleMapper : MySqlTestSet
    {
        [Test]
        public static void T_InsertAndSelect()
        {

            try
            {

                var connStr = GetMySqlConnString();
                var conn = new MySqlConnection(connStr);
                conn.UseConnectionPool = true;
                conn.Open();

                DropTableIfExists(conn);
                CreateTable(conn);
                for (int i = 0; i < 2000; ++i)
                {
                    InsertData(conn);
                }
                SelectDataBack(conn);
                conn.Close();

            }
            catch (Exception ex)
            {

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
            //this is very basic mapper***
            var reader = cmd.ExecuteReader();
            //------------------------------------------------------

            var mapper = Mapper.Map((object target, int col_id) =>
            {

            });
            //------------------------------------------------------
            //create sample record
            //var targetRecordSample = new { col_id = 0, col1 = 0, col2 = "", col3 = "", colo4 = DateTime.MinValue };
            while (reader.Read())
            {
                //simple map query result to member of the target object
                //reader.NewRecordLike(targetRecordSample);
                 
            }
            reader.Close();
        }
    }
}