//MIT 2015, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;

namespace MySqlTest
{
    public abstract class MySqlTestSet : MySqlTesterBase
    {

        protected static MySqlConnectionString GetMySqlConnString()
        {
            string h = "127.0.0.1";
            string u = "root";
            string p = "123";
            string d = "test";
            return new MySqlConnectionString(h, u, p, d);
        }

    }
}