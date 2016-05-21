//MIT 2015, brezza92, EngineKit and contributors
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpConnect.LiquidData
{
    public static class ColumnBasedTableHelper
    {

        public static ColumnBasedTable CreateColumnBaseTableFromCsv(string file, Encoding enc)
        {
            var table = new ColumnBasedTable();
            using (var fs = new FileStream(file, FileMode.Open))
            {
                var reader = new StreamReader(fs, enc);
                int line_id = 0;
                string firstline = reader.ReadLine();
                string[] col_names = ParseCsvLine(firstline);
                int col_count = col_names.Length;

                DataColumn[] columns = new DataColumn[col_count];
                for (int i = 0; i < col_count; ++i)
                {
                    columns[i] = table.CreateDataColumn(col_names[i]);
                }

                line_id++;
                string line = reader.ReadLine();
                while (line != null)
                {
                    string[] cells = ParseCsvLine(line);
                    if (cells.Length != col_count)
                    {
                        throw new NotSupportedException("column count not match!");
                    } 
                    for (int i = 0; i < col_count; ++i)
                    {
                        columns[i].AddData(cells[i]);
                    }

                    line_id++;
                    line = reader.ReadLine();
                }
                reader.Close();
                fs.Close();
            }
            return table;
        }

        static string[] ParseCsvLine(string csvline)
        {
            char[] buffer = csvline.ToCharArray();
            List<string> output = new List<string>();
            int j = buffer.Length;
            int state = 0;
            //TODO: optimize currentBuffer
            List<char> currentBuffer = new List<char>();

            for (int i = 0; i < j; ++i)
            {
                char c = buffer[i];
                switch (state)
                {
                    case 0: //init
                        {
                            if (c == '"')
                            {
                                state = 1;
                            }
                            else if (c == ',')
                            {
                                output.Add(new string(currentBuffer.ToArray()));
                                currentBuffer.Clear();
                            }
                            else
                            {
                                state = 2;
                                currentBuffer.Add(c);
                            }

                        }
                        break;
                    case 1:  //string escape
                        {
                            if (c == '"')
                            {
                                state = 2;
                            }
                            else
                            {
                                currentBuffer.Add(c);
                            }
                        }
                        break;
                    case 2:
                        {
                            if (c == ',')
                            {
                                output.Add(new string(currentBuffer.ToArray()));
                                currentBuffer.Clear();
                            }
                            else
                            {
                                if (c == '"')
                                {
                                    state = 1;
                                }
                                else
                                {
                                    currentBuffer.Add(c);
                                }
                            }
                        }
                        break;
                }
            }
            if (currentBuffer.Count > 0)
            {
                output.Add(new string(currentBuffer.ToArray()));
            }
            return output.ToArray();

        }
    }
}