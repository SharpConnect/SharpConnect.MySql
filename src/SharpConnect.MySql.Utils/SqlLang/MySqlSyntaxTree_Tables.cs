//MIT, 2019-present, Brezza92, EngineKit
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpConnect.MySql.SqlLang
{

    public class TablePart
    {
        public string TableName;
        public string DatabaseName;

        public string PrimaryKey;
        //public string IndexKey;
        public string Engine;
        public string Auto_increment;
        public bool HasDefault;
        public string Charset;
        public string Using;

        public List<KeyPart> KeyList;
        public List<FieldPart> FieldList;

        public override string ToString()
        {
            return TableName;
        }
    }

    public abstract class FieldType
    {
        public string TypeName { get; set; }
        public abstract void WriteTo(CodeStringBuilder stbuilder);

        public object OtherSemanticType { get; set; }        
#if DEBUG
        public override string ToString()
        {
            CodeStringBuilder stbuilder = new CodeStringBuilder();
            WriteTo(stbuilder);
            return stbuilder.GetStringContent();
        }
#endif
    }

    public class SimpleFieldType : FieldType
    {
        public SimpleFieldType(string primitiveTypeName)
        {
            TypeName = primitiveTypeName;
            LengthDecimalPart = -1;//default
            LengthIntegerPart = -1;//default
        }
        public int LengthIntegerPart { get; set; }
        public int LengthDecimalPart { get; set; }
        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append(TypeName);
            if (LengthIntegerPart > -1)
            {
                stbuilder.Append('(');
                stbuilder.Append(LengthIntegerPart.ToString());
                if (LengthDecimalPart > -1)
                {
                    stbuilder.Append(',');
                    stbuilder.Append(LengthDecimalPart.ToString());
                }
                stbuilder.Append(')');
            }
        }

    }
    public class EnumFieldType : FieldType
    {
        public List<string> EnumMembers = new List<string>();
        public EnumFieldType()
        {
            TypeName = "enum";
        }
        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append("enum(");
            int i = 0;
            foreach (string enumMb in EnumMembers)
            {
                if (i > 0) stbuilder.Append(',');
                stbuilder.Append(enumMb);
            }
            stbuilder.Append(')');
        }

    }

    public class FieldPart
    {
        public string FieldName { get; set; }
        public FieldType FieldType;
        public IdenExpression FieldExpr { get; set; }
        //-----------------------------
        //public int A; //non-nullable
        //public float B; //non-nullable
        //-----------------------------
        //public int? A1; //nullable
        //public float? B1; //nullable 
        //-----------------------------          
        public string CharacterSet;
        public bool NotNull;
        public bool HasAutoIncrement;
        public Expression DefaultValue;
        public string Other;

        public void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append(FieldName);
            stbuilder.Append(' ');
            FieldType.WriteTo(stbuilder);
            //TODO: add 
            if (DefaultValue != null)
            {
                stbuilder.Append("default ");
                DefaultValue.WriteTo(stbuilder);
            }
        }
#if DEBUG
        public override string ToString()
        {
            CodeStringBuilder stbuilder = new CodeStringBuilder();
            WriteTo(stbuilder);
            return stbuilder.GetStringContent();
        }
#endif


    }

    public class KeyPart
    {
        public string IndexName;
        public string IndexKind;
        public List<string> IndexColumns = new List<string>();

        public void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append(IndexKind);
            stbuilder.Append(' ');
            stbuilder.Append(IndexKind);
            int j = IndexColumns.Count;
            if (j > 0)
            {
                for (int i = 0; i < j; ++i)
                {
                    if (i > 0) stbuilder.Append(',');
                    stbuilder.Append(IndexColumns[i]);
                }
            }
        }
    }

}