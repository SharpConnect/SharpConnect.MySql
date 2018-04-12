//MIT, 2018, Phycolos, EngineKit and Contributos
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpConnect.MySql.Parser
{
    class MySqlInfoToCsCodeGenerator
    {

        public void ConvertSQL(TablePart table, string DBName)
        {
            StringBuilder strb = new StringBuilder();
            if (table.DatabaseName == null)
            {
                table.DatabaseName = DBName;
            }
            if (table.TableName != null)
            {
                strb.AppendLine("//" + DateTime.Now.ToString("s"));

                strb.AppendLine("using System;");
                strb.AppendLine("");
                strb.AppendLine("namespace " + table.DatabaseName);
                strb.AppendLine("{");

                CreateDBTableAttribute(ref strb);

                CreateDBFieldAttribute(ref strb);

                CreateIndexAttribute(ref strb);

                CreateInterfaceTable(ref strb, table);

                CreateInterfaceIndexKeys(ref strb, table.KeyList);

                strb.Append("}");
                string temp = strb.ToString();
                System.IO.File.WriteAllText(@"MySimpleFile.cs", strb.ToString());
            }

        }

        void CreateDBTableAttribute(ref StringBuilder strb)
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

        void CreateDBFieldAttribute(ref StringBuilder strb)
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

        void CreateIndexAttribute(ref StringBuilder strb)
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

        void CreateInterfaceTable(ref StringBuilder strb, TablePart table)
        {
            strb.Append("\t[DBTable(");
            if (table.DatabaseName != null) strb.Append("DatabaseName=" + '"' + table.DatabaseName + '"');
            if (table.TableName != null) strb.Append(", TableName=" + '"' + table.TableName + '"');
            if (table.Auto_increment != null) strb.Append(", Auto_increment=" + '"' + table.Auto_increment + '"');
            if (table.Charset != null) strb.Append(", Charset=" + '"' + table.Charset + '"');
            if (table.Engine != null) strb.Append(", Engine=" + '"' + table.Engine + '"');
            if (table.HasDefault) strb.Append(", Default=" + table.HasDefault);
            if (table.Using != null) strb.Append(", Using=" + '"' + table.Using + '"');
            strb.AppendLine(")]");
            strb.AppendLine("\tinterface " + table.TableName);
            strb.AppendLine("\t{");

            string temp = strb.ToString();

            if (table.FieldList.Count != 0)
            {
                FieldPart[] field = table.FieldList.ToArray();

                for (int i = 0; i < field.Length; ++i)
                {
                    string kind = "";

                    strb.Append("\t\t[DBField(");
                    if (field[i].FieldName != null) strb.Append("FieldName=" + '"' + field[i].FieldName + '"');
                    if (field[i].CharacterSet != null) strb.Append(", CharacterSet=" + '"' + field[i].CharacterSet + '"');
                    if (field[i].FieldDefault != null) strb.Append(", FieldDefault=" + '"' + field[i].FieldDefault + '"');
                    if (field[i].HasAuto) strb.Append(", HasAuto=" + field[i].HasAuto);
                    if (field[i].HasUnsign) strb.Append(", HasUnsign=" + field[i].HasUnsign);
                    if (field[i].Type != null) strb.Append(", Type=" + '"' + field[i].Type + '"');
                    if (field[i].Not != null) strb.Append(", Not=" + '"' + field[i].Not + '"');
                    if (field[i].Length != null) strb.Append(", Length=" + field[i].Length);
                    if (field[i].Other != null) strb.Append(", Other=" + '"' + field[i].Other + '"');
                    strb.AppendLine(")]");

                    if (field[i].Type == "varchar") kind = "string";
                    else if (field[i].Type == "float") kind = field[i].Type;
                    else if (field[i].Type == "text") kind = "string";
                    else if (field[i].Type == "int") kind = field[i].Type;
                    else if (field[i].Type == "datetime") kind = "string";
                    else if (field[i].Type == "char") kind = field[i].Type;
                    else if (field[i].Type == "blob") kind = "string";


                    strb.AppendLine("\t\t" + kind + " " + field[i] + "{ get; set; }");
                }
            }
            strb.AppendLine("\t}");
            strb.AppendLine("");

            string temp2 = strb.ToString();
        }

        void CreateInterfaceIndexKeys(ref StringBuilder strb, List<KeyPart> KeyList)
        {
            strb.AppendLine("\t[IndexOfTable(typeof(patient))]");
            strb.AppendLine("\tinterface IndexKeys");
            strb.AppendLine("\t{");
            string temp = strb.ToString();
            KeyPart[] Keys = KeyList.ToArray();
            for (int i = 0; i < Keys.Length; ++i)
            {
                strb.Append("\t\t[IndexKey(");
                if (Keys[i].IndexName != null) strb.Append("Name=" + '"' + Keys[i].IndexName + '"');
                if (Keys[i].IndexKind != null) strb.Append(", Kind=" + '"' + Keys[i].IndexKind + '"');
                if (Keys[i].IndexColumns != null)
                {
                    strb.Append(", Columns=" + '"');
                    for (int k = 0; k < Keys[i].IndexColumns.Count; ++k)
                    {
                        if (k > 0)
                        {
                            strb.Append(",");
                        }
                        strb.Append(Keys[i].IndexColumns[0]);
                    }
                }
                strb.AppendLine('"' + ")]");

                strb.AppendLine("\t\tstring " + Keys[i].IndexName + " { get; set; }");

            }

            strb.AppendLine("\t}");

            string temp2 = strb.ToString();
        }

    }
}