//MIT, 2018, Phycolos, EngineKit and Contributos
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpConnect.MySql.SqlLang
{
    class MyParserNotSupportException : Exception { }

    class TokenStream
    {
        List<Token> _tokenList;
        public TokenStream(List<Token> tklist)
        {
            _tokenList = tklist;
            Count = tklist.Count;
        }
        public Token CurrentToken
        {
            get { return _tokenList[CurrentIndex]; }
        }
         
        public bool IsEnd
        {
            get
            {
                return CurrentIndex >= Count;
            }
        }
        public void ReadNext()
        {
            CurrentIndex++;
        }

        public int CurrentIndex
        {
            get;
            set;
        }
        public int Count
        {
            get; private set;
        }

    }

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

    public class FieldPart
    {
        public string FieldName;
        public string Length;
        public string Type;

        //-----------------------------
        //public int A; //non-nullable
        //public float B; //non-nullable
        //-----------------------------
        //public int? A1; //nullable
        //public float? B1; //nullable 
        //-----------------------------


        public bool HasUnsign;
        public string CharacterSet;
        public string Not;
        public bool HasAuto;
        public string FieldDefault;
        public string Other;

        public override string ToString()
        {
            return FieldName;
        }
    }

    public class KeyPart
    {
        public string IndexName;
        public string IndexKind;

        public List<string> IndexColumns;

        public override string ToString()
        {
            return IndexKind;
        }
    }

    public enum MySqlTokenName
    {
        Unknown,
        Iden,
        Create,
        Database,
        Table,
        Unsigned,
        Auto_Increment,
        FieldTypeWithParen,
        FieldType,
        Comma,
        CharacterSet,
        Not,
        Null,
        PrimaryKey,
        UniqueKey,
        IndexKey,
        FulltextKey,
        Using,
        ParenOpen,
        ParenClose,
        Default,
        Set,
        Engine,
        Drop,
    }

    class TokenNameDict
    {

        Dictionary<string, MySqlTokenName> _registerTokenNames = new Dictionary<string, MySqlTokenName>();
        public TokenNameDict()
        {
            RegisterTokenName("drop", MySqlTokenName.Drop);

            RegisterTokenName("create", MySqlTokenName.Create);
            RegisterTokenName("table", MySqlTokenName.Table);
            RegisterTokenName("not", MySqlTokenName.Not);
            RegisterTokenName("null", MySqlTokenName.Null);
            RegisterTokenName("auto_increment", MySqlTokenName.Auto_Increment);
            RegisterTokenName("unsigned", MySqlTokenName.Unsigned);
            RegisterTokenName("primary", MySqlTokenName.PrimaryKey);
            RegisterTokenName("unique", MySqlTokenName.UniqueKey);
            RegisterTokenName("key", MySqlTokenName.IndexKey);
            RegisterTokenName("fulltext", MySqlTokenName.FulltextKey);
            RegisterTokenName("using", MySqlTokenName.Using);
            RegisterTokenName("character", MySqlTokenName.CharacterSet);
            RegisterTokenName("charset", MySqlTokenName.CharacterSet);
            RegisterTokenName("default", MySqlTokenName.Default);
            RegisterTokenName("set", MySqlTokenName.Set);
            RegisterTokenName("engine", MySqlTokenName.Engine);

            //data type without paren -----
            RegisterTokenName("text", MySqlTokenName.FieldType);
            RegisterTokenName("blob", MySqlTokenName.FieldType);
            RegisterTokenName("float", MySqlTokenName.FieldType);

            RegisterTokenName("tinytext", MySqlTokenName.FieldType);
            RegisterTokenName("mediumtext", MySqlTokenName.FieldType);
            RegisterTokenName("mediumblob", MySqlTokenName.FieldType);
            RegisterTokenName("longtext", MySqlTokenName.FieldType);
            RegisterTokenName("longblob", MySqlTokenName.FieldType);

            RegisterTokenName("timestamp", MySqlTokenName.FieldType);
            RegisterTokenName("datetime", MySqlTokenName.FieldType);
            RegisterTokenName("date", MySqlTokenName.FieldType);
            RegisterTokenName("time", MySqlTokenName.FieldType);
            RegisterTokenName("xml", MySqlTokenName.FieldType);
        }
        void RegisterTokenName(string orgText, MySqlTokenName tkname)
        {
            _registerTokenNames.Add(orgText, tkname);
        }
        public void AssignTokenName(Token tk)
        {
            MySqlTokenName tkname;
            if (_registerTokenNames.TryGetValue(tk.OriginalText.ToLower(), out tkname))
            {
                tk.TokenName = tkname;
            }
            else
            {
                tk.TokenName = MySqlTokenName.Unknown;
            }
        }
    }


    class Token
    {
        public Token() { }
        public Token(string orgText)
        {
            OriginalText = orgText;
        }
        public string OriginalText { get; set; }
        public override string ToString()
        {
            return OriginalText;
        }
        public MySqlTokenName TokenName { get; set; }
    }

    class CompoundToken : Token
    {
        public string TypeName;
        public string Content;

        public override string ToString()
        {
            return TypeName + "(" + Content + ")";
        }
    }


    public class MySqlParser
    {

        public void ParseSql(string createSql)
        {
            //----------------------
            //1. tokenization => tokenizer
            //2. parse => parser
            //3. semantic checking => semantic checker

            //1.1 
            string[] tokens = createSql.Split(new char[] { ' ', '\n', '\r', '=', ';' }, StringSplitOptions.RemoveEmptyEntries);
            TokenNameDict tkDict = new TokenNameDict();


            //1.2 primary tokename
            List<Token> tokenList = new List<Token>();
            foreach (string orgTk in tokens)
            {

#if DEBUG
                if (orgTk == "`Index_2`")
                {

                }
#endif
                //check iden
                if (orgTk.StartsWith("`"))
                {
                    //TODO:
                    //create iden  token here
                    Token token = new Token(orgTk);
                    token.TokenName = MySqlTokenName.Iden;
                    tokenList.Add(token);

                    continue;
                }

                if (orgTk.EndsWith(","))
                {
                    string[] temp = orgTk.Split(',');

                    foreach (string each in temp)
                    {
                        if (each != "")
                        {
                            Token tokenIden = new Token(each);
                            tokenIden.TokenName = MySqlTokenName.Iden;
                            tkDict.AssignTokenName(tokenIden);
                            tokenList.Add(tokenIden);
                        }
                        else
                        {
                            Token tokenComma = new Token(",");
                            tokenComma.TokenName = MySqlTokenName.Comma;
                            tokenList.Add(tokenComma);
                        }
                    }

                    continue;
                }

                if (orgTk.StartsWith(")"))
                {
                    Token token = new Token(orgTk);
                    token.TokenName = MySqlTokenName.ParenClose;
                    tokenList.Add(token);

                    continue;
                }

                //check paren
                int openParen_pos = orgTk.IndexOf('(');
                if (openParen_pos > -1)
                {
                    if (openParen_pos > 0)
                    {
                        int closeParen_pos = orgTk.LastIndexOf(')');
                        if (closeParen_pos < 0)
                        {
                            //TODO: check if this can occur?
                            throw new MyParserNotSupportException();
                        }
                        //----------------- 
                        CompoundToken compToken = new CompoundToken();
                        compToken.TypeName = orgTk.Substring(0, openParen_pos);
                        compToken.Content = orgTk.Substring(openParen_pos + 1, closeParen_pos - openParen_pos - 1);
                        compToken.TokenName = MySqlTokenName.FieldTypeWithParen;
                        //tkDict.AssignTokenName(compToken);
                        tokenList.Add(compToken);
                    }
                    else //openParen_pos == 0
                    {
                        Token token = new Token(orgTk);
                        tkDict.AssignTokenName(token);

                        if (orgTk.EndsWith(")"))
                        {
                            token.TokenName = MySqlTokenName.Iden;
                        }
                        else
                        {
                            token.TokenName = MySqlTokenName.ParenOpen;
                        }

                        tokenList.Add(token);
                    }
                    continue;
                }
                else
                {
                    Token token = new Token(orgTk);
                    tkDict.AssignTokenName(token);
                    tokenList.Add(token);
                }

            }

            //2.1 ....
            //parse
            TokenStream tokenstrm = new TokenStream(tokenList);
            int count = tokenstrm.Count;

            TablePart tableResult;
            tableTreeList = new List<TablePart>();

            while (!tokenstrm.IsEnd)
            {
                Token tk = tokenstrm.CurrentToken;
                switch (tk.TokenName)
                {
                    case MySqlTokenName.Create:
                        {
                            tokenstrm.ReadNext();
                            Token nextTk = tokenstrm.CurrentToken;
                            switch (nextTk.TokenName)
                            {
                                case MySqlTokenName.Table:
                                    {
                                        TablePart table = new TablePart();
                                        tokenstrm.ReadNext();
                                        tableResult = ParseCreateTable(tokenstrm, table);
                                        tableTreeList.Add(tableResult);
                                    }
                                    break;
                            }
                        }
                        break;
                }
                tokenstrm.ReadNext();
            }
        }

        List<TablePart> tableTreeList;
        public List<TablePart> ResultTables
        {
            get { return tableTreeList; }
        }


        void ParseIden(TokenStream tkstream, ref TablePart table)
        {
            Token iden = tkstream.CurrentToken;
            if (iden.TokenName == MySqlTokenName.Iden)
            {
                //get table name here
                table.TableName = iden.ToString();

                string[] splitName = iden.OriginalText.Split('.');

                if (splitName.Length > 1)
                {
                    string[] dbName = splitName[0].Split(new char[] { '`' }, StringSplitOptions.RemoveEmptyEntries);
                    table.DatabaseName = dbName[0];
                    string[] tbName = splitName[1].Split(new char[] { '`' }, StringSplitOptions.RemoveEmptyEntries);
                    table.TableName = tbName[0];
                }
                else
                {
                    string[] tbName = splitName[0].Split(new char[] { '`' }, StringSplitOptions.RemoveEmptyEntries);
                    table.TableName = tbName[0];
                }


                tkstream.ReadNext();
            }
            else
            {
                //HANDLE 
                throw new MyParserNotSupportException();
            }

        }

        void ParseOpenParen(TokenStream tkstream)
        {
            //open

            Token iden = tkstream.CurrentToken;
            tkstream.ReadNext();
        }

        void ParseContent(TokenStream tkstream, ref TablePart table, ref FieldPart field, ref KeyPart key)
        {
            //get fields over here
            Token iden = tkstream.CurrentToken;

            switch (tkstream.CurrentToken.OriginalText)
            {
                case ",":
                    {
                        switch (table.PrimaryKey)
                        {
                            case null:
                                {
                                    table.FieldList.Add(field);
                                    field = new FieldPart();
                                }
                                break;
                            default:
                                {
                                    if (key.IndexKind != null)
                                    {
                                        table.KeyList.Add(key);
                                        key = new KeyPart();
                                    }
                                }
                                break;
                        }
                    }
                    break;
                default:
                    {
                        switch (iden.TokenName)
                        {
                            case MySqlTokenName.Iden:
                                {
                                    string[] fieldName = iden.OriginalText.Split(new char[] { '`' }, StringSplitOptions.RemoveEmptyEntries);
                                    field.FieldName = fieldName[0];
                                }
                                break;
                            case MySqlTokenName.FieldTypeWithParen:
                                {
                                    CompoundToken ctk = (CompoundToken)iden;
                                    field.Type = ctk.TypeName;
                                    field.Length = ctk.Content;
                                }
                                break;
                            case MySqlTokenName.FieldType:
                                {
                                    field.Type = iden.OriginalText;
                                }
                                break;
                            case MySqlTokenName.Unsigned:
                                {
                                    field.HasUnsign = true;
                                }
                                break;
                            case MySqlTokenName.CharacterSet:
                                {
                                    tkstream.ReadNext();
                                    tkstream.ReadNext();
                                    Token idenNext = tkstream.CurrentToken;
                                    if (idenNext.TokenName == MySqlTokenName.Unknown)
                                    {
                                        field.CharacterSet = idenNext.OriginalText;
                                    }
                                }
                                break;
                            case MySqlTokenName.Not:
                                {
                                    tkstream.ReadNext();
                                    Token idenNext = tkstream.CurrentToken;
                                    field.Not = idenNext.OriginalText;
                                }
                                break;
                            case MySqlTokenName.Default:
                                {
                                    tkstream.ReadNext();
                                    Token idenNext = tkstream.CurrentToken;
                                    string[] dfValue = idenNext.OriginalText.Split(new string[] { "'" }, StringSplitOptions.RemoveEmptyEntries);
                                    if (dfValue.Length > 0)
                                    {
                                        field.FieldDefault = dfValue[0];
                                    }
                                    else
                                    {
                                        field.FieldDefault = "";
                                    }
                                }
                                break;
                            case MySqlTokenName.Unknown:
                                {
                                    field.Other = iden.OriginalText;
                                }
                                break;
                            case MySqlTokenName.PrimaryKey:
                                {
                                    tkstream.ReadNext();
                                    key.IndexKind = "PRIMARY";
                                }
                                goto case MySqlTokenName.IndexKey;
                            case MySqlTokenName.UniqueKey:
                                {
                                    tkstream.ReadNext();
                                    key.IndexKind = "UNIQUE";
                                }
                                goto case MySqlTokenName.IndexKey;
                            case MySqlTokenName.FulltextKey:
                                {
                                    tkstream.ReadNext();
                                    key.IndexKind = "FULLTEXT";
                                }
                                goto case MySqlTokenName.IndexKey;
                            case MySqlTokenName.IndexKey:
                                {
                                    tkstream.ReadNext();
                                    if (key.IndexKind == null)
                                    {
                                        key.IndexKind = "INDEX";
                                    }
                                    if (key.IndexKind == "PRIMARY")
                                    {
                                        string[] pkName = tkstream.CurrentToken.OriginalText.Split(new char[] { '`', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                                        table.PrimaryKey = pkName[0];
                                    }
                                    Token idenNext = tkstream.CurrentToken;
                                    string[] ndName = idenNext.OriginalText.Split(new char[] { '`', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                                    key.IndexName = ndName[0];

                                    key.IndexColumns = new List<string>();

                                    if (key.IndexKind != "PRIMARY")
                                    {
                                        tkstream.ReadNext();
                                    }
                                    if (tkstream.CurrentToken.OriginalText.StartsWith("("))
                                    {
                                        string[] temp = tkstream.CurrentToken.OriginalText.Split('(');
                                        string pos = temp[1];

                                        while (!pos.EndsWith(")"))
                                        {
                                            string[] keyName = pos.Split(new char[] { '`' }, StringSplitOptions.RemoveEmptyEntries);
                                            key.IndexColumns.Add(keyName[0]);
                                            tkstream.ReadNext();
                                            pos = tkstream.CurrentToken.OriginalText;
                                        }
                                        if (pos.EndsWith(")"))
                                        {
                                            string[] temp2 = pos.Split(')');
                                            string[] keyName = temp2[0].Split(new char[] { '`' }, StringSplitOptions.RemoveEmptyEntries);
                                            key.IndexColumns.Add(keyName[0]);
                                            break;
                                        }
                                    }

                                }
                                break;
                            case MySqlTokenName.Using:
                                {
                                    tkstream.ReadNext();
                                    Token idenNext = tkstream.CurrentToken;
                                    table.Using = idenNext.OriginalText;
                                }
                                break;
                            case MySqlTokenName.Auto_Increment:
                                {
                                    field.HasAuto = true;
                                }
                                break;
                            default:
                                field.Other = iden.OriginalText;
                                break;
                        }
                    }
                    break;
            }

            tkstream.ReadNext();

        }

        void ParseCloseParen(TokenStream tkstream, ref TablePart table, ref KeyPart key)
        {
            //close
            if (key.IndexKind != null)
            {
                table.KeyList.Add(key);
                key = new KeyPart();
            }

            Token iden = tkstream.CurrentToken;
            tkstream.ReadNext();
        }

        void ParseLastContent(TokenStream tkstream, ref TablePart table)
        {
            Token iden = tkstream.CurrentToken;

            if (iden.TokenName == MySqlTokenName.Engine)
            {
                tkstream.ReadNext();
                Token idenNext = tkstream.CurrentToken;
                table.Engine = idenNext.OriginalText;
            }
            else if (iden.TokenName == MySqlTokenName.Auto_Increment)
            {
                tkstream.ReadNext();
                Token idenNext = tkstream.CurrentToken;
                table.Auto_increment = idenNext.OriginalText;
            }
            else if (iden.TokenName == MySqlTokenName.Default)
            {
                table.HasDefault = true;
            }
            else if (iden.TokenName == MySqlTokenName.CharacterSet)
            {
                tkstream.ReadNext();
                Token idenNext = tkstream.CurrentToken;
                table.Charset = idenNext.OriginalText;
            }
            else
            {
                //more condition
                throw new MyParserNotSupportException();
            }

            tkstream.ReadNext();
        }

        TablePart ParseCreateTable(TokenStream tkstream, TablePart table)
        {

            //TablePart table = new TablePart();
            FieldPart field = new FieldPart();
            KeyPart key = new KeyPart();

            ParseIden(tkstream, ref table);
            table.FieldList = new List<FieldPart>();
            table.KeyList = new List<KeyPart>();

            if (tkstream.CurrentToken.TokenName == MySqlTokenName.ParenOpen)
            {
                ParseOpenParen(tkstream);
            }
            else
            {
                throw new MyParserNotSupportException();
            }
            //..
            //fields ...

            //Token iden = tkstream.CurrentToken;

            while (tkstream.CurrentToken.TokenName != MySqlTokenName.ParenClose)
            {
                string item = tkstream.CurrentToken.ToString();

                /*if (tkstream.CurrentToken.OriginalText == ",")
                {
                    table.FieldList.Add(field);

                    field = new FieldPart();
                }*/

                ParseContent(tkstream, ref table, ref field, ref key);
                //table.FieldList.Add(field);
            }

            //....
            ParseCloseParen(tkstream, ref table, ref key);

            while (tkstream.CurrentToken.TokenName != MySqlTokenName.ParenClose)
            {
                ParseLastContent(tkstream, ref table);
                if (tkstream.IsEnd || tkstream.CurrentToken.TokenName == MySqlTokenName.Drop)
                {
                    break;
                }
            }

            return table;
        }
    }
}