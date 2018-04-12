//MIT, 2018, Phycolos
//MIT, 2015-2017, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using System.Text;

namespace SharpConnect.MySql.SqlLang
{
    enum TokenKind
    {
        Whitespace,
        Iden,
        StringLiteral,
        Keyword,
        Punc
    }
    enum WellknownTokenName
    {
        Whitespace,
        Iden,
        StringLiteral,
        Keyword,
        Punc
    }

    class Token
    {
        public string Value { get; set; }
    }

    class MySqlLexer
    {
        List<Token> tokenStream = new List<Token>();
        public MySqlLexer()
        {

        }
    }

    /// <summary>
    /// simple sql parser for MySql
    /// </summary>
    class MySqlParser
    {

    }
}