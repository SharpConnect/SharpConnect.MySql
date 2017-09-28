//MIT, 2015-2017, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using System.Text;
using SharpConnect.MySql.Information;
using SharpConnect.MySql.SqlLang;

namespace SharpConnect.MySql.CodeMapper
{
    /// <summary>
    /// sample cs code mapper generator
    /// </summary>
    public class CsCodeMapperGenerator
    {

        /// <summary>
        /// output code 
        /// </summary>
        public StringBuilder StBuilder { get; set; }

        /// <summary>
        /// generate cs code for this table
        /// </summary>
        /// <param name="tableinfo"></param>
        public void GenerateCsCodeForSqlTable(MySqlTableInfo tableinfo)
        {
            //from table info => we generate the C# code
            StringBuilder stbuilder = this.StBuilder;
            stbuilder.AppendLine("class " + tableinfo.Name + "{");
            List<MySqlColumnInfo> cols = tableinfo.Columns;
            int colCount = cols.Count;
            for (int i = 0; i < colCount; ++i)
            {
                MySqlColumnInfo col = cols[i];
                //map sql-type to cs-type
                stbuilder.AppendLine("public " + GetCsTypeFromSqlType(col) + " " + col.Name + ";");

            }
            stbuilder.Append("}");
        }
        string GetCsTypeFromSqlType(MySqlColumnInfo col)
        {
            //parse sql
            string fieldTypeName = col.FieldTypeName;
            if (fieldTypeName.StartsWith("varchar") ||
                fieldTypeName.StartsWith("char"))
            {
                return "string";
            }
            else if (fieldTypeName.StartsWith("int"))
            {
                return "int";
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }

}