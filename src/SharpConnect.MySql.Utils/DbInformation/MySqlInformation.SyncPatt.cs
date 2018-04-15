//MIT, 2016-2018, brezza92, EngineKit and contributors

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
            Dictionary<string, MySqlDatabaseInfo> databaseList = new Dictionary<string, MySqlDatabaseInfo>();
            while (reader.Read())
            {
                //read database name
                string dbInfoName = reader.GetString(0);
                databaseList.Add(dbInfoName.ToUpper(), new MySqlDatabaseInfo(dbInfoName) { OwnerDbServer = dbServer });
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
                foreach (MySqlTableInfo tbl in tableInfoList)
                {
                    tbl.ReloadColumnList(conn);
                }
            }
        }
        
        public static void ReloadStoreProcList(this MySqlDatabaseInfo db, MySqlConnection conn, bool readDetail = false)
        {

            Use(db, conn);
            //-----------------------------------
            //To find out what tables the default database contains
            var cmd = new MySqlCommand("show procedure status where db=?db", conn);
            List<MySqlStoreProcInfo> storeProcList = new List<MySqlStoreProcInfo>();
            cmd.Parameters.AddWithValue("?db", db.Name);
            var reader = cmd.ExecuteReader();


            int ord_name = reader.GetOrdinal("Name");
            int ord_type = reader.GetOrdinal("Type");
            int ord_definer = reader.GetOrdinal("Definer");
            int ord_modifed = reader.GetOrdinal("Modified");
            int ord_created = reader.GetOrdinal("Created");

            while (reader.Read())
            {
                MySqlStoreProcInfo storeProc = new MySqlStoreProcInfo(reader.GetString("Name"));
                storeProc.OwnerDatabase = db;
                storeProcList.Add(storeProc);
            }
            reader.Close();
            //-------------
            db.StoreProcs = storeProcList;
            //------------
            if (readDetail)
            {
                foreach (MySqlStoreProcInfo proc in storeProcList)
                {
                    proc.Sql = proc.GetShowCreateStoreProcSql(conn);
                }
            }
        }
        public static void ReloadStoreFuncList(this MySqlDatabaseInfo db, MySqlConnection conn, bool readDetail = false)
        {

            Use(db, conn);
            //-----------------------------------
            //To find out what tables the default database contains
            var cmd = new MySqlCommand("show function status where db=?db", conn);
            List<MySqlStoreFuncInfo> storeFuncList = new List<MySqlStoreFuncInfo>();
            cmd.Parameters.AddWithValue("?db", db.Name);
            var reader = cmd.ExecuteReader(); 

            int ord_name = reader.GetOrdinal("Name");
            int ord_type = reader.GetOrdinal("Type");
            int ord_definer = reader.GetOrdinal("Definer");
            int ord_modifed = reader.GetOrdinal("Modified");
            int ord_created = reader.GetOrdinal("Created");

            while (reader.Read())
            {
                MySqlStoreFuncInfo storeProc = new MySqlStoreFuncInfo(reader.GetString("Name"));
                storeProc.OwnerDatabase = db;
                storeFuncList.Add(storeProc);
            }
            reader.Close();
            //-------------
            db.StoreFuncs = storeFuncList;

            //------------

            if (readDetail)
            {
                foreach (MySqlStoreFuncInfo func in storeFuncList)
                {
                    func.Sql = func.GetShowCreateStoreFunctionSql(conn);
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
            int ord_createTable = reader.GetOrdinal("Create Table");

            string createTableSql = null;
            reader.StringConverter = null;
            while (reader.Read())
            {
                createTableSql = reader.GetString(ord_createTable);
            }
            reader.Close();
            return createTableSql;
        }

        public static string GetShowCreateStoreProcSql(this MySqlStoreProcInfo storeFunc, MySqlConnection conn)
        {
            var cmd = new MySqlCommand("show create procedure " + storeFunc.Name, conn);
            var reader = cmd.ExecuteReader();
            string createTableSql = null;
            int ord_createProc = reader.GetOrdinal("Create Procedure");
            while (reader.Read())
            {
                createTableSql = reader.GetString(ord_createProc);
            }
            reader.Close();
            return createTableSql;
        }
        public static string GetShowCreateStoreFunctionSql(this MySqlStoreFuncInfo storeFunc, MySqlConnection conn)
        {
            var cmd = new MySqlCommand("show create function " + storeFunc.Name, conn);
            var reader = cmd.ExecuteReader();
            string createTableSql = null;

            int ord_createFunc = reader.GetOrdinal("Create Function");
            while (reader.Read())
            {
                createTableSql = reader.GetString(ord_createFunc);
            }
            reader.Close();
            return createTableSql;
        }
    }

}