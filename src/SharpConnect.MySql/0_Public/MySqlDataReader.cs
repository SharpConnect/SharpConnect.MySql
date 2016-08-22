//MIT, 2015-2016, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql.Internal;

namespace SharpConnect.MySql
{
    namespace SyncPatt
    {
        public static partial class MySqlSyncPattExtension
        {
            public static void Close(this MySqlDataReader reader)
            {
                reader.InternalClose();
            }
        }
    }
    namespace AsyncPatt
    {
        public static partial class MySqlAsyncPattExtension
        {
            public static void Close(this MySqlDataReader reader, Action nextAction)
            {
                reader.InternalClose(nextAction);
            }
        }
    }

    public abstract class MySqlDataReader
    {
        DataRowPacket currentRow;

        public abstract bool Read();
        internal virtual void InternalClose(Action nextAction = null) { }

        public abstract void SetCurrentRowIndex(int index);
        public abstract MySqlSubTable CurrentSubTable
        {
            get;
        }

        public int FieldCount
        {
            get
            {
                return CurrentSubTable.FieldCount;
            }
        }
        /// <summary>
        /// get field name of specific column index
        /// </summary>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        public string GetName(int colIndex)
        {
            return CurrentSubTable.GetFieldDefinition(colIndex).Name;
        }
        //-------------------------
        public bool HasRows
        {
            get
            {
                return !CurrentSubTable.IsEmpty && CurrentSubTable.RowCount > 0;
            }
        }
        internal void SetCurrentRow(DataRowPacket currentRow)
        {
            this.currentRow = currentRow;
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

    class MySqlQueryDataReader : MySqlDataReader
    {
        Query _query;
        Queue<MySqlTableResult> subTables = new Queue<MySqlTableResult>();
        MySqlSubTable currentSubTable;
        //-------------------------

        int currentTableRowCount = 0;
        int currentRowIndex = 0;
        //-------------------------
        bool firstResultArrived;
        bool tableResultIsNotComplete;
        Action<MySqlQueryDataReader> onFirstDataArrived;
        internal MySqlQueryDataReader(Query query)
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

        public override MySqlSubTable CurrentSubTable
        {
            get { return currentSubTable; }
        }
        public override void SetCurrentRowIndex(int index)
        {
            //set support in this mode
            throw new NotSupportedException();
        }
        /// <summary>
        /// blocking, wait for first data arrive
        /// </summary>
        internal void WaitUntilFirstDataArrive()
        {
            TRY_AGAIN:
            if (currentSubTable.IsEmpty)
            {
                //no current table 
                bool hasSomeSubTables = false;
                lock (subTables)
                {
                    if (subTables.Count > 0)
                    {
                        currentSubTable = new MySqlSubTable(subTables.Dequeue());

                        currentTableRowCount = currentSubTable.RowCount;
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
        internal void SetFirstDataArriveDelegate(Action<MySqlQueryDataReader> onFirstDataArrived)
        {
            this.onFirstDataArrived = onFirstDataArrived;
        }



        /// <summary>
        /// async, read each sub table
        /// </summary>
        /// <param name="onEachSubTable"></param>
        internal void ReadSubTable(Action<MySqlSubTable> onEachSubTable)
        {
            TRY_AGAIN:
            if (CurrentSubTable.IsEmpty)
            {
                //no current table  
                bool hasSomeSubTables = false;
                lock (subTables)
                {
                    if (subTables.Count > 0)
                    {
                        //1. get subtable
                        currentSubTable = new MySqlSubTable(subTables.Dequeue());
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
                    currentSubTable = MySqlSubTable.Empty;
                    goto TRY_AGAIN;
                }
            }
        }


        /// <summary>
        /// async read row
        /// </summary>
        /// <param name="onEachRow"></param>
        public void Read(Action onEachRow)
        {
            ReadSubTable(st =>
            {
                //on each subtable
                var tableReader = st.CreateDataReader();
                int j = st.RowCount;
                for (int i = 0; i < j; ++i)
                {
                    tableReader.SetCurrentRowIndex(i);
                    onEachRow();
                }
                //if last one
                if (st.IsLastTable)
                {
                    //async close
                    this.InternalClose(() => { });
                }
            });
        }
        /// <summary>
        /// sync read row
        /// </summary>
        /// <returns></returns>
        public override bool Read()
        {
            TRY_AGAIN:
            if (currentSubTable.IsEmpty)
            {
                //no current table 
                bool hasSomeSubTables = false;
                lock (subTables)
                {
                    if (subTables.Count > 0)
                    {
                        currentSubTable = new MySqlSubTable(subTables.Dequeue());
                        hasSomeSubTables = true;
                    }
                }

                if (hasSomeSubTables)
                {

                    currentRowIndex = 0;
                    currentTableRowCount = currentSubTable.RowCount;
                }
                else
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
            }
            //------------------------------------------------------------------
            if (currentRowIndex < currentTableRowCount)
            {
                SetCurrentRow(currentSubTable.GetRow(currentRowIndex));
                currentRowIndex++;
                return true;
            }
            else
            {
                currentSubTable = MySqlSubTable.Empty;
                goto TRY_AGAIN;
            }

        }
        internal override void InternalClose(Action nextAction = null)
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
                    currentRowIndex = 0;
                    currentTableRowCount = 0;
                    currentSubTable = MySqlSubTable.Empty;
                    subTables.Clear();
                    nextAction();
                });
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


    class MySubTableDataReader : MySqlDataReader
    {
        //for read on each subtable
        MySqlSubTable subT;
        List<DataRowPacket> rows;
        int currentRowIndex;
        int currentRowCount;
        internal MySubTableDataReader(MySqlSubTable tableResult)
        {
            this.subT = tableResult;
            this.rows = tableResult.GetMySqlTableResult().rows;
            currentRowCount = rows.Count;
            currentRowIndex = 0;
            SetCurrentRow(null);
            SetCurrentRowIndex(0);
        }
        public override MySqlSubTable CurrentSubTable
        {
            get
            {
                return subT;
            }
        }
        public override bool Read()
        {
            if (currentRowIndex < rows.Count)
            {
                SetCurrentRow(rows[currentRowIndex]);
                currentRowIndex++;
                return true;
            }
            else
            {
                return false;
            }
        }
        public override void SetCurrentRowIndex(int index)
        {
            this.currentRowIndex = index;
            if (index < rows.Count)
            {
                SetCurrentRow(rows[index]);
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
            get
            {
                return tableResult.rows.Count;
            }
        }
        public bool HasRows
        {
            get
            {
                return tableResult.rows != null && tableResult.rows.Count > 0;
            }
        }
        internal MySqlTableResult GetMySqlTableResult()
        {
            return this.tableResult;
        }
        internal DataRowPacket GetRow(int index)
        {
            return tableResult.rows[index];
        }
        public bool IsEmpty
        {
            get { return tableResult == null; }
        }

        public MySqlDataReader CreateDataReader()
        {
            return new MySubTableDataReader(this);
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