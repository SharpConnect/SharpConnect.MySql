//MIT 2015, brezza27, EngineKit and contributors

using System;

using System.Collections.Generic;
using SharpConnect.MySql.Internal;

namespace SharpConnect.MySql
{
    public class CommandParams
    {
        Dictionary<string, MyStructData> _bindedValues;
        Dictionary<string, string> _bindSpecialKeyValues;
        MyStructData reuseData;

        public CommandParams()
        {
            _bindedValues = new Dictionary<string, MyStructData>();
            _bindSpecialKeyValues = new Dictionary<string, string>();
            reuseData = new MyStructData();
            reuseData.type = Types.NULL;
        }


        public void SetSpecialKey(string key, string tablename)
        {
            key = "??" + key;
            _bindSpecialKeyValues[key] = "`" + tablename + "`";
        }
        public void ClearBindValues()
        {
            _bindedValues.Clear();
        }
        public void ClearSpecialKeyValues()
        {
            _bindSpecialKeyValues.Clear();
        }

        public void AddWithValue(string key, string value)
        {
            if (value != null)
            {
                reuseData.myString = value;
                reuseData.type = Types.VAR_STRING;
            }
            else
            {
                reuseData.myString = null;
                reuseData.type = Types.NULL;
            }
            AddKeyWithReuseData(key);
        }
        public void AddWithValue(string key, byte value)
        {
            reuseData.myByte = value;
            reuseData.type = Types.BIT;
            AddKeyWithReuseData(key);
        }
        public void AddWithValue(string key, int value)
        {
            reuseData.myInt32 = value;
            reuseData.type = Types.LONG;//Types.LONG = int32
            AddKeyWithReuseData(key);
        }
        public void AddWithValue(string key, long value)
        {
            reuseData.myInt64 = value;
            reuseData.type = Types.LONGLONG;
            AddKeyWithReuseData(key);
        }
        public void AddWithValue(string key, float value)
        {
            reuseData.myFloat = value;
            reuseData.type = Types.FLOAT;
            AddKeyWithReuseData(key);
        }
        public void AddWithValue(string key, double value)
        {
            reuseData.myDouble = value;
            reuseData.type = Types.DOUBLE;
            AddKeyWithReuseData(key);
        }
        public void AddWithValue(string key, decimal value)
        {
            reuseData.myDecimal = value;
            reuseData.type = Types.DECIMAL;
            AddKeyWithReuseData(key);
        }
        public void AddWithValue(string key, byte[] value)
        {
            if (value != null)
            {
                reuseData.myBuffer = value;
                reuseData.type = Types.LONG_BLOB;
            }
            else
            {
                reuseData.myBuffer = null;
                reuseData.type = Types.NULL;
            }
            AddKeyWithReuseData(key);
        }
        public void AddWithValue(string key, DateTime value)
        {
            reuseData.myDateTime = value;
            reuseData.type = Types.DATETIME;
            AddKeyWithReuseData(key);
        }
        void AddKeyWithReuseData(string key)
        {
            key = "?" + key;
            _bindedValues[key] = reuseData;
        }

        internal MyStructData GetData(string key)
        {
            MyStructData value;
            _bindedValues.TryGetValue(key, out value);
            return value;
        }
        internal string GetSpecialKeyValue(string key)
        {
            string value;
            _bindSpecialKeyValues.TryGetValue(key, out value);
            return value;
        }
        internal bool IsValueKeys(string key)
        {
            return _bindedValues.ContainsKey(key);
        }
    }
}