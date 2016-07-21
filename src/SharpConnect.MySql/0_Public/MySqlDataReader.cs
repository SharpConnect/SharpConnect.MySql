//MIT, 2015-2016, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql.Internal;
namespace SharpConnect.MySql
{

    public class MySqlDataReader
    {
        Query _query;
        Queue<MySqlTableResult> subTables = new Queue<MySqlTableResult>();
        MySqlTableResult currentTableResult = null;
        List<DataRowPacket> currentTableRows;
        int currentTableRowCount = 0;
        int currentRowIndex = 0;
        bool tableResultIsNotComplete;
        //
        DataRowPacket currentRow;
        bool firstResultArrived;
        internal MySqlDataReader(Query query)
        {
            _query = query;
            //start 
            query.SetResultListener(subtable =>
            {
                //we need the subtable must arrive in correct order ***
                firstResultArrived = true;
                lock (subTables)
                {
                    subTables.Enqueue(subtable);
                    tableResultIsNotComplete = subtable.HasFollowerTable; //***
                }
            });
        }
        //-------------------------

        public int FieldCount
        {
            get
            {
                //similar to Read()
                TRY_AGAIN:
                if (currentTableResult == null)
                {
                    //no current table 
                    bool hasSomeSubTables = false;
                    lock (subTables)
                    {
                        if (subTables.Count > 0)
                        {
                            currentTableResult = subTables.Dequeue();
                            hasSomeSubTables = true;
                        }
                    }
                    if (!hasSomeSubTables)
                    {
                        if (tableResultIsNotComplete)
                        {
                            //we are in isPartial table mode (not complete)
                            //so must wait until the table arrive **
                            //------------------                    
                            //wait ***
                            //------------------
                            //TODO: review here *** tight loop
                            while (tableResultIsNotComplete) ; //*** tigh loop
                                                     //------------------
                            goto TRY_AGAIN;
                        }
                    }
                }
                return (currentTableResult == null) ?
                    0 :
                    currentTableResult.tableHeader.ColumnCount;
            }
        }
        /// <summary>
        /// get field name of specific column index
        /// </summary>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        public string GetName(int colIndex)
        {
            return currentTableResult.tableHeader.GetFields()[colIndex].name;
        }
        //-------------------------
        public bool HasRows
        {
            get
            {
                return currentTableResult != null
                    && currentTableResult.rows.Count > 0;
            }
        }
        public bool Read()
        {
            TRY_AGAIN:
            if (currentTableResult == null)
            {
                //no current table
                currentRowIndex = 0;
                currentTableRows = null;
                bool hasSomeSubTables = false;
                lock (subTables)
                {
                    if (subTables.Count > 0)
                    {
                        currentTableResult = subTables.Dequeue();
                        hasSomeSubTables = true;
                    }
                }
                if (!hasSomeSubTables)
                {
                    if (tableResultIsNotComplete)
                    {
                        //we are in isPartial table mode (not complete)
                        //so must wait until the table arrive **
                        //------------------                    
                        //wait ***
                        //------------------
                        //TODO: review here *** tight loop
                        while (tableResultIsNotComplete) ; //*** tight loop
                        //------------------
                        goto TRY_AGAIN;
                    }
                    else if (!firstResultArrived)
                    {
                        //another tight loop
                        //wait for first result arrive
                        //TODO: review here *** tight loop
                        while (!firstResultArrived) ;//*** tight loop
                        goto TRY_AGAIN;
                    }
                    else
                    {
                        //not in partial table mode
                        return false;
                    }
                }
                //
                currentTableRows = currentTableResult.rows;
                currentTableRowCount = currentTableRows.Count;
            }
            //
            if (currentRowIndex < currentTableRowCount)
            {
                //------
                //Console.WriteLine(currentRowIndex.ToString());
                //------
                currentRow = currentTableResult.rows[currentRowIndex];
                currentRowIndex++;
                return true;
            }
            else
            {
                currentTableResult = null;
                goto TRY_AGAIN;
            }
        }
        public void Close(Action nextAction = null)
        {
            if (nextAction == null)
            {
                //block
                _query.Close();

            }
            else
            {
                //unblock
                _query.Close(() =>
                {
                    currentTableResult = null;
                    currentRowIndex = 0;
                    currentRow = null;
                    subTables.Clear();
                    nextAction();
                });
            }
        }
        //-------------------------------------------
        public sbyte GetInt8(int colIndex)
        {

            //TODO: check match type and check index here
            return (sbyte)currentRow.Cells[colIndex].myInt32;
        }
        public byte GetUInt8(int colIndex)
        {
            //TODO: check match type and check index here
            return (byte)currentRow.Cells[colIndex].myInt32;
        }
        public short GetInt16(int colIndex)
        {   //TODO: check match type and check index here
            return (short)currentRow.Cells[colIndex].myInt32;
        }
        public ushort GetUInt16(int colIndex)
        {
            //TODO: check match type and check index here
            return (ushort)currentRow.Cells[colIndex].myInt32;
        }

        public int GetInt32(int colIndex)
        {
            //TODO: check match type and check index here
            return currentRow.Cells[colIndex].myInt32;
        }
        public uint GetUInt32(int colIndex)
        {
            //TODO: check match type and check index here
            return currentRow.Cells[colIndex].myUInt32;
        }
        public long GetLong(int colIndex)
        {
            //TODO: check match type and check index here
            return currentRow.Cells[colIndex].myInt64;
        }
        public ulong GetULong(int colIndex)
        {
            //TODO: check match type and check index here
            return currentRow.Cells[colIndex].myUInt64;
        }
        public decimal GetDecimal(int colIndex)
        {
            //TODO: check match type and index here
            return currentRow.Cells[colIndex].myDecimal;
        }
        public string GetString(int colIndex)
        {
            //TODO: check match type and index here
            return currentRow.Cells[colIndex].myString;
        }
        public string GetString(int colIndex, System.Text.Encoding encoding)
        {
            //TODO: check match type and index here
            return currentRow.Cells[colIndex].myString;
        }
        public byte[] GetBuffer(int colIndex)
        {
            //TODO: check match type and index here
            return currentRow.Cells[colIndex].myBuffer;
        }

        public DateTime GetDateTime(int colIndex)
        {
            //TODO: check match type and check index here
            return currentRow.Cells[colIndex].myDateTime;
        }
        public object GetValue(int colIndex)
        {
            MyStructData data = currentRow.Cells[colIndex];
            switch (data.type)
            {
                case Types.BLOB:
                case Types.LONG_BLOB:
                case Types.MEDIUM_BLOB:
                case Types.TINY_BLOB:
                    return data.myBuffer;

                case Types.DATE:
                case Types.NEWDATE:
                    return data.myDateTime;
                //stbuilder.Append('\'');
                //stbuilder.Append(data.myDateTime.ToString("yyyy-MM-dd"));
                //stbuilder.Append('\'');
                //break;
                case Types.DATETIME:
                    //stbuilder.Append('\'');
                    //stbuilder.Append(data.myDateTime.ToString("yyyy-MM-dd hh:mm:ss"));
                    //stbuilder.Append('\'');
                    //break;
                    return data.myDateTime;
                case Types.TIMESTAMP:
                case Types.TIME:
                    ////TODO: review here
                    //stbuilder.Append('\'');
                    //stbuilder.Append(data.myDateTime.ToString("hh:mm:ss"));
                    //stbuilder.Append('\'');
                    //break;
                    return data.myDateTime;
                case Types.STRING:
                case Types.VARCHAR:
                case Types.VAR_STRING:

                    //stbuilder.Append('\'');
                    ////TODO: check /escape string here ****
                    //stbuilder.Append(data.myString);
                    //stbuilder.Append('\'');
                    //break;
                    return data.myString;
                case Types.BIT:
                    throw new NotSupportedException();
                // stbuilder.Append(Encoding.ASCII.GetString(new byte[] { (byte)data.myInt32 }));

                case Types.DOUBLE:
                    return data.myDouble;
                //stbuilder.Append(data.myDouble.ToString());
                //break;
                case Types.FLOAT:
                    return data.myDouble;//TODO: review here
                //stbuilder.Append(((float)data.myDouble).ToString());

                case Types.TINY:
                case Types.SHORT:
                case Types.LONG:
                case Types.INT24:
                case Types.YEAR:
                    return data.myInt32;
                //stbuilder.Append(data.myInt32.ToString());

                case Types.LONGLONG:
                    return data.myInt64;
                //stbuilder.Append(data.myInt64.ToString());

                case Types.DECIMAL:
                    //stbuilder.Append(data.myDecimal.ToString());
                    return data.myDecimal;

                default:
                    throw new NotSupportedException();
            }
        }
    }


    static class MySqlTypeConversionInfo
    {
        //built in type conversion 
        static Dictionary<Type, ProperDataType> dataTypeMaps = new Dictionary<Type, ProperDataType>();
        static MySqlTypeConversionInfo()
        {
            //-----------------------------------------------------------
            dataTypeMaps.Add(typeof(bool), ProperDataType.Bool);
            dataTypeMaps.Add(typeof(byte), ProperDataType.Byte);
            dataTypeMaps.Add(typeof(sbyte), ProperDataType.Sbyte);
            dataTypeMaps.Add(typeof(char), ProperDataType.Char);
            dataTypeMaps.Add(typeof(Int16), ProperDataType.Int16);
            dataTypeMaps.Add(typeof(UInt16), ProperDataType.UInt16);
            dataTypeMaps.Add(typeof(int), ProperDataType.Int32);
            dataTypeMaps.Add(typeof(uint), ProperDataType.UInt32);
            dataTypeMaps.Add(typeof(long), ProperDataType.Int64);
            dataTypeMaps.Add(typeof(ulong), ProperDataType.UInt64);
            dataTypeMaps.Add(typeof(float), ProperDataType.Float32);
            dataTypeMaps.Add(typeof(double), ProperDataType.Double64);
            dataTypeMaps.Add(typeof(DateTime), ProperDataType.DateTime);
            dataTypeMaps.Add(typeof(string), ProperDataType.String);
            dataTypeMaps.Add(typeof(byte[]), ProperDataType.Buffer);
            //-----------------------------------------------------------

        }
        public static ProperDataType GetProperDataType(object o)
        {
            ProperDataType foundProperType;
            if (!dataTypeMaps.TryGetValue(o.GetType(), out foundProperType))
            {
                return ProperDataType.Unknown;
            }
            return foundProperType;
        }
    }
    enum ProperDataType
    {
        Unknown,
        Bool,
        Byte,
        Sbyte,
        /// <summary>
        /// 2 byte width charactor
        /// </summary>
        Char,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        //----------
        Float32,
        Double64,
        Decimal,
        DateTime,
        //----------
        String,
        Buffer,
        //----------
    }

}