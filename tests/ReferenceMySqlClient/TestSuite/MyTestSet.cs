//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;

namespace MySqlTest
{
    public abstract class MySqlTestSet : MySqlTesterBase
    {
        protected static string GetMySqlConnString()
        {
            string h = "127.0.0.1";
            string u = "root";
            string p = "mysqldev";
            string d = "test";
            return "server = " + h + "; user = " + u + "; database = " + d + "; password = " + p + ";";            
        }
    }
}