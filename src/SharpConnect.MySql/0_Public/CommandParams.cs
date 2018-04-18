//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql.Internal;
namespace SharpConnect.MySql
{
    public class CommandParams
    {
        Dictionary<string, MyStructData> _values = new Dictionary<string, MyStructData>(); //user bound values
        Dictionary<string, string> _sqlParts;//null at first, special  extension
        internal CommandParams()
        {
        }
        internal IStringConverter StringConv
        {
            get;
            set;
        }
        public void AddWithValue(string key, string value)
        {
            var data = new MyStructData();
            if (value == "")
            {
                data.myString = "";
                data.type = MySqlDataType.VAR_STRING;
            }
            else if (value != null)
            {
                //replace some value 
                if (StringConv != null)
                {
                    AddWithValue(key, StringConv.WriteConv(value));
                    return;
                }
                else
                {
                    data.myString = value.Replace("\'", "\\\'");
                    data.type = MySqlDataType.VAR_STRING;
                }
            }
            else
            {
                data.myString = null;
                data.type = MySqlDataType.NULL;
            }
            _values[key] = data;
        }
        public void AddWithValue(string key, string value, IStringConverter strConv)
        {
            var data = new MyStructData();
            if (value == "")
            {
                data.myString = "";
                data.type = MySqlDataType.VAR_STRING;
            }
            else if (value != null)
            {

                //replace some value 
                if (strConv != null)
                {
                    AddWithValue(key, strConv.WriteConv(value));
                    return;
                }
                else
                {
                    data.myString = value.Replace("\'", "\\\'");
                    data.type = MySqlDataType.VAR_STRING;
                }
            }
            else
            {
                data.myString = null;
                data.type = MySqlDataType.NULL;
            }
            _values[key] = data;
        }
        public void AddWithValue(string key, string value, System.Text.Encoding enc)
        {
            var data = new MyStructData();
            if (value == "")
            {
                data.myString = "";
                data.type = MySqlDataType.VAR_STRING;
            }
            else if (value != null)
            {
                //replace some value 
                if (enc != null)
                {
                    AddWithValue(key, enc.GetBytes(value));
                    return;
                }
                else
                {
                    data.myString = value.Replace("\'", "\\\'");
                    data.type = MySqlDataType.VAR_STRING;
                }
            }
            else
            {
                data.myString = null;
                data.type = MySqlDataType.NULL;
            }
            _values[key] = data;
        }


        public void AddWithValue(string key, byte value)
        {
            //TODO: review here
            var data = new MyStructData();
            data.myString = value.ToString();
            data.type = MySqlDataType.STRING;
            //data.myInt32 = value;
            //data.type = Types.TINY;
            _values[key] = data;
        }
        public void AddWithValue(string key, short value)
        {
            var data = new MyStructData();
            data.myInt32 = value;
            data.type = MySqlDataType.SHORT;
            _values[key] = data;
        }
        public void AddWithValue(string key, int value)
        {
            //INT 4       min        max
            //signed -2147483648 2147483647
            //unsigned     0     4294967295
            //---------------------------

            var data = new MyStructData();
            data.myInt32 = value;
            data.type = MySqlDataType.LONG;//Types.LONG = int32
            _values[key] = data;
        }
        public void AddWithValue(string key, long value)
        {
            var data = new MyStructData();
            data.myInt64 = value;
            data.type = MySqlDataType.LONGLONG;
            _values[key] = data;
        }
        public void AddWithValue(string key, float value)
        {
            var data = new MyStructData();
            data.myDouble = value;
            data.type = MySqlDataType.FLOAT;
            _values[key] = data;
        }
        public void AddWithValue(string key, double value)
        {
            var data = new MyStructData();
            data.myDouble = value;
            data.type = MySqlDataType.DOUBLE;
            _values[key] = data;
        }
        public void AddWithValue(string key, decimal value)
        {
            var data = new MyStructData();
            data.myString = value.ToString();
            data.type = MySqlDataType.STRING;
            _values[key] = data;
        }
        public void AddWithValue(string key, byte[] value)
        {
            var data = new MyStructData();
            if (value != null)
            {
                data.myBuffer = value;
                data.type = MySqlDataType.LONG_BLOB;
            }
            else
            {
                data.myBuffer = null;
                data.type = MySqlDataType.NULL;
            }
            _values[key] = data;
        }
        public void AddWithValue(string key, DateTime value)
        {
            var data = new MyStructData();
            data.myDateTime = value;
            data.type = MySqlDataType.DATETIME;
            _values[key] = data;
        }
        public void AddWithValue(string key, sbyte value)
        {
            //tiny int signed (-128 to 127)
            var data = new MyStructData();
            data.myInt32 = value;
            data.type = MySqlDataType.TINY;
            _values[key] = data;
        }
        public void AddWithValue(string key, char value)
        {
            //1 unicode char => 2 bytes store
            var data = new MyStructData();
            data.myUInt32 = value;
            data.type = MySqlDataType.LONGLONG; //TODO:?
            _values[key] = data;
        }
        public void AddWithValue(string key, ushort value)
        {
            //INT 2       min        max
            //signed      -32768    32767
            //unsigned     0     65535
            //---------------------------

            var data = new MyStructData();
            data.myString = value.ToString();
            data.type = MySqlDataType.STRING;
            //data.myUInt32 = value;
            //data.type = Types.SHORT;
            _values[key] = data;
        }
        public void AddWithValue(string key, uint value)
        {
            //INT 4       min        max
            //signed -2147483648 2147483647
            //unsigned     0     4294967295
            //---------------------------
            var data = new MyStructData();
            data.myUInt32 = value;
            data.type = MySqlDataType.LONGLONG;//** 
            _values[key] = data;
        }
        public void AddWithValue(string key, ulong value)
        {
            var data = new MyStructData();
            data.myString = value.ToString();
            data.type = MySqlDataType.STRING;
            //data.myUInt64 = value;
            //data.type = Types.LONGLONG;
            _values[key] = data;
        }
        public void AddWithNull(string key)
        {
            var data = new MyStructData();
            data.myString = null;
            data.type = MySqlDataType.NULL;
            //data.myUInt64 = value;
            //data.type = Types.LONGLONG;
            _values[key] = data;
        }
        //-------------------------------------------------------
        //user's bound data values 
        public void AddWithValue(string key, object value)
        {

            //get type of value
            if (value == null)
            {
                AddWithNull(key);
                return;
            }

            //get type of value
            switch (MySqlTypeConversionInfo.GetProperDataType(value))
            {
                //switch proper type
                default:
                case ProperDataType.Unknown:
                    throw new Exception("unknown data type?");
                case ProperDataType.String:
                    AddWithValue(key, (string)value);
                    break;
                case ProperDataType.Buffer:
                    AddWithValue(key, (byte[])value);
                    break;
                case ProperDataType.Bool:
                    AddWithValue(key, (bool)value);
                    break;
                case ProperDataType.Sbyte:
                    AddWithValue(key, (sbyte)value);
                    break;
                case ProperDataType.Char:
                    AddWithValue(key, (char)value);
                    break;
                case ProperDataType.Int16:
                    AddWithValue(key, (short)value);
                    break;
                case ProperDataType.UInt16:
                    AddWithValue(key, (ushort)value);
                    break;
                case ProperDataType.Int32:
                    AddWithValue(key, (int)value);
                    break;
                case ProperDataType.UInt32:
                    AddWithValue(key, (uint)value);
                    break;
                case ProperDataType.Int64:
                    AddWithValue(key, (long)value);
                    break;
                case ProperDataType.UInt64:
                    AddWithValue(key, (ulong)value);
                    break;
                case ProperDataType.DateTime:
                    AddWithValue(key, (DateTime)value);
                    break;
                case ProperDataType.Float32:
                    AddWithValue(key, (float)value);
                    break;
                case ProperDataType.Double64:
                    AddWithValue(key, (double)value);
                    break;
                case ProperDataType.Decimal:
                    AddWithValue(key, (decimal)value);
                    break;
            }
        }

        internal bool TryGetData(string key, out MyStructData data)
        {
            return _values.TryGetValue(key, out data);
        }

        /// <summary>
        /// clear binding data value
        /// </summary>
        public void ClearDataValues()
        {
            _values.Clear();
        }
        /// <summary>
        /// clear binding data value
        /// </summary>
        public void Clear()
        {
            ClearDataValues();
        }

        //-------------------------------------------------------
        //sql parts : special extension 
        public void SetSqlPart(string sqlBoundKey, string sqlPart)
        {
            if (_sqlParts == null)
            {
                _sqlParts = new Dictionary<string, string>();
            }

            _sqlParts[sqlBoundKey] = "`" + sqlPart + "`";
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
        public string[] GetAttachedValueKeys()
        {
            var keys = new string[_values.Count];
            int i = 0;
            foreach (string k in _values.Keys)
            {
                keys[i] = k;
                i++;
            }
            return keys;
        }
    }
}