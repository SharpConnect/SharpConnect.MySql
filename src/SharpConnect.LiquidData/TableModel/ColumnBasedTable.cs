//MIT 2015, brezza27, EngineKit and contributors
using System;
using System.Collections.Generic;

namespace SharpConnect.LiquidData
{
    /// <summary>
    /// column-based table 
    /// </summary>
    public class ColumnBasedTable
    {
        //note: this is column-based table

        List<DataColumn> _dataColumns = new List<DataColumn>();
        Dictionary<string, int> _colNames = new Dictionary<string, int>();
        ColumnNameState _columnNameState = ColumnNameState.Dirty;
        enum ColumnNameState
        {
            Dirty,
            OK
        }


        public int RowCount
        {
            get
            {
                return _dataColumns[0].RowCount;
            }
        }
        public int ColumnCount
        {
            get
            {
                return _dataColumns.Count;
            }
        }
        public void RemoveColumn(int columnIndex)
        {
            //when user remove column or change column name
            //we must update index 
            _dataColumns.RemoveAt(columnIndex);
            _columnNameState = ColumnNameState.Dirty;
        }
        public string GetColumnName(int colIndex)
        {
            return _dataColumns[colIndex].ColumnName;
        }
        public object GetCellData(int row, int column)
        {
            return _dataColumns[column].GetCellData(row);
        }
        public DataColumn GetColumn(int index)
        {
            return _dataColumns[index];
        }
        public IEnumerable<DataColumn> GetColumnIterForward()
        {
            foreach (DataColumn col in _dataColumns)
            {
                yield return col;
            }
        }

        public DataColumn CreateDataColumn(string colName)
        {
            if (!_colNames.ContainsKey(colName))
            {
                var dataColumn = new DataColumn(this, colName);
                _dataColumns.Add(dataColumn);
                _columnNameState = ColumnNameState.Dirty;
                return dataColumn;
            }
            else
            {
                throw new Exception("duplicate coloumn name " + colName);
            }

        }

        public int GetColumnIndex(string colname)
        {
            if (_columnNameState == ColumnNameState.Dirty)
            {
                //recreate table column names
                ValidateColumnNames();
            }

            int found;
            if (!_colNames.TryGetValue(colname, out found))
            {
                found = -1; //not found
            }
            return found;

        }
        void ValidateColumnNames()
        {

            _colNames.Clear();
            int j = _dataColumns.Count;
            for (int i = 0; i < j; ++i)
            {
                DataColumn col = _dataColumns[i];
                _colNames[col.ColumnName] = i;
            }

            _columnNameState = ColumnNameState.OK;

        }
        internal void InvalidateColumnNameState()
        {
            _columnNameState = ColumnNameState.Dirty;
        }
    }

    public class DataColumn
    {
        ColumnBasedTable _ownerTable;
        List<object> _cells = new List<object>();
        string _name;
        internal DataColumn(ColumnBasedTable ownerTable, string name)
        {
            ColumnName = name;
            _ownerTable = ownerTable;
        }
        public int RowCount
        {
            get
            {
                return _cells.Count;
            }
        }
        /// <summary>
        /// TODO: review here when da
        /// </summary>
        internal string ColumnName
        {
            get { return _name; }
            set
            {
                _name = value;

            }
        }
        public ColumnTypeHint TypeHint
        {
            get;
            set;
        }
        public void AddData(object data)
        {
            _cells.Add(data);
        }
        public object GetCellData(int rowIndex)
        {
            return _cells[rowIndex];
        }
    }
    public enum ColumnTypeHint
    {
        Unknown,
        String,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Double,
        Boolean
    }


}