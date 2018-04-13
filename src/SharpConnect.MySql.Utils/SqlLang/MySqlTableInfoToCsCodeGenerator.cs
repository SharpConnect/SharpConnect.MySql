//MIT, 2018, Phycolos, EngineKit and Contributos
using System;
using System.Collections.Generic;
using System.Text;
using SharpConnect.MySql.Information;

namespace SharpConnect.MySql.SqlLang
{



    public class DBTableAttribute : Attribute
    {
        public string DatabaseName { get; set; }
        public string TableName { get; set; }
        public string Auto_increment { get; set; }
        public string Charset { get; set; }
        public string Engine { get; set; }
        public bool Default { get; set; }
        public string Using { get; set; }
    }

    public class DBFieldAttribute : Attribute
    {
        public string CharacterSet { get; set; }
        public string FieldDefault { get; set; }
        public string FieldName { get; set; }
        public bool HasAuto { get; set; }
        public bool HasUnsign { get; set; }
        public string Type { get; set; }
        public string Not { get; set; }
        public int Length { get; set; }
        public string Other { get; set; }
    }

    public class IndexOfTableAttribute : Attribute
    {
        public IndexOfTableAttribute(Type owner)
        {

        }
    }

    public class MySqlInfoToCsCodeGenerator
    {
        TablePart _table;
        bool _generateAttr;

        public void GenerateCsCodeAndSave(TablePart table, string saveToFilename)
        {
            _table = table;
            //
            StringBuilder output = new StringBuilder();
            GenerateCsCode(table, output);
            System.IO.File.WriteAllText(saveToFilename, output.ToString());

        }
        
        public void GenerateCsCode(TablePart table, StringBuilder strb)
        {
            _table = table;
            //
            strb.AppendLine("//" + DateTime.Now.ToString("s"));
            strb.AppendLine("using System;");
            strb.AppendLine("");
            strb.AppendLine("namespace " + table.DatabaseName);
            strb.AppendLine("{");

            if (_generateAttr)
            {
                CreateDBTableAttribute(strb);

                CreateDBFieldAttribute(strb);

                CreateIndexAttribute(strb);
            }

            CreateInterfaceTable(strb, table);

            CreateInterfaceIndexKeys(strb, table.KeyList);

            strb.Append("}");
        }

        void CreateDBTableAttribute(StringBuilder strb)
        {
            strb.AppendLine("\tclass DBTableAttribute : Attribute");
            strb.AppendLine("\t{");
            strb.AppendLine("\t\tpublic string DatabaseName { get; set; }");
            strb.AppendLine("\t\tpublic string TableName { get; set; }");
            strb.AppendLine("\t\tpublic string Auto_increment { get; set; }");
            strb.AppendLine("\t\tpublic string Charset { get; set; }");
            strb.AppendLine("\t\tpublic string Engine { get; set; }");
            strb.AppendLine("\t\tpublic bool Default { get; set; }");
            strb.AppendLine("\t\tpublic string Using { get; set; }");
            strb.AppendLine("\t}");
            strb.AppendLine("");

            string temp = strb.ToString();
        }

        void CreateDBFieldAttribute(StringBuilder strb)
        {
            strb.AppendLine("\tclass DBFieldAttribute : Attribute");
            strb.AppendLine("\t{");
            strb.AppendLine("\t\tpublic string CharacterSet { get; set; }");
            strb.AppendLine("\t\tpublic string FieldDefault { get; set; }");
            strb.AppendLine("\t\tpublic string FieldName { get; set; }");
            strb.AppendLine("\t\tpublic bool HasAuto { get; set; }");
            strb.AppendLine("\t\tpublic bool HasUnsign { get; set; }");
            strb.AppendLine("\t\tpublic string Type { get; set; }");
            strb.AppendLine("\t\tpublic string Not { get; set; }");
            strb.AppendLine("\t\tpublic int Length { get; set; }");
            strb.AppendLine("\t\tpublic string Other { get; set; }");
            strb.AppendLine("\t}");
            strb.AppendLine("");

            string temp = strb.ToString();
        }

        void CreateIndexAttribute(StringBuilder strb)
        {
            strb.AppendLine("\tclass IndexOfTableAttribute : Attribute");
            strb.AppendLine("\t{");
            strb.AppendLine("\t\tpublic IndexOfTableAttribute(Type owner)");
            strb.AppendLine("\t\t{");
            strb.AppendLine("");
            strb.AppendLine("\t\t}");
            strb.AppendLine("\t}");
            strb.AppendLine("");
            strb.AppendLine("\tclass IndexKeyAttribute : Attribute");
            strb.AppendLine("\t{");
            strb.AppendLine("\t\tpublic string Kind { get; set; }");
            strb.AppendLine("\t\tpublic string Name { get; set; }");
            strb.AppendLine("\t\tpublic string Columns { get; set; }");
            strb.AppendLine("\t}");
            strb.AppendLine("");

            string temp = strb.ToString();
        }

        void CreateInterfaceTable(StringBuilder strb, TablePart table)
        {
            strb.Append("\t[DBTable(");
            if (table.DatabaseName != null) strb.Append("DatabaseName=" + '"' + table.DatabaseName + '"');
            if (table.TableName != null) strb.Append(", TableName=" + '"' + table.TableName + '"');
            if (table.Auto_increment != null) strb.Append(", Auto_increment=" + '"' + table.Auto_increment + '"');
            if (table.Charset != null) strb.Append(", Charset=" + '"' + table.Charset + '"');
            if (table.Engine != null) strb.Append(", Engine=" + '"' + table.Engine + '"');
            if (table.HasDefault) strb.Append(", Default=true");
            if (table.Using != null) strb.Append(", Using=" + '"' + table.Using + '"');
            strb.AppendLine(")]");
            strb.AppendLine("\tinterface " + table.TableName);
            strb.AppendLine("\t{");

            string temp = strb.ToString();

            if (table.FieldList.Count != 0)
            {
                FieldPart[] fields = table.FieldList.ToArray();
                for (int i = 0; i < fields.Length; ++i)
                {

                    FieldPart field = fields[i];

                    strb.Append("\t\t[DBField(");
                    if (field.FieldName != null) strb.Append("FieldName=" + '"' + field.FieldName + '"');
                    if (field.CharacterSet != null) strb.Append(", CharacterSet=" + '"' + field.CharacterSet + '"');
                    if (field.FieldDefault != null) strb.Append(", FieldDefault=" + '"' + field.FieldDefault + '"');
                    if (field.HasAuto) strb.Append(", HasAuto=true");
                    if (field.HasUnsign) strb.Append(", HasUnsign=true");
                    if (field.Type != null) strb.Append(", Type=" + '"' + field.Type + '"');
                    if (field.Not != null) strb.Append(", Not=" + '"' + field.Not + '"');
                    if (field.Length != null) strb.Append(", Length=" + field.Length);
                    if (field.Other != null) strb.Append(", Other=" + '"' + field.Other + '"');
                    strb.AppendLine(")]");

                    string kind = "";
                    switch (field.Type)
                    {
                        case "varchar": kind = "string"; break;
                        case "float": kind = "float"; break;
                        case "text": kind = "string"; break;
                        case "int": kind = "int"; break;
                        case "datetime": kind = "string"; break;
                        case "char": kind = "string"; break;
                        case "blob": kind = "string"; break;
                        case "bool": kind = "bool"; break;
                    }

                    strb.AppendLine("\t\t" + kind + " " + fields[i] + "{ get; set; }");
                }
            }
            strb.AppendLine("\t}");
            strb.AppendLine("");

            string temp2 = strb.ToString();
        }
        /// <summary>
        /// create index keys of 'current' table
        /// </summary>
        /// <param name="strb"></param>
        /// <param name="KeyList"></param>
        void CreateInterfaceIndexKeys(StringBuilder strb, List<KeyPart> KeyList)
        {
            //use nested namespace
            strb.AppendLine("\tnamespace IndexKeys{");

            strb.AppendLine("\t[IndexOfTable(typeof(" + _table.TableName + "))]");
            strb.AppendLine("\tinterface " + _table.TableName);
            strb.AppendLine("\t{");
            string temp = strb.ToString();
            KeyPart[] keys = KeyList.ToArray();
            for (int i = 0; i < keys.Length; ++i)
            {
                KeyPart key = keys[i];

                strb.Append("\t\t[IndexKey(");
                if (key.IndexName != null) strb.Append("Name=" + '"' + key.IndexName + '"');
                if (key.IndexKind != null) strb.Append(", Kind=" + '"' + key.IndexKind + '"');
                if (key.IndexColumns != null)
                {
                    strb.Append(", Columns=" + '"');
                    for (int k = 0; k < key.IndexColumns.Count; ++k)
                    {
                        if (k > 0)
                        {
                            strb.Append(",");
                        }
                        strb.Append(key.IndexColumns[0]);
                    }
                }
                strb.AppendLine('"' + ")]");
                strb.AppendLine("\t\tstring " + key.IndexName + " { get; set; }");
            }

            strb.AppendLine("\t}");
            //
            strb.AppendLine("\t}");
        }

    }
}