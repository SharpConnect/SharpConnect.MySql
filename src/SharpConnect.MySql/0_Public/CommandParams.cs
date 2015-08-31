//MIT 2015, brezza27, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql.Internal;

namespace SharpConnect.MySql
{
    public class CommandParams
    {

        Dictionary<string, MyStructData> _values = new Dictionary<string, MyStructData>(); //user bound values
        Dictionary<string, string> _sqlParts;//null at first, special  extension
        public CommandParams()
        {
        }

        //-------------------------------------------------------
        //user's bound data values

        public void AddWithValue(string key, string value)
        {
            var data = new MyStructData();
            if (value != null)
            {
                data.myString = value;
                data.type = Types.VAR_STRING;
            }
            else
            {
                data.myString = null;
                data.type = Types.NULL;
            }
            _values["?" + key] = data;

        }
        public void AddWithValue(string key, byte value)
        {
            var data = new MyStructData();
            data.myInt32 = value;
            data.type = Types.BIT;
            _values["?" + key] = data;
        }
        public void AddWithValue(string key, int value)
        {
            var data = new MyStructData();
            data.myInt32 = value;
            data.type = Types.LONG;//Types.LONG = int32
            _values["?" + key] = data;
        }
        public void AddWithValue(string key, long value)
        {
            var data = new MyStructData();
            data.myInt64 = value;
            data.type = Types.LONGLONG;
            _values["?" + key] = data;
        }
        public void AddWithValue(string key, float value)
        {
            var data = new MyStructData();
            data.myDouble = value;
            data.type = Types.FLOAT;
            _values["?" + key] = data;
        }
        public void AddWithValue(string key, double value)
        {
            var data = new MyStructData();
            data.myDouble = value;
            data.type = Types.DOUBLE;
            _values["?" + key] = data;
        }
        public void AddWithValue(string key, decimal value)
        {
            var data = new MyStructData();
            data.myDecimal = value;
            data.type = Types.DECIMAL;
            _values["?" + key] = data;
        }
        public void AddWithValue(string key, byte[] value)
        {
            var data = new MyStructData();
            if (value != null)
            {
                data.myBuffer = value;
                data.type = Types.LONG_BLOB;
            }
            else
            {
                data.myBuffer = null;
                data.type = Types.NULL;
            }
            _values["?" + key] = data;
        }
        public void AddWithValue(string key, DateTime value)
        {
            var data = new MyStructData();
            data.myDateTime = value;
            data.type = Types.DATETIME;
            _values["?" + key] = data;
        }


        //-------------------------------------------------------
        //TODO: how about other datatype,
        //sbyte, uint,ulong, ?
        //-------------------------------------------------------
        public void AddWithValue(string key, sbyte value)
        {
            throw new NotImplementedException();
        }
        public void AddWithValue(string key, char value)
        {
            throw new NotImplementedException();
        }
        public void AddWithValue(string key, ushort value)
        {
            throw new NotImplementedException();
        }
        public void AddWithValue(string key, uint value)
        {
            throw new NotImplementedException();
        }
        public void AddWithValue(string key, ulong value)
        {
            throw new NotImplementedException();
        }


        internal bool TryGetData(string key, out MyStructData data)
        {
            return _values.TryGetValue(key, out data);
        }
 
        public void ClearDataValues()
        {
            _values.Clear();
        }


        //-------------------------------------------------------
        //sql parts : special extension 
        public void SetSqlPart(string sqlBoundKey, string sqlPart)
        {
            if (_sqlParts == null)
            {
                _sqlParts = new Dictionary<string, string>();
            }

            _sqlParts["??" + sqlBoundKey] = "`" + sqlPart + "`";
        }
        public bool TryGetSqlPart(string sqlBoundKey, out string sqlPart)
        {
            if (_sqlParts == null)
            {
                sqlPart = null;
                return false;
            }

            return _sqlParts.TryGetValue(sqlBoundKey, out sqlPart);
        }
        public void ClearSqlParts()
        {
            if (_sqlParts != null)
            {
                _sqlParts.Clear();
            }
        }
        //-------------------------------------------------------
        
    }
}