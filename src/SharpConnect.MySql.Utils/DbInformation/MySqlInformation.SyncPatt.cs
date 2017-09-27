//MIT, 2015-2017, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect.MySql.Information;
namespace SharpConnect.MySql.SyncPatt
{
    public static class MySqlDbInformationExtension
    {
        /// <summary>
        /// reload database name from specific server
        /// </summary>
        /// <param name="dbServer"></param>
        /// <param name="conn"></param>
        public static void ReloadDatabaseList(this MySqlDbServerInfo dbServer, MySqlConnection conn)
        {
            var cmd = new MySqlCommand("show databases", conn);
            var reader = cmd.ExecuteReader();
            List<MySqlDatabaseInfo> databaseList = new List<MySqlDatabaseInfo>();
            while (reader.Read())
            {
                //read database name
                databaseList.Add(new MySqlDatabaseInfo(reader.GetString(0)) { OwnerDbServer = dbServer });
            }
            reader.Close();
            //----------
            dbServer.Databases = databaseList;
        }

        /// <summary>
        /// set input db as current db, 'use' command
        /// </summary>
        /// <param name="db"></param>
        /// <param name="conn"></param>
        public static void Use(this MySqlDatabaseInfo db, MySqlConnection conn)
        {
            var cmd = new MySqlCommand("use " + db.Name, conn);
            cmd.ExecuteNonQuery();
        }
        /// <summary>
        /// reload table in specific database
        /// </summary>
        /// <param name="db"></param>
        /// <param name="conn"></param>
        public static void ReloadTableList(this MySqlDatabaseInfo db, MySqlConnection conn, bool readTableDetail = false)
        {

            Use(db, conn);
            //-----------------------------------
            //To find out what tables the default database contains
            var cmd = new MySqlCommand("show tables", conn);
            List<MySqlTableInfo> tableInfoList = new List<MySqlTableInfo>();
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                tableInfoList.Add(new MySqlTableInfo(reader.GetString(0)) { OwnerDatabase = db });
            }
            reader.Close();
            //-------------
            db.Tables = tableInfoList;
            if (readTableDetail)
            {
                foreach (var tbl in tableInfoList)
                {
                    tbl.ReloadColumnList(conn);
                }
            }
        }

        /// <summary>
        /// reload table in specific database
        /// </summary>
        /// <param name="table"></param>
        /// <param name="conn"></param>
        public static void ReloadColumnList(this MySqlTableInfo table, MySqlConnection conn)
        {
            var cmd = new MySqlCommand("describe " + table.Name, conn);
            var descReader = cmd.ExecuteReader();
            //columns
            //Field..
            //Type
            //Null
            //Key
            //Default,
            //Extra 
            int field_col = descReader.GetOrdinal("Field"),
            type_col = descReader.GetOrdinal("Type"),
            null_col = descReader.GetOrdinal("Null"),
            key_col = descReader.GetOrdinal("Key"),
            default_col = descReader.GetOrdinal("Default"),
            extra_col = descReader.GetOrdinal("Extra");
            //---------------------
            List<MySqlColumnInfo> colInfoList = new List<MySqlColumnInfo>();
            while (descReader.Read())
            {
                //
                MySqlColumnInfo colInfo = new MySqlColumnInfo();
                colInfo.Name = descReader.GetString(field_col);
                colInfo.FieldTypeName = descReader.GetString(type_col);
                colInfo.Nullable = descReader.GetString(null_col) == "YES";
                colInfo.Key = descReader.GetString(key_col);
                colInfo.DefaultValue = descReader.GetString(default_col);
                colInfo.ExtraInfo = descReader.GetString(extra_col);
                //
                colInfoList.Add(colInfo);
            }
            descReader.Close();
            table.Columns = colInfoList;
        }

        /// <summary>
        /// call show create table command 
        /// </summary>
        /// <param name="table"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static string GetShowCreateTableSql(this MySqlTableInfo table, MySqlConnection conn)
        {
            var cmd = new MySqlCommand("show create table " + table.Name, conn);
            var reader = cmd.ExecuteReader();
            string createTableSql = null;
            while (reader.Read())
            {
                createTableSql = reader.GetString(1);
            }
            reader.Close();
            return createTableSql;
        }
    }

}