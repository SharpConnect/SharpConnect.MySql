//MIT, 2015-2016, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql.Internal;
using System.Text;

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

        MySqlSubTable currentSubTable;
        List<DataRowPacket> rows;
        int currentRowIndex;
        int subTableRowCount;
        bool isEmptyTable = true; //default
        MyStructData[] cells;
        BufferReader bufferReader = new BufferReader();
        StringBuilder tmpStringBuilder = new StringBuilder();


        internal virtual void InternalClose(Action nextAction = null) { }
        internal bool IsEmptyTable
        {
            get { return isEmptyTable; }
        }
        public void SetCurrentSubTable(MySqlSubTable currentSubTable)
        {
            this.currentSubTable = currentSubTable;
            if (!currentSubTable.IsEmpty)
            {
                this.rows = currentSubTable.GetMySqlTableResult().rows;
                isEmptyTable = false;
                subTableRowCount = rows.Count;
                //expand buffer for each row 
                cells = new MyStructData[currentSubTable.FieldCount];
            }
            else
            {
                rows = null;
                isEmptyTable = true;
                subTableRowCount = 0;

            }
            currentRowIndex = 0;
        }
        public virtual bool Read()
        {
            if (currentRowIndex < subTableRowCount)
            {
                SetCurrentRow(currentRowIndex);
                currentRowIndex++;
                return true;
            }
            else
            {
                return false;
            }
        }
        public virtual void SetCurrentRowIndex(int index)
        {
            this.currentRowIndex = index;
            if (index < rows.Count)
            {
                SetCurrentRow(index);
            }
        }
        public MySqlSubTable CurrentSubTable
        {
            get { return currentSubTable; }
        }

        public bool IsLastTable
        {
            get { return CurrentSubTable.IsLastTable; }
        }
        public int FieldCount
        {
            get
            {
                return CurrentSubTable.FieldCount;
            }
        }
        public System.Text.Encoding StringEncoding
        {
            get;
            set;
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
                return !isEmptyTable && subTableRowCount > 0;
            }
        }
        internal void SetCurrentRow(int currentIndex)
        {
            DataRowPacket currentRow = rows[currentIndex];
            //expand this row to buffer ***
            bufferReader.SetSource(currentRow._rowDataBuffer);

            if (currentRow.IsBinaryProtocol)
            {
                //read each cell , binary protocol ***
                //1. skip start packet byte [00]
                //2. skip null-bitmap, length:(column-count+7+2)/8
                //r.ReadFiller(1);//skip start packet byte [00]
                //r.ReadFiller((columnCount + 7 + 2) / 8);//skip null-bitmap, length:(column-count+7+2)/8
                int columnCount = currentSubTable.FieldCount;
                bufferReader.Position = 1 + ((columnCount + 7 + 2) / 8); //skip 1+ bitmap len
                for (int i = 0; i < columnCount; ++i)
                {
                    cells[i] = ReadCurrentRowBinaryProtocol(currentSubTable.GetFieldDefinition(i));
                }

            }
            else
            {
                //read each cell , read as text protocol
                int columnCount = currentSubTable.FieldCount;
                for (int i = 0; i < columnCount; ++i)
                {
                    cells[i] = ReadCurrentRowTextProtocol(currentSubTable.GetFieldDefinition(i));
                }
            }
        }



        MyStructData ReadCurrentRowBinaryProtocol(MySqlFieldDefinition f)
        {
            MySqlDataType fieldType = (MySqlDataType)f.FieldType;
            MyStructData myData = new MyStructData();
            BufferReader r = this.bufferReader;
            switch (fieldType)
            {
                case MySqlDataType.TIMESTAMP://
                case MySqlDataType.DATE://
                case MySqlDataType.DATETIME://
                case MySqlDataType.NEWDATE://
                    r.ReadLengthCodedDateTime(out myData.myDateTime);
                    myData.type = fieldType;
                    return myData;
                case MySqlDataType.TINY://length = 1;
                    myData.myInt32 = r.U1();
                    myData.type = fieldType;
                    return myData;
                case MySqlDataType.SHORT://length = 2;
                case MySqlDataType.YEAR://length = 2;
                    myData.myInt32 = (int)r.U2();
                    myData.type = fieldType;
                    return myData;
                case MySqlDataType.INT24:
                case MySqlDataType.LONG://length = 4;
                    myData.myInt32 = (int)r.U4();
                    myData.type = fieldType;
                    return myData;
                case MySqlDataType.FLOAT:
                    myData.myDouble = r.ReadFloat();
                    myData.type = fieldType;
                    return myData;
                case MySqlDataType.DOUBLE:
                    myData.myDouble = r.ReadDouble();
                    myData.type = fieldType;
                    return myData;
                case MySqlDataType.NEWDECIMAL:
                    myData.myDecimal = r.ReadDecimal();
                    myData.type = fieldType;
                    return myData;
                case MySqlDataType.LONGLONG:
                    myData.myInt64 = r.ReadInt64();
                    myData.type = fieldType;
                    return myData;
                case MySqlDataType.STRING:
                case MySqlDataType.VARCHAR:
                case MySqlDataType.VAR_STRING:
                    myData.myString = r.ReadLengthCodedString();
                    myData.type = fieldType;
                    return myData;
                case MySqlDataType.TINY_BLOB:
                case MySqlDataType.MEDIUM_BLOB:
                case MySqlDataType.LONG_BLOB:
                case MySqlDataType.BLOB:
                case MySqlDataType.BIT:
                    myData.myBuffer = r.ReadLengthCodedBuffer();
                    myData.type = fieldType;
                    return myData;
                case MySqlDataType.GEOMETRY:
                    throw new NotSupportedException();
                default:
                    myData.myBuffer = r.ReadLengthCodedBuffer();
                    myData.type = MySqlDataType.NULL;
                    return myData;
            }
        }
        MyStructData ReadCurrentRowTextProtocol(MySqlFieldDefinition f)
        {

            BufferReader r = this.bufferReader;
            MyStructData data = new MyStructData();
            MySqlDataType type = (MySqlDataType)f.FieldType;
            string numberString = null;
            switch (type)
            {

                case MySqlDataType.TIMESTAMP:
                case MySqlDataType.DATE:
                case MySqlDataType.DATETIME:
                case MySqlDataType.NEWDATE:
                    {

                        QueryParsingConfig qparsingConfig = currentSubTable.ParsingConfig;
                        tmpStringBuilder.Length = 0;//clear 
                        data.myString = r.ReadLengthCodedString();
                        data.type = type;
                        if (data.myString == null)
                        {
                            data.type = MySqlDataType.NULL;
                            return data;
                        }
                        if (qparsingConfig.DateStrings)
                        {
                            return data;
                        }

                        //-------------------------------------------------------------
                        //    var originalString = dateString;
                        //    if (field.type === Types.DATE) {
                        //      dateString += ' 00:00:00';
                        //    }
                        tmpStringBuilder.Append(data.myString);
                        //string originalString = dateString;
                        if (type == MySqlDataType.DATE)
                        {
                            tmpStringBuilder.Append(" 00:00:00");
                        }
                        //    if (timeZone !== 'local') {
                        //      dateString += ' ' + timeZone;
                        //    }

                        if (!qparsingConfig.UseLocalTimeZone)
                        {
                            tmpStringBuilder.Append(' ' + qparsingConfig.TimeZone);
                        }
                        //var dt;
                        //    dt = new Date(dateString);
                        //    if (isNaN(dt.getTime())) {
                        //      return originalString;
                        //    }

                        data.myDateTime = DateTime.Parse(tmpStringBuilder.ToString(),
                            System.Globalization.CultureInfo.InvariantCulture);
                        data.type = type;
                        tmpStringBuilder.Length = 0;//clear 
                    }
                    return data;
                case MySqlDataType.TINY:
                case MySqlDataType.SHORT:
                case MySqlDataType.LONG:
                case MySqlDataType.INT24:
                case MySqlDataType.YEAR:

                    //TODO: review here,                    
                    data.myString = numberString = r.ReadLengthCodedString();
                    if (numberString == null ||
                        (f.IsZeroFill && numberString[0] == '0') ||
                        numberString.Length == 0)
                    {
                        data.type = MySqlDataType.NULL;
                    }
                    else
                    {
                        data.myInt32 = Convert.ToInt32(numberString);
                        data.type = type;
                    }
                    return data;
                case MySqlDataType.FLOAT:
                case MySqlDataType.DOUBLE:
                    data.myString = numberString = r.ReadLengthCodedString();
                    if (numberString == null || (f.IsZeroFill && numberString[0] == '0'))
                    {
                        data.type = MySqlDataType.NULL;
                    }
                    else
                    {
                        data.myDouble = Convert.ToDouble(numberString);
                        data.type = type;
                    }
                    return data;
                //    return (numberString === null || (field.zeroFill && numberString[0] == "0"))
                //      ? numberString : Number(numberString);
                case MySqlDataType.NEWDECIMAL:
                case MySqlDataType.LONGLONG:
                    //    numberString = parser.parseLengthCodedString();
                    //    return (numberString === null || (field.zeroFill && numberString[0] == "0"))
                    //      ? numberString
                    //      : ((supportBigNumbers && (bigNumberStrings || (Number(numberString) > IEEE_754_BINARY_64_PRECISION)))
                    //        ? numberString
                    //        : Number(numberString));

                    QueryParsingConfig config = currentSubTable.ParsingConfig;
                    data.myString = numberString = r.ReadLengthCodedString();
                    if (numberString == null || (f.IsZeroFill && numberString[0] == '0'))
                    {
                        data.type = MySqlDataType.NULL;
                    }
                    else if (config.SupportBigNumbers && (config.BigNumberStrings || (Convert.ToInt64(numberString) > Packet.IEEE_754_BINARY_64_PRECISION)))
                    {
                        //store as string ?
                        //TODO: review here  again
                        data.myString = numberString;
                        data.type = type;
                        throw new NotSupportedException();
                    }
                    else if (type == MySqlDataType.LONGLONG)
                    {
                        data.myInt64 = Convert.ToInt64(numberString);
                        data.type = type;
                    }
                    else//decimal
                    {
                        data.myDecimal = Convert.ToDecimal(numberString);
                        data.type = type;
                    }
                    return data;
                case MySqlDataType.BIT:

                    data.myBuffer = r.ReadLengthCodedBuffer();
                    data.type = type;
                    return data;
                //    return parser.parseLengthCodedBuffer();
                case MySqlDataType.STRING:
                case MySqlDataType.VAR_STRING:

                case MySqlDataType.TINY_BLOB:
                case MySqlDataType.MEDIUM_BLOB:
                case MySqlDataType.LONG_BLOB:
                case MySqlDataType.BLOB:
                    if (f.MarkedAsBinary)
                    {
                        data.myBuffer = r.ReadLengthCodedBuffer();
                        data.type = (data.myBuffer != null) ? type : MySqlDataType.NULL;
                    }
                    else
                    {
                        data.myString = r.ReadLengthCodedString();
                        data.type = (data.myString != null) ? type : MySqlDataType.NULL;
                    }
                    return data;
                //    return (field.charsetNr === Charsets.BINARY)
                //      ? parser.parseLengthCodedBuffer()
                //      : parser.parseLengthCodedString();
                case MySqlDataType.GEOMETRY:
                    //TODO: unfinished
                    data.type = MySqlDataType.GEOMETRY;
                    return data;
                default:
                    data.myString = r.ReadLengthCodedString();
                    data.type = type;
                    return data;
            }
        }
        public sbyte GetInt8(int colIndex)
        {
            //TODO: check match type and check index here

            return (sbyte)cells[colIndex].myInt32;
        }
        public byte GetUInt8(int colIndex)
        {
            //TODO: check match type and check index here
            return (byte)cells[colIndex].myInt32;
        }
        public short GetInt16(int colIndex)
        {   //TODO: check match type and check index here
            return (short)cells[colIndex].myInt32;
        }
        public ushort GetUInt16(int colIndex)
        {
            //TODO: check match type and check index here
            return (ushort)cells[colIndex].myInt32;
        }

        public int GetInt32(int colIndex)
        {
            //TODO: check match type and check index here
            return cells[colIndex].myInt32;
        }
        public uint GetUInt32(int colIndex)
        {
            //TODO: check match type and check index here
            return cells[colIndex].myUInt32;
        }
        public long GetLong(int colIndex)
        {
            //TODO: check match type and check index here
            return cells[colIndex].myInt64;
        }
        public ulong GetULong(int colIndex)
        {
            //TODO: check match type and check index here
            return cells[colIndex].myUInt64;
        }
        public decimal GetDecimal(int colIndex)
        {
            //TODO: check match type and index here
            return cells[colIndex].myDecimal;
        }
        public string GetString(int colIndex)
        {
            //TODO: check match type and index here
            return cells[colIndex].myString;
        }
        public string GetString(int colIndex, System.Text.Encoding encoding)
        {
            //TODO: check match type and index here
            return cells[colIndex].myString;
        }
        public byte[] GetBuffer(int colIndex)
        {
            //TODO: check match type and index here
            return cells[colIndex].myBuffer;
        }
        public bool IsDBNull(int colIndex)
        {
            return cells[colIndex].type == MySqlDataType.NULL;
        }
        public DateTime GetDateTime(int colIndex)
        {
            //TODO: check match type and check index here
            return cells[colIndex].myDateTime;
        }
        public object GetValue(int colIndex)
        {
            MyStructData data = cells[colIndex];
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

        bool emptySubTable = true;
        //-------------------------

        //int currentTableRowCount = 0;
        //int currentRowIndex = 0;
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
            if (emptySubTable)
            {
                //no current table 
                bool hasSomeSubTables = false;
                lock (subTables)
                {
                    if (subTables.Count > 0)
                    {
                        MySqlSubTable subt = new MySqlSubTable(subTables.Dequeue());
                        SetCurrentSubTable(subt);
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
                        SetCurrentSubTable(new MySqlSubTable(subTables.Dequeue()));
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
                    SetCurrentSubTable(MySqlSubTable.Empty);
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
                tableReader.StringEncoding = this.StringEncoding;

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
            if (IsEmptyTable)
            {
                //no current table 
                bool hasSomeSubTables = false;
                lock (subTables)
                {
                    if (subTables.Count > 0)
                    {
                        MySqlSubTable subT = new MySqlSubTable(subTables.Dequeue());
                        SetCurrentSubTable(subT);
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
            }
            //------------------------------------------------------------------
            if (base.Read())
            {
                return true;
            }
            else
            {
                SetCurrentSubTable(MySqlSubTable.Empty);
                goto TRY_AGAIN;
            }
            //if (!base.Read())
            //{
            //    SetCurrentSubTable(MySqlSubTable.Empty);
            //    goto TRY_AGAIN;
            //}
            //else
            //{
            //    return true;
            //}
            //if (currentRowIndex < currentTableRowCount)
            //{
            //    SetCurrentRow(currentSubTable.GetRow(currentRowIndex));
            //    currentRowIndex++;
            //    return true;
            //}
            //else
            //{

            //    SetCurrentSubTable(MySqlSubTable.Empty);
            //    goto TRY_AGAIN;
            //}

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
                    //after close  (just close relation with the connection)
                    //we can continue read cache data in this reader ***
                    //so we don't clear the cache data . 
                    //currentRowIndex = 0;
                    //currentTableRowCount = 0;
                    //currentSubTable = MySqlSubTable.Empty;
                    //subTables.Clear(); 
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
        internal MySubTableDataReader(MySqlSubTable tableResult)
        {
            SetCurrentSubTable(tableResult);
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
        internal QueryParsingConfig ParsingConfig
        {
            get
            {
                return this.tableResult.tableHeader.ParsingConfig;
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
        internal bool MarkedAsBinary
        {
            get
            {
                return fieldPacket.charsetNr == (int)CharSets.BINARY;
            }
        }
        internal bool IsZeroFill
        {
            get
            {
                return fieldPacket.zeroFill;
            }
        }

    }

}