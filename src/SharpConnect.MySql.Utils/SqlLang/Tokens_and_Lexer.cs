//MIT, 2018, Phycolos, EngineKit and Contributos
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpConnect.MySql.SqlLang
{
    class MyParserNotSupportException : Exception { }

    public class TokenLine
    {
        List<Token> _tokenlist;
        int _currentIndex;
        int _lim;
        public int LineNumber { get; }

        public Token CurrentToken => _tokenlist[_currentIndex];

        public TokenLine() : this(0)
        {

        }
        public TokenLine(int lineNumber)
        {
            _tokenlist = new List<Token>();
            _currentIndex = -1;
            _lim = -1;
            LineNumber = lineNumber;
        }

        public void Add(Token token)
        {
            _tokenlist.Add(token);
            _lim++;
        }
        public void Update(Token token)
        {
            int index = _tokenlist.FindIndex(t => t.Location == token.Location);
            if (index >= 0)
            {
                token.WhiteSpaceCount = _tokenlist[index].WhiteSpaceCount;
                token.TokenName = _tokenlist[index].TokenName;
                _tokenlist[index] = token;
            }
        }
        public void Read()
        {
            _currentIndex++;
        }
        public void BackOneStep()
        {
            _currentIndex--;
        }
        public void ReaderReset()
        {
            _currentIndex = -1;
        }
        public void MoveToEnd()
        {
            _currentIndex = _tokenlist.Count - 1;
        }
        public bool IsBegin()
        {
            return _currentIndex <= 0;
        }
        public bool IsEnd()
        {
            if (_lim >= 0)
                return _currentIndex >= _lim;
            else
                return true;
        }

        public Token[] ToArray()
        {
            return _tokenlist.ToArray();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var tk in _tokenlist)
            {
                builder.Append(new string(' ', tk.WhiteSpaceCount) + tk.OriginalText);
            }
            return builder.ToString();
        }
    }

    public class TokenStream
    {
        readonly List<TokenLine> _tokenLines;
        readonly int _lim;
        public TokenStream(List<Token> tklist)
        {
            _tokenLines = new List<TokenLine>();
            ToTokenLine(tklist);

            CurrentLineIndex = -1;
            _lim = _tokenLines.Count - 1;
            if (_tokenLines.Count > 0)
            {
                _currentLine = _tokenLines[0];
                CurrentLineIndex = 0;
            }
        }

        public TokenStream(List<TokenLine> tokenLines)
        {
            _tokenLines = tokenLines;

            CurrentLineIndex = -1;
            _lim = _tokenLines.Count - 1;
            if (_tokenLines.Count > 0)
            {
                _currentLine = _tokenLines[0];
                CurrentLineIndex = 0;
            }
        }

        public TokenStream SubTokenStream(int startLine, int length)
        {
            List<TokenLine> tokenLines = new List<TokenLine>();
            for (int i = startLine; i < startLine + length; i++)
            {
                tokenLines.Add(_tokenLines[i]);
            }

            return new TokenStream(tokenLines);
        }

        public void UpdateToken(Token token)
        {
            TokenLine tokenLine = _tokenLines.Find((l) => l.LineNumber == token.Location.LineNo);
            if (tokenLine != null)
            {
                tokenLine.Update(token);
            }
        }

        public TokenLine[] ToLineArray()
        {
            return _tokenLines.ToArray();
        }

        public List<TokenStream> SplitBlocks(string startLineWith)
        {
            List<TokenStream> tokenStreams = new List<TokenStream>();
            List<TokenLine> temp = new List<TokenLine>();

            foreach (var line in _tokenLines)
            {
                temp.Add(line);
                line.Read();
                if (line.CurrentToken.TokenName != TokenName.NewLine)
                {
                    if (line.CurrentToken.OriginalText.StartsWith(startLineWith))
                    {
                        tokenStreams.Add(new TokenStream(temp));
                        temp = new List<TokenLine>();
                    }
                }
                line.ReaderReset();
            }

            //check remain line or not contains splittext
            if (temp.Count > 0)
            {
                tokenStreams.Add(new TokenStream(temp));
            }

            return tokenStreams;
        }

        private void ToTokenLine(List<Token> tklist)
        {
            int lineNumber = -1;//default line start with 0, 1st token line should be >= 0 
            TokenLine line = null;
            foreach (var tk in tklist)
            {
                //1st time lineNumber = -1 and location lineNo should >= 0, then create new line for store token in that line
                if (lineNumber < tk.Location.LineNo)
                {
                    lineNumber = tk.Location.LineNo;
                    line = new TokenLine(lineNumber);
                    _tokenLines.Add(line);
                }
                line.Add(tk);
            }
        }

        public Token CurrentToken => _currentLine.CurrentToken;
        private TokenLine _currentLine;

        public bool IsEnd => (CurrentLineIndex >= _tokenLines.Count - 1) && _currentLine.IsEnd();

        public void Read()
        {
            if (_currentLine.IsEnd())
            {
                if (IsEnd)
                {
                    return;
                }
                _currentLine = _tokenLines[++CurrentLineIndex];
                _currentLine.ReaderReset();
            }
            _currentLine.Read();
            CheckSkip();
        }
        private void CheckSkip()
        {
            if (CurrentToken.TokenName == TokenName.NewLine)
            {
                Read();
            }
        }

        public void BackOneStep()
        {
            if (_currentLine.IsBegin())
            {
                _currentLine = _tokenLines[--CurrentLineIndex];
                _currentLine.MoveToEnd();
            }
            else
                _currentLine.BackOneStep();
        }
        public int CurrentIndex
        {
            get;
            private set;
        }
        public int CurrentLineIndex
        {
            get;
            private set;
        }
        public int Count
        {
            get; private set;
        }
        public void MoveToEnd() => _tokenLines[_lim].MoveToEnd();

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var line in _tokenLines)
            {
                builder.Append(line);
            }
            return builder.ToString();
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
                tk.MySqlTokenName = tkname;
            }
            else
            {
                tk.MySqlTokenName = MySqlTokenName.Unknown;
            }
        }
    }

    public struct Location
    {
        public readonly int LineNo;
        public readonly int ColNo;
        public Location(int lineNo, int colNo)
        {
            LineNo = lineNo;
            ColNo = colNo;
        }
#if DEBUG
        public override string ToString()
        {
            return "(" + LineNo + "," + ColNo + ")";
        }
#endif
        public Location GetNewLocation(int inlineLen)
        {
            return new Location(LineNo, ColNo + inlineLen);
        }

        public static bool operator ==(Location left, Location right)
        {
            return (left.ColNo == right.ColNo) && (left.LineNo == right.LineNo);
        }

        public static bool operator !=(Location left, Location right)
        {
            return (left.ColNo != right.ColNo) || (left.LineNo != right.LineNo);
        }
    }

    public class Token
    {
#if DEBUG
        static int s_dbugTotalId;
        public readonly int dbugId = s_dbugTotalId++;
#endif
        public Token(string orgText, TokenName tokenName, Location location)
        {
#if DEBUG
            if (dbugId == 166)
            {

            }
#endif
            OriginalText = orgText;
            TokenName = tokenName;
            Location = location;
            WhiteSpaceCount = 0;
        }
        public Token(string orgText, Location location) : this(orgText, Lexer.GetTokenName(orgText), location)
        {
        }

        public int WhiteSpaceCount { get; set; }
        public Location Location { get; set; }
        public TokenName TokenName { get; set; }
        public string OriginalText { get; set; }
        public override string ToString()
        {
            return OriginalText;
        }
        public MySqlTokenName MySqlTokenName { get; set; }
    }
    public enum TokenName
    {
        Unknown, //default
        //
        LineComment,
        Keyword,
        Iden,
        IdenWithEscape,
        BindingIden,
        LiteralNumber,
        LiteralString,
        LiteralBoolean,
        NewLine,

        Dot, //.
        Plus,//+
        Minus,//-
        Multiply,//* 
        Div, // /
        Assign,//=
        BANG,//!
        MySql_Not, //not
        And, //&
        Or, //|
        CondAnd, //&&
        CondOr,//||
        Mod,//% , percent 

        MySql_MOD, // MOD
        MySql_DIV, //DIV
        MySqL_AND,//AND, and
        MySqL_OR,//OR, or
        MySql_EQ,
        Tilde,         //"~" 

        MySql_Is, //is       
        MySql_In, //in
        MySql_Like, //like

        Caret, //^ 
        Lesser, //<
        Greater, //>
        LesserOrEqual, //<=
        GreaterOrEqual, //>=
        NotEqual1, //<>
        NotEqual2, //!=
        NotLike,  //not like
        NotIn, //not in
        NotBetween,
        ShiftLeft,//<<
        ShiftRight,//>>

        //---------
        Dollar, //$
        At, //@
        Question,//?
        Comma, //,
        Colon, //:
        SemiColon,//;
        //-----------
        //
        BackTick,//`
        Quote, // '
        DoubleQuote, //"
        //

        //-----------
        OpenParen, // (
        CloseParen, //)
        OpenBracket, //[
        CloseBracket, //]
        OpenBrace, //{
        CloseBrace, //}


        //--------
        StarIden, //

    }
    public class Lexer
    {
        //a tokenenizer       
        TokenStream _tkStream;

        static Dictionary<string, TokenName> s_tokenNameDic = new Dictionary<string, TokenName>();
        static Dictionary<string, bool> s_mySqlKeywords = new Dictionary<string, bool>();

        static Lexer()
        {
            s_tokenNameDic.Add(",", TokenName.Comma);
            s_tokenNameDic.Add(".", TokenName.Dot);
            s_tokenNameDic.Add("+", TokenName.Plus);
            s_tokenNameDic.Add("-", TokenName.Minus);
            s_tokenNameDic.Add("*", TokenName.Multiply);
            s_tokenNameDic.Add("/", TokenName.Div);
            s_tokenNameDic.Add("=", TokenName.Assign);
            s_tokenNameDic.Add(":", TokenName.Colon);
            s_tokenNameDic.Add(";", TokenName.SemiColon);
            //

            s_tokenNameDic.Add("<<", TokenName.ShiftLeft);
            s_tokenNameDic.Add(">>", TokenName.ShiftRight);
            //
            s_tokenNameDic.Add("<", TokenName.Lesser);
            s_tokenNameDic.Add(">", TokenName.Greater);
            s_tokenNameDic.Add(">=", TokenName.GreaterOrEqual);
            s_tokenNameDic.Add("<=", TokenName.LesserOrEqual);

            s_tokenNameDic.Add("<>", TokenName.NotEqual1);
            s_tokenNameDic.Add("!=", TokenName.NotEqual2);
            s_tokenNameDic.Add("!", TokenName.BANG);

            s_tokenNameDic.Add("{", TokenName.OpenBrace);
            s_tokenNameDic.Add("}", TokenName.CloseBrace);
            s_tokenNameDic.Add("[", TokenName.OpenBracket);
            s_tokenNameDic.Add("]", TokenName.CloseBracket);
            //
            s_tokenNameDic.Add("`", TokenName.BackTick);
            s_tokenNameDic.Add("\"", TokenName.DoubleQuote);
            s_tokenNameDic.Add("'", TokenName.Quote);

            s_tokenNameDic.Add("(", TokenName.OpenParen);
            s_tokenNameDic.Add(")", TokenName.CloseParen);
            s_tokenNameDic.Add("$", TokenName.Dollar);
            s_tokenNameDic.Add("?", TokenName.Question);
            s_tokenNameDic.Add("@", TokenName.At);
            s_tokenNameDic.Add("&", TokenName.And);
            s_tokenNameDic.Add("&&", TokenName.CondAnd);
            s_tokenNameDic.Add("|", TokenName.Or);
            s_tokenNameDic.Add("||", TokenName.Or);
            s_tokenNameDic.Add("%", TokenName.Mod);

            //-----------------------
            //name as operator 
            s_tokenNameDic.Add("and", TokenName.MySqL_AND);
            s_tokenNameDic.Add("div", TokenName.MySqL_AND);
            s_tokenNameDic.Add("or", TokenName.MySqL_AND);
            s_tokenNameDic.Add("is", TokenName.MySql_Is);
            s_tokenNameDic.Add("not", TokenName.MySql_Not);
            s_tokenNameDic.Add("like", TokenName.MySql_Is);
            s_tokenNameDic.Add("in", TokenName.MySql_In);
            //------------------
            //some operator 

            //keywords...
            RegisterMySqlKeywords("select insert update set delete where from create drop asc desc default as on duplicated");
            RegisterMySqlKeywords("into values");
            RegisterMySqlKeywords("group by having order");
            RegisterMySqlKeywords("distinct");
            RegisterMySqlKeywords("join inner outer left right");
            RegisterMySqlKeywords("primary key engine charset");//***
            RegisterMySqlKeywords("null char enum float tinyint smallint mediumint int blob text true false");
            RegisterMySqlKeywords("table database limit");
            RegisterMySqlKeywords("call procedure");//call store proc
            RegisterMySqlKeywords("let");
        }
        static void RegisterMySqlKeywords(string whitespace_sep_keywords)
        {
            string[] keywords = whitespace_sep_keywords.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            RegisterMySqlKeywords(keywords);
        }
        static void RegisterMySqlKeywords(string[] keywords)
        {
            for (int i = 0; i < keywords.Length; ++i)
            {
                if (!s_mySqlKeywords.ContainsKey(keywords[i]))
                {
                    s_mySqlKeywords.Add(keywords[i], true);
                }
                else
                {

                }
            }
        }
        public static TokenName GetTokenName(string s)
        {
            if (!s_tokenNameDic.TryGetValue(s, out TokenName tkName))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("unknown-tk:" + s);
#endif

                return TokenName.Unknown;
            }
            return tkName;
        }
        static bool CheckNextTokenIs(char[] buffer, int curIndex, char expectChar)
        {
            if (curIndex < buffer.Length - 1)
            {
                //not the last one
                //then look ahead
                char c2 = buffer[curIndex + 1];
                return c2 == expectChar;
            }
            return false;
        }
        public TokenStream ResultTokenStream => _tkStream;


        Token CreateTokenFromString(string orgText, Location loca)
        {
            //check if org Text is iden or keyword 

            if (s_tokenNameDic.TryGetValue(orgText, out TokenName tkName))
            {
                return new Token(orgText, tkName, loca);
            }
            else if (s_mySqlKeywords.TryGetValue(orgText, out bool found))
            {
                //this is mysql keyword
                return new Token(orgText, TokenName.Keyword, loca);
            }
            else
            {
                return new Token(orgText, TokenName.Iden, loca);
            }
        }


        int _currentLineNo;
        int _currentLineStartAt;
        int _currentIndex;
        int _startCollectPos;
        public void Lex(string sql)
        {
            char[] buffer = sql.ToCharArray();
            List<Token> tokens = new List<Token>();
            int currentState = 0;
            _startCollectPos = 0;
            char escape_startWith = '\0';
            char escape_endWith = '\0';
            bool iden_escape = false;

            int dbugCount = 0;
            int whitespaceCount = 0;
            Action<Token> AddToken = new Action<Token>((tk) =>
            {
                tk.WhiteSpaceCount = whitespaceCount;
                whitespaceCount = 0;
                tokens.Add(tk);
            });
            _currentLineNo = _currentLineStartAt = 0;//start at 0
            for (; _currentIndex < buffer.Length; ++_currentIndex)
            {
                dbugCount++;
                char c = buffer[_currentIndex];
                //check if c is terminal or not
                switch (currentState)
                {
                    case 0:
                        {
                            if (c == '\r')
                            {
                                char n = buffer[_currentIndex + 1];
                                if (n == '\n')
                                {
                                    //_startCollectPos = _currentIndex;
                                    Token tk = new Token("\r\n", TokenName.NewLine, Loca());
                                    AddToken(tk);

                                    _currentIndex++;//= '\n'
                                    _currentLineStartAt = _currentIndex + 1;
                                    _startCollectPos = _currentIndex + 1;
                                    _currentLineNo++;
                                }
                                continue;
                            }
                            if (c == '\n')
                            {

                                Token tk = new Token("\n", TokenName.NewLine, Loca());
                                AddToken(tk);

                                _currentIndex++;
                                _currentLineStartAt = _currentIndex;
                                _startCollectPos = _currentIndex;
                                _currentLineNo++;
                                continue;
                            }
                            //init state
                            if (c == ' ')
                            {
                                _startCollectPos = _currentIndex;
                                whitespaceCount++;
                                continue;
                            }
                            if (char.IsWhiteSpace(c))
                            {
                                _startCollectPos = _currentIndex;
                                //whitespaceCount++;
                                continue;
                            }
                            //
                            //
                            if (char.IsLetter(c) || c == '_')
                            {
                                //collect the letter
                                _startCollectPos = _currentIndex;
                                currentState = 1; //collecting letter
                            }
                            else if (char.IsNumber(c))
                            {
                                //numeric token
                                //support decimal form
                                _startCollectPos = _currentIndex;
                                currentState = 3;//collect number before dot 
                            }
                            else
                            {
                                switch (c)
                                {
                                    default:
                                        {
                                            throw new NotSupportedException();
                                        }
                                    case '`':
                                        {
                                            iden_escape = true;
                                            //parse escape sequence
                                            escape_endWith = c;
                                            escape_startWith = c;
                                            currentState = 2;
                                            _startCollectPos = _currentIndex;
                                        }
                                        break;
                                    case '"':
                                    case '\'':
                                        {
                                            iden_escape = false;
                                            //parse escape sequence
                                            escape_endWith = c;
                                            escape_startWith = c;
                                            currentState = 2;
                                            _startCollectPos = _currentIndex;
                                        }
                                        break;
                                    case '@':
                                    case '$':
                                    case '?':
                                        {
                                            //must follow by letter
                                            //we collect this as binding identitfier
                                            _startCollectPos = _currentIndex;
                                            currentState = 1; //collecting letter
                                        }
                                        break;
                                    case ':':
                                    case '=': //assign or equality
                                    case '{':
                                    case '}':
                                    case '[':
                                    case ']':
                                    case ')':
                                    case '(':
                                    case '.':
                                    case '+':
                                    case '*':
                                    case '-':
                                    case ',':
                                    case ';':
                                    case '%':
                                    case '^':
                                        {
                                            //open paren
                                            Token tk = new Token(c.ToString(), Loca());
                                            AddToken(tk); //tokens.Add(tk);
                                            _startCollectPos = _currentIndex;
                                        }
                                        break;

                                    case '!':
                                        {
                                            if (CheckNextTokenIs(buffer, _currentIndex, '='))
                                            {
                                                Token tk = new Token("!=", Loca());
                                                AddToken(tk); //tokens.Add(tk);
                                                _currentIndex++;//consume next i

                                            }
                                            else
                                            {
                                                Token tk = new Token("!", Loca());
                                                AddToken(tk); //tokens.Add(tk);

                                            }
                                            _startCollectPos = _currentIndex;
                                        }
                                        break;
                                    case '<':
                                        {
                                            //TODO: impl <=> operator

                                            if (CheckNextTokenIs(buffer, _currentIndex, '='))
                                            {
                                                Token tk = new Token("<=", Loca());
                                                AddToken(tk); //tokens.Add(tk);
                                                _currentIndex++;//consume next i
                                            }
                                            else if (CheckNextTokenIs(buffer, _currentIndex, '>'))
                                            {
                                                Token tk = new Token("<>", Loca()); //!=
                                                AddToken(tk); //tokens.Add(tk);
                                                _currentIndex++;//consume next i
                                            }
                                            else if (CheckNextTokenIs(buffer, _currentIndex, '<'))
                                            {
                                                Token tk = new Token("<<", Loca());
                                                AddToken(tk); //tokens.Add(tk);
                                                _currentIndex++;//consume next i
                                            }
                                            else
                                            {
                                                Token tk = new Token("<", Loca());
                                                AddToken(tk); //tokens.Add(tk);
                                            }
                                            _startCollectPos = _currentIndex;
                                        }
                                        break;
                                    case '>':
                                        {
                                            if (CheckNextTokenIs(buffer, _currentIndex, '='))
                                            {
                                                Token tk = new Token(">=", Loca());
                                                AddToken(tk); //tokens.Add(tk);
                                                _currentIndex++;//consume next i
                                            }
                                            else if (CheckNextTokenIs(buffer, _currentIndex, '>'))
                                            {
                                                Token tk = new Token(">>", Loca());
                                                AddToken(tk); //tokens.Add(tk);
                                                _currentIndex++;//consume next i
                                            }
                                            else
                                            {
                                                Token tk = new Token(">", Loca());
                                                AddToken(tk); //tokens.Add(tk);
                                            }
                                            _startCollectPos = _currentIndex;
                                        }
                                        break;
                                    case '|':
                                        {
                                            if (CheckNextTokenIs(buffer, _currentIndex, '|'))
                                            {
                                                Token tk = new Token("||", Loca());
                                                AddToken(tk); //tokens.Add(tk);
                                                _currentIndex++;//consume next i
                                            }
                                            else
                                            {
                                                Token tk = new Token("|", Loca());
                                                AddToken(tk); //tokens.Add(tk);
                                            }
                                            _startCollectPos = _currentIndex;
                                        }
                                        break;
                                    case '&':
                                        {
                                            if (CheckNextTokenIs(buffer, _currentIndex, '&'))
                                            {
                                                Token tk = new Token("&&", Loca());
                                                AddToken(tk); //tokens.Add(tk);
                                                _currentIndex++;//consume next i
                                            }
                                            else
                                            {
                                                Token tk = new Token("&", Loca());
                                                AddToken(tk); //tokens.Add(tk);
                                            }
                                            _startCollectPos = _currentIndex;
                                        }
                                        break;
                                    case '#':
                                        {
                                            //begin with single line comment
                                            //single line comment 
                                            currentState = 5;
                                            //read until end of line
                                            _startCollectPos = _currentIndex;
                                        }
                                        break;
                                    case '/':
                                        {
                                            if (CheckNextTokenIs(buffer, _currentIndex, '/'))
                                            {
                                                //this is a line comment
                                                //begin with single line comment
                                                //single line comment 
                                                currentState = 5;
                                                //read until end of line
                                                _startCollectPos = _currentIndex;
                                            }
                                            else
                                            {
                                                Token tk = new Token(c.ToString(), Loca());
                                                AddToken(tk); //tokens.Add(tk);
                                                _startCollectPos = _currentIndex;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                        break;
                    case 1:
                        {
                            //collecting letter
                            if (char.IsLetter(c) || c == '_' || char.IsNumber(c))
                            {
                                continue;//continue collect
                            }
                            //---
                            //else => flush collecting data to a new token
                            Token tk = CreateTokenFromString(new string(buffer, _startCollectPos, _currentIndex - _startCollectPos), Loca());
                            AddToken(tk); //tokens.Add(tk);
                            //then eval this again
                            currentState = 0;
                            _startCollectPos = _currentIndex;
                            goto case 0;
                        }
                    case 2:
                        {
                            if (c == escape_endWith)
                            {
                                //flush

                                if (iden_escape)
                                {
                                    Token tk = new Token(new string(buffer, _startCollectPos, _currentIndex - _startCollectPos + 1), TokenName.IdenWithEscape, Loca());
                                    AddToken(tk); //tokens.Add(tk);
                                    iden_escape = false;//reset
                                }
                                else
                                {
                                    Token tk = new Token(new string(buffer, _startCollectPos, _currentIndex - _startCollectPos + 1), TokenName.LiteralString, Loca());
                                    AddToken(tk); //tokens.Add(tk);
                                }
                                _startCollectPos = _currentIndex;
                                currentState = 0;
                            }
                            else
                            {

                                //continue collect data
                            }
                        }
                        break;
                    case 3:
                        {
                            //collect number before dot .
                            if (char.IsNumber(c))
                            {
                                //continue collect number
                            }
                            else if (c == '.')
                            {
                                //continue collect
                                currentState = 4;
                            }
                            else if (char.IsLetter(c) || c == '_')
                            {
                                //error!
                                throw new NotSupportedException();
                            }
                            else
                            {
                                //other flush
                                Token tk = new Token(new string(buffer, _startCollectPos, _currentIndex - _startCollectPos), TokenName.LiteralNumber, Loca());
                                AddToken(tk); //tokens.Add(tk);
                                currentState = 0;
                                _startCollectPos = _currentIndex;
                                goto case 0;
                            }
                            //-----------
                        }
                        break;
                    case 4:
                        {
                            //after dot must be a number
                            if (char.IsNumber(c))
                            {
                                //continue collect number
                            }
                            else
                            {
                                //flush
                                Token tk = new Token(new string(buffer, _startCollectPos, _currentIndex - _startCollectPos), TokenName.LiteralNumber, Loca());
                                AddToken(tk); //tokens.Add(tk);
                                currentState = 0;
                                _startCollectPos = _currentIndex;
                                goto case 0;
                            }
                        }
                        break;
                    case 5:
                        {
                            do
                            {
                                if (c == '\r')
                                {
                                    //_currentLineNo++;
                                    //_currentLineStartAt = _currentIndex;

                                    if (_currentIndex < buffer.Length - 1)
                                    {
                                        char next_c = buffer[_currentIndex + 1];
                                        if (next_c == '\n')
                                        {

                                            //flush
                                            Token tk = new Token(new string(buffer, _startCollectPos, _currentIndex - _startCollectPos), TokenName.LineComment, Loca());

                                            AddToken(tk); //tokens.Add(tk);
                                            currentState = 0;
                                            _currentIndex++; //***='\n'
                                            //_currentLineStartAt = _currentIndex;
                                            //_startCollectPos = _currentIndex;
                                            break;//break from while
                                        }
                                        else
                                        {
                                            //flush
                                            Token tk = new Token(new string(buffer, _startCollectPos, _currentIndex - _startCollectPos), TokenName.LineComment, Loca());
                                            AddToken(tk); //tokens.Add(tk);
                                            currentState = 0;
                                            //_currentLineStartAt = _currentIndex;
                                            //_startCollectPos = _currentIndex;
                                            //no i++
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        //flush collecting string 
                                        Token tk = new Token(new string(buffer, _startCollectPos, _currentIndex - _startCollectPos), TokenName.LineComment, Loca());
                                        AddToken(tk); //tokens.Add(tk);
                                        currentState = 0;
                                        //_currentLineStartAt = _currentIndex;
                                        //_startCollectPos = _currentIndex;
                                        //stop here
                                    }
                                    //enter new line                                   
                                }
                                else if (c == '\n')
                                {

                                    _currentLineNo++;
                                    //flush
                                    Token tk = new Token(new string(buffer, _startCollectPos, _currentIndex - _startCollectPos), TokenName.LineComment, Loca());
                                    AddToken(tk); //tokens.Add(tk);
                                    currentState = 0;
                                    //no i++
                                    break;
                                }
                                else
                                {
                                    //collect
                                    _currentIndex++;
                                    if (_currentIndex < buffer.Length)
                                    {
                                        c = buffer[_currentIndex];
                                    }
                                    else
                                    {
                                        //just comment without newline
                                        _currentIndex--;
                                        Token tk = new Token(new string(buffer, _startCollectPos, _currentIndex - _startCollectPos), TokenName.LineComment, Loca());
                                        AddToken(tk); //tokens.Add(tk);
                                        currentState = 0;
                                        _startCollectPos = _currentIndex;
                                        goto SKIP_NEWLINE;
                                    }
                                }

                            } while (_currentIndex < buffer.Length);

                            _startCollectPos = _currentIndex;//='\n'
                            Token nl = new Token("\r\n", TokenName.NewLine, Loca());
                            AddToken(nl);
                            _startCollectPos = _currentIndex + 1;//next char after newline
                            _currentLineStartAt = _currentIndex + 1;

                            _currentLineNo++;
                        SKIP_NEWLINE: { }
                        }
                        break;
                }
            }


            //remaining data
            switch (currentState)
            {
                default:
                    {
                        if (buffer.Length > 0 &&
                            _startCollectPos < buffer.Length - 1)
                        {
                            throw new NotSupportedException();
                        }
                    }
                    break;
                case 1://collecting letter
                    {
                        Token tk = new Token(new string(buffer, _startCollectPos, buffer.Length - _startCollectPos), TokenName.Iden, Loca());
                        AddToken(tk); //tokens.Add(tk);
                    }
                    break;
                case 3: //collecting number before dot
                    {
                        Token tk = new Token(new string(buffer, _startCollectPos, buffer.Length - _startCollectPos), TokenName.LiteralNumber, Loca());
                        AddToken(tk); //tokens.Add(tk);
                    }
                    break;
                case 4://collecting number after dot
                    {
                        Token tk = new Token(new string(buffer, _startCollectPos, buffer.Length - _startCollectPos), TokenName.LiteralNumber, Loca());
                        AddToken(tk); //tokens.Add(tk);
                    }
                    break;
            }
            //***
            _tkStream = new TokenStream(tokens);
        }
        Location Loca()
        {
            return new Location(_currentLineNo, _startCollectPos - _currentLineStartAt);
        }
    }

}