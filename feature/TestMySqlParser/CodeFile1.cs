/*** TEST ***/
using System;

namespace screen
{
    class DBTableAttribute : Attribute
    {
        public string DatabaseName { get; set; }
        public string TableName { get; set; }
        public string Auto_increment { get; set; }
        public string Charset { get; set; }
        public string Engine { get; set; }
        public string Default { get; set; }
        public string Using { get; set; }

    }

    class DBFieldAttribute : Attribute
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

    class IndexOfTableAttribute : Attribute
    {
        public IndexOfTableAttribute(Type owner)
        {

        }
    }

    class IndexKeyAttribute : Attribute
    {
        public string Kind { get; set; }
        public string Name { get; set; }
        public string Columns { get; set; }
    }

    [DBTable(DatabaseName="", TableName="")]
    interface patient
    {
        [DBField(Length = 10)]
        int HN { get; set; }
        [DBField(Length=10)]
        string varchar { get; set; }

    }

    [IndexOfTable(typeof(patient))]
    interface IndexKeys
    {
        [IndexKey(Kind = "PRIMARY", Name = "HN", Columns = "HN")]
        string HN { get; set; }
        [IndexKey(Kind = "UNIQUE", Name = "Index_4", Columns = "HN")]
        string Index_4 { get; set; }
        [IndexKey(Kind = "INDEX", Name = "Index_2", Columns = "p_fname,p_lname")]
        string Index_2 { get; set; }
        [IndexKey(Kind = "INDEX", Name = "Index_3", Columns = "gender")]
        string Index_3 { get; set; }
        [IndexKey(Kind = "FULLTEXT", Name = "Index_5", Columns = "disease")]
        string Index_5 { get; set; }
    }

}