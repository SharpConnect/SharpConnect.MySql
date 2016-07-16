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
        bool isPartialTable;
        //
        DataRowPacket currentRow;
        internal MySqlDataReader(Query query)
        {
            _query = query;
            //start 
            query.SetResultListener(subtable =>
            {
                //we need the subtable must arrive in correct order ***
                lock (subTables)
                {
                    subTables.Enqueue(subtable);
                    isPartialTable = subtable.IsPartialTable; //***
                }
            });
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
                    if (isPartialTable)
                    {
                        //we are in isPartial table mode (not complete)
                        //so must wait until the table arrive **
                        //------------------                    
                        //wait ***
                        //------------------
                        //TODO: review here *** tight loop
                        while (isPartialTable) ; //*** tigh loop
                        //------------------
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

    }
}