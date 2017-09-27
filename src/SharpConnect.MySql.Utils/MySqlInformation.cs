//MIT, 2015-2017, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;
using SharpConnect.MySql.SyncPatt;

namespace SharpConnect.MySql.Information
{
    public class MySqlDbServerInfo
    {
        public MySqlDbServerInfo(string name)
        {
            this.Name = name;
        }
        /// <summary>
        /// name of this server
        /// </summary>
        public string Name { get; private set; }
        public List<MySqlDatabaseInfo> Databases { get; internal set; }
    }

    public class MySqlDatabaseInfo
    {
        public MySqlDatabaseInfo(string name)
        {
            this.Name = name;
        }
        public string Name { get; private set; }
        public List<MySqlTableInfo> Tables { get; internal set; }
        public MySqlDbServerInfo OwnerDbServer { get; internal set; }
    }


    public class MySqlTableInfo
    {
        public MySqlTableInfo(string name)
        {
            this.Name = name;
        }
        public string Name { get; private set; }
        public List<MySqlColumnInfo> Columns { get; internal set; }
        public MySqlDatabaseInfo OwnerDatabase { get; internal set; }
    }

    public class MySqlColumnInfo
    {
        public MySqlColumnInfo()
        {
        }
        public string FieldTypeName { get; set; }
        public string Name { get; set; }
        public bool Nullable { get; set; }
        public string Key { get; set; }
        public string DefaultValue { get; set; }
        public string ExtraInfo { get; set; }
    }



}