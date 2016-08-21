//MIT, 2015-2016, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql.Internal;

namespace SharpConnect.MySql
{
    public delegate void OnEachSubTable(MySqlSubTable subtable);

    public class MySqlDataReader
    {
        Query _query;
        Queue<MySqlTableResult> subTables = new Queue<MySqlTableResult>();
        MySqlTableResult currentTableResult = null;
        List<DataRowPacket> currentTableRows;
        int currentTableRowCount = 0;
        int currentRowIndex = 0;
        bool tableResultIsNotComplete;
        DataRowPacket currentRow;
        bool firstResultArrived;
        Action<MySqlDataReader> onFirstDataArrived;
        internal MySqlDataReader(Query query)
        {

            _query = query;
            //set result listener for query object  before actual query.Read()
            query.SetResultListener(subtable =>
            {
                //we need the subtable must arrive in correct order ***

                lock (subTables)
                {
                    subTables.Enqueue(subtable);
                    tableResultIsNotComplete = subtable.HasFollower; //***
                }
                if (!firstResultArrived)
                {
                    firstResultArrived = true;
                    if (onFirstDataArrived != null)
                    {
                        onFirstDataArrived(this);
                        onFirstDataArrived = null;
                    }
                }
            });
        }

        public MySqlSubTable CurrentSubTable
        {
            get { return new MySqlSubTable(currentTableResult); }
        }

        /// <summary>
        /// blocking, wait for first data arrive
        /// </summary>
        internal void WaitUntilFirstDataArrive()
        {
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
                        currentTableRowCount = currentTableResult.rows.Count;
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
                        //*** tigh loop
                        //wait on this
                        while (tableResultIsNotComplete) ;
                        goto TRY_AGAIN;
                    }
                }
            }
        }
        /// <summary>
        /// non blocking
        /// </summary>
        /// <param name="onFirstDataArrived"></param>
        internal void SetFirstDataArriveDelegate(Action<MySqlDataReader> onFirstDataArrived)
        {
            this.onFirstDataArrived = onFirstDataArrived;
        }

        public int FieldCount
        {
            get
            {
                //similar to Read() 
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
        /// <summary>
        /// async read ***
        /// </summary>
        /// <param name="onEachSubTable"></param>
        public void ReadSubTable(OnEachSubTable onEachSubTable)
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
                        CentralWaitingTasks.AddWaitingTask(new WaitingTask(() =>
                        {
                            if (tableResultIsNotComplete)
                            {
                                //not complete, continue waiting
                                return false;
                            }
                            else
                            {
                                //try again 
                                ReadSubTable(onEachSubTable);
                                return true;
                            }
                        }));
                    }
                    else if (!firstResultArrived)
                    {

                        CentralWaitingTasks.AddWaitingTask(new WaitingTask(() =>
                        {
                            if (!firstResultArrived)
                            {
                                //not complete, continue waiting
                                return false;
                            }
                            else
                            {
                                //try again
                                ReadSubTable(onEachSubTable);
                                return true;
                            }
                        }));
                    }
                    else
                    {
                        //finish
                        return;
                    }
                }
                else
                {
                    //has some subtable
                    //so start clear here
                    onEachSubTable(CurrentSubTable);
                    //after invoke
                    currentTableResult = null;
                    goto TRY_AGAIN;
                }
            }
        }
        /// <summary>
        /// sync read row
        /// </summary>
        /// <returns></returns>
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
                case MySqlDataType.BLOB:
                case MySqlDataType.LONG_BLOB:
                case MySqlDataType.MEDIUM_BLOB:
                case MySqlDataType.TINY_BLOB:
                    return data.myBuffer;

                case MySqlDataType.DATE:
                case MySqlDataType.NEWDATE:
                    return data.myDateTime;
                //stbuilder.Append('\'');
                //stbuilder.Append(data.myDateTime.ToString("yyyy-MM-dd"));
                //stbuilder.Append('\'');
                //break;
                case MySqlDataType.DATETIME:
                    //stbuilder.Append('\'');
                    //stbuilder.Append(data.myDateTime.ToString("yyyy-MM-dd hh:mm:ss"));
                    //stbuilder.Append('\'');
                    //break;
                    return data.myDateTime;
                case MySqlDataType.TIMESTAMP:
                case MySqlDataType.TIME:
                    ////TODO: review here
                    //stbuilder.Append('\'');
                    //stbuilder.Append(data.myDateTime.ToString("hh:mm:ss"));
                    //stbuilder.Append('\'');
                    //break;
                    return data.myDateTime;
                case MySqlDataType.STRING:
                case MySqlDataType.VARCHAR:
                case MySqlDataType.VAR_STRING:

                    //stbuilder.Append('\'');
                    ////TODO: check /escape string here ****
                    //stbuilder.Append(data.myString);
                    //stbuilder.Append('\'');
                    //break;
                    return data.myString;
                case MySqlDataType.BIT:
                    throw new NotSupportedException();
                // stbuilder.Append(Encoding.ASCII.GetString(new byte[] { (byte)data.myInt32 }));

                case MySqlDataType.DOUBLE:
                    return data.myDouble;
                //stbuilder.Append(data.myDouble.ToString());
                //break;
                case MySqlDataType.FLOAT:
                    return data.myDouble;//TODO: review here
                //stbuilder.Append(((float)data.myDouble).ToString());

                case MySqlDataType.TINY:
                case MySqlDataType.SHORT:
                case MySqlDataType.LONG:
                case MySqlDataType.INT24:
                case MySqlDataType.YEAR:
                    return data.myInt32;
                //stbuilder.Append(data.myInt32.ToString());

                case MySqlDataType.LONGLONG:
                    return data.myInt64;
                //stbuilder.Append(data.myInt64.ToString());

                case MySqlDataType.DECIMAL:
                    //stbuilder.Append(data.myDecimal.ToString());
                    return data.myDecimal;

                default:
                    throw new NotSupportedException();
            }
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


    public struct MySubTableDataReader
    {
        //for read on each subtable
        readonly MySqlTableResult tableResult;
        int currentRowIndex;
        DataRowPacket currentRow;
        internal MySubTableDataReader(MySqlTableResult tableResult)
        {
            this.tableResult = tableResult;
            currentRowIndex = 0;
            currentRow = null;
            SetCurrentRowIndex(0);
        }
        public int RowIndex { get { return this.currentRowIndex; } }
        public void SetCurrentRowIndex(int index)
        {
            this.currentRowIndex = index;
            if (index < tableResult.rows.Count)
            {
                currentRow = tableResult.rows[index];
            }
        }
        public int RowCount
        {
            get { return this.tableResult.rows.Count; }
        }
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
                case MySqlDataType.BLOB:
                case MySqlDataType.LONG_BLOB:
                case MySqlDataType.MEDIUM_BLOB:
                case MySqlDataType.TINY_BLOB:
                    return data.myBuffer;

                case MySqlDataType.DATE:
                case MySqlDataType.NEWDATE:
                    return data.myDateTime;
                //stbuilder.Append('\'');
                //stbuilder.Append(data.myDateTime.ToString("yyyy-MM-dd"));
                //stbuilder.Append('\'');
                //break;
                case MySqlDataType.DATETIME:
                    //stbuilder.Append('\'');
                    //stbuilder.Append(data.myDateTime.ToString("yyyy-MM-dd hh:mm:ss"));
                    //stbuilder.Append('\'');
                    //break;
                    return data.myDateTime;
                case MySqlDataType.TIMESTAMP:
                case MySqlDataType.TIME:
                    ////TODO: review here
                    //stbuilder.Append('\'');
                    //stbuilder.Append(data.myDateTime.ToString("hh:mm:ss"));
                    //stbuilder.Append('\'');
                    //break;
                    return data.myDateTime;
                case MySqlDataType.STRING:
                case MySqlDataType.VARCHAR:
                case MySqlDataType.VAR_STRING:

                    //stbuilder.Append('\'');
                    ////TODO: check /escape string here ****
                    //stbuilder.Append(data.myString);
                    //stbuilder.Append('\'');
                    //break;
                    return data.myString;
                case MySqlDataType.BIT:
                    throw new NotSupportedException();
                // stbuilder.Append(Encoding.ASCII.GetString(new byte[] { (byte)data.myInt32 }));

                case MySqlDataType.DOUBLE:
                    return data.myDouble;
                //stbuilder.Append(data.myDouble.ToString());
                //break;
                case MySqlDataType.FLOAT:
                    return data.myDouble;//TODO: review here
                                         //stbuilder.Append(((float)data.myDouble).ToString());

                case MySqlDataType.TINY:
                case MySqlDataType.SHORT:
                case MySqlDataType.LONG:
                case MySqlDataType.INT24:
                case MySqlDataType.YEAR:
                    return data.myInt32;
                //stbuilder.Append(data.myInt32.ToString());

                case MySqlDataType.LONGLONG:
                    return data.myInt64;
                //stbuilder.Append(data.myInt64.ToString());

                case MySqlDataType.DECIMAL:
                    //stbuilder.Append(data.myDecimal.ToString());
                    return data.myDecimal;

                default:
                    throw new NotSupportedException();
            }
        }

    }



    public struct MySqlSubTable
    {
        public static readonly MySqlSubTable Empty = new MySqlSubTable();
        readonly MySqlTableResult tableResult;
        internal MySqlSubTable(MySqlTableResult tableResult)
        {
            this.tableResult = tableResult;
        }
        public SubTableHeader Header
        {
            get
            {
                if (tableResult == null)
                {
                    //empty subtable header
                    return new SubTableHeader();
                }
                else
                {
                    return new SubTableHeader(this.tableResult.tableHeader);
                }

            }
        }
        public int RowCount
        {
            get { return tableResult.rows.Count; }
        }
        public bool HasRows
        {
            get
            {
                return tableResult.rows != null && tableResult.rows.Count > 0;
            }
        }

        public MySubTableDataReader CreateDataReader()
        {
            return new MySubTableDataReader(this.tableResult);
        }

        public int FieldCount
        {
            get
            {
                return tableResult.tableHeader.ColumnCount;
            }
        }
        public MySqlFieldDefinition GetFieldDefinition(int index)
        {
            return new MySqlFieldDefinition(tableResult.tableHeader.GetField(index));
        }
        public string GetFieldName(int index)
        {
            return tableResult.tableHeader.GetField(index).name;
        }
        public int GetFieldType(int index)
        {
            return tableResult.tableHeader.GetField(index).columnType;
        }
        public MySqlFieldDefinition GetFieldDefinition(string fieldname)
        {
            int index = tableResult.tableHeader.GetFieldIndex(fieldname);
            if (index > -1)
            {
                return GetFieldDefinition(index);
            }
            else
            {
                return new MySqlFieldDefinition();
            }
        }
        //----------------------------
        public bool IsLastTable
        {
            get { return !tableResult.HasFollower; }
        }

        //-------------------------------------------------------
        public static bool operator ==(MySqlSubTable sub1, MySqlSubTable sub2)
        {
            return sub1.tableResult == sub2.tableResult;
        }
        public static bool operator !=(MySqlSubTable sub1, MySqlSubTable sub2)
        {
            return sub1.tableResult != sub2.tableResult;
        }
        public override int GetHashCode()
        {
            return tableResult.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj is MySqlSubTable)
            {
                return ((MySqlSubTable)obj).tableResult == this.tableResult;
            }
            return false;
        }
        //-------------------------------------------------------


    }


    public struct SubTableHeader
    {
        TableHeader tableHeader;
        internal SubTableHeader(TableHeader tableHeader)
        {
            this.tableHeader = tableHeader;
        }
        public static bool operator ==(SubTableHeader sub1, SubTableHeader sub2)
        {
            return sub1.tableHeader == sub2.tableHeader;
        }
        public static bool operator !=(SubTableHeader sub1, SubTableHeader sub2)
        {
            return sub1.tableHeader != sub2.tableHeader;
        }
        public override int GetHashCode()
        {
            return tableHeader.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj is SubTableHeader)
            {
                return ((SubTableHeader)obj).tableHeader == this.tableHeader;
            }
            return false;
        }
    }

    public struct MySqlFieldDefinition
    {
        FieldPacket fieldPacket;

        public static readonly MySqlFieldDefinition Empty = new MySqlFieldDefinition();

        internal MySqlFieldDefinition(FieldPacket fieldPacket)
        {
            this.fieldPacket = fieldPacket;
        }
        public bool IsEmpty
        {
            get { return fieldPacket == null; }
        }
        public int FieldType
        {
            get { return this.fieldPacket.columnType; }
        }
        public string Name
        {
            get { return this.fieldPacket.name; }
        }
        public int FieldIndex
        {
            get
            {
                return fieldPacket.FieldIndex;
            }
        }
    }

}