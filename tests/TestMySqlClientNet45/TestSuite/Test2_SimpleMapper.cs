//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect.MySql.Mapper;
 
using SharpConnect.MySql.SyncPatt;

namespace MySqlTest
{
    public class TestSet2_SimpleMapper : MySqlTestSet
    {
        [Test]
        public static void T_InsertAndSelect()
        {



            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.UseConnectionPool = true;
            conn.Open();

            DropTableIfExists(conn);
            CreateTable(conn);
            for (int i = 0; i < 100; ++i)
            {
                InsertData(conn);
            }
            SelectDataBack(conn);
            conn.Close();


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
            var mapper = Mapper.Map((SimpleInfo t, int col_id, string col2, string col3) =>
            {
                t.col1 = col_id;
                t.col2 = col2;
            });
            mapper.DataReader = reader;
            while (reader.Read())
            {
                //simple map query result to member of the target object  
                //we create simpleinfo and use mapper to map field 
                var simpleInfo = mapper.Map(new SimpleInfo());
            }
            reader.Close();
        }

        class SimpleInfo
        {
            public int col1;
            public string col2;
        }
    } 

}