//MIT, 2015-2017, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;

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
#if DEBUG
        public override string ToString()
        {
            return "server:" + Name;
        }
#endif
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
#if DEBUG
        public override string ToString()
        {
            return "db:" + Name;
        }
#endif
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
#if DEBUG
        public override string ToString()
        {
            return "table:" + Name;
        }
#endif
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
#if DEBUG
        public override string ToString()
        {
            return "col:" + Name;
        }
#endif
    }



}