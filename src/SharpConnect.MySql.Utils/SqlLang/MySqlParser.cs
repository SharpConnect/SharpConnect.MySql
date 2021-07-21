//MIT, 2019-present, Brezza92, EngineKit
using System;
using System.Collections.Generic;


namespace SharpConnect.MySql.SqlLang
{

    public abstract class MySqlParserBase
    {
        protected Stack<Expression> _expressions = new Stack<Expression>();
        protected TokenStream _tkstream;

        protected bool ExpectLiteralNumber()
        {
            if (_tkstream.IsEnd)
            {
                return false;
            }
            _tkstream.Read();
            Token tk = _tkstream.CurrentToken;
            if (tk.TokenName == TokenName.LiteralNumber)
            {
                //accept
                _expressions.Push(new NumberLiteralExpression(tk));
                return true;
            }
            else
            {
                //backward
                _tkstream.BackOneStep();
                return false;
            }
        }
        protected bool ExpectLiteralString()
        {
            if (_tkstream.IsEnd)
            {
                return false;
            }
            _tkstream.Read();
            Token tk = _tkstream.CurrentToken;
            if (tk.TokenName == TokenName.LiteralString)
            {
                //accept
                _expressions.Push(new StringLiteralExpression(tk));
                return true;
            }
            else
            {
                //backward

                _tkstream.BackOneStep();
                return false;
            }
        }
        protected bool ExpectPuncs(TokenName[] tokenNames)
        {
            if (_tkstream.IsEnd)
            {
                return false;
            }
            _tkstream.Read();
            Token tk = _tkstream.CurrentToken;

            for (int i = 0; i < tokenNames.Length; ++i)
            {
                if (tk.TokenName == tokenNames[i])
                {
                    return true;
                }
            }

            //backward
            _tkstream.BackOneStep();
            return false;
        }
        protected bool ExpectPunc(TokenName tokenName)
        {
            if (_tkstream.IsEnd)
            {
                return false;
            }
            _tkstream.Read();
            Token tk = _tkstream.CurrentToken;
            if (tk.TokenName == tokenName)
            {
                //accept
                return true;
            }
            else
            {
                //backward
                _tkstream.BackOneStep();
                return false;
            }
        }
        protected bool Expect_CaseInsensitive(params string[] strList)
        {
            if (_tkstream.IsEnd)
            {
                return false;
            }
            _tkstream.Read();
            Token tk = _tkstream.CurrentToken;
            string tkOrgText = tk.OriginalText.ToLower();
            foreach (string str in strList)
            {
                if (tkOrgText == str)
                {
                    return true;
                }
            }
            _tkstream.BackOneStep();
            return false;
        }
        protected Token CurrentToken => _tkstream.CurrentToken;
        protected bool Expect(string org)
        {
            if (_tkstream.IsEnd)
            {
                return false;
            }
            _tkstream.Read();
            Token tk = _tkstream.CurrentToken;
            if (tk.OriginalText == org)
            {
                //accept
                return true;
            }
            else
            {
                //backward
                _tkstream.BackOneStep();
                return false;
            }
        }
        protected bool ExpectKeywords(string[] keywords)
        {
            if (_tkstream.IsEnd)
            {
                return false;
            }
            _tkstream.Read();
            Token tk = _tkstream.CurrentToken;

            if (tk.TokenName == TokenName.Keyword)
            {
                for (int i = 0; i < keywords.Length; ++i)
                {
                    if (keywords[i] == tk.OriginalText)
                    {
                        return true;
                    }
                }
                _tkstream.BackOneStep();
                return false;
            }
            else
            {
                _tkstream.BackOneStep();
                return false;
            }
        }
        protected bool ExpectToken(TokenName tkname)
        {
            if (_tkstream.IsEnd)
            {
                return false;
            }
            _tkstream.Read();
            Token tk = _tkstream.CurrentToken;
            if (tk.TokenName == tkname)
            {
                return true;
            }
            else
            {
                _tkstream.BackOneStep();
                return false;
            }
        }
        protected bool ExpectKeyword(string keyword)
        {
            if (_tkstream.IsEnd)
            {
                return false;
            }
            _tkstream.Read();
            Token tk = _tkstream.CurrentToken;
            if (tk.TokenName == TokenName.Keyword
                && tk.OriginalText == keyword)
            {
                return true;
            }
            else
            {
                _tkstream.BackOneStep();
                return false;
            }
        }
        protected bool ExpectKeyword()
        {
            if (_tkstream.IsEnd)
            {
                return false;
            }
            _tkstream.Read();
            Token tk = _tkstream.CurrentToken;
            if (tk.TokenName == TokenName.Keyword)
            {
                return true;
            }
            else
            {
                _tkstream.BackOneStep();
                return false;
            }
        }
        protected bool ExpectIden()
        {
            if (_tkstream.IsEnd)
            {
                return false;
            }
            _tkstream.Read();
            Token tk = _tkstream.CurrentToken;
            if (tk.TokenName == TokenName.Iden ||
                tk.TokenName == TokenName.IdenWithEscape)
            {
                _expressions.Push(new IdenExpression(tk));
                return true;
            }
            else
            {
                _tkstream.BackOneStep();
                return false;
            }
        }

    }
    public class MySqlExpressionParser : MySqlParserBase
    {
        static readonly TokenName[] s_binaryOps = new TokenName[] {
                    TokenName.Plus,TokenName.Minus ,
                    TokenName.Div,TokenName.Multiply,
                    TokenName.Lesser , TokenName.LesserOrEqual,
                    TokenName.Greater, TokenName.GreaterOrEqual,
                    TokenName.Assign};

        static readonly TokenName[] s_unaryOps = new TokenName[] {
                    TokenName.Dot,
                    TokenName.OpenParen};


        public Expression GetResultExpression()
        {
            if (_expressions.Count > 1)
            {
                throw new NotSupportedException();
            }
            else if (_expressions.Count == 0)
            {
                return null;
            }

            Expression expr = _expressions.Pop();
            while (expr.Owner != null)
            {
                expr = (Expression)expr.Owner;
            }
            return expr;
        }
        public void Parse(string expression)
        {
            //1. tokenization => tokenizer
            Lexer lexer = new Lexer();
            lexer.Lex(expression);
            //2. parse
            _tkstream = lexer.ResultTokenStream;
            Parse(_tkstream);

        }
        public void Parse(TokenStream tkstream)
        {
            //this start at each expression
            //expression not start with keyword
            //it start with iden, literal, some op,some punc
            _tkstream = tkstream;
            int state = 0;
            
            while (!_tkstream.IsEnd)
            {
                _tkstream.Read();

                //sql statement start with keyword
                Token currentToken = _tkstream.CurrentToken;
                //parse sql statement
                switch (state)
                {
                    default: throw new NotSupportedException();
                    case 100: break;//stop
                    case 0:
                        {
                            //begin
                            switch (currentToken.TokenName)
                            {
                                default:
                                    throw new NotSupportedException();
                                case TokenName.BlockComment:
                                    //temp ignore?
                                    break;
                                case TokenName.BANG:
                                case TokenName.MySql_Not:
                                case TokenName.Plus:
                                case TokenName.Minus:
                                case TokenName.Tilde:
                                    {
                                        //unary operator***
                                        var unaryOpExpr = new UnaryOpExpression();
                                        unaryOpExpr.SetBeginLocation(currentToken);

                                        unaryOpExpr.Op = new ExprOperator(currentToken);
                                        MergeExpression(unaryOpExpr);


                                        state = 0;//post unary op
                                    }
                                    break;
                                case TokenName.OpenParen:
                                    {
                                        //start new parsing context
                                        Token open_paren = CurrentToken;

                                        var subParser = new MySqlExpressionParser();
                                        subParser.Parse(tkstream);
                                        tkstream.Read();
                                        if (_tkstream.CurrentToken.TokenName == TokenName.CloseParen)
                                        {

                                            //ok
                                            var parenExpr = new ParenExpression();
                                            parenExpr.SetLocation(open_paren, _tkstream.CurrentToken);
                                            //

                                            Expression resultExpression = subParser.GetResultExpression();
                                            if (resultExpression != null)
                                            {
                                                parenExpr.Expression = resultExpression;
                                            }
                                            else
                                            {
                                                throw new NotSupportedException();
                                            }

                                            MergeExpression(parenExpr);
                                            state = 1;//post value
                                        }
                                        else
                                        {
                                            throw new NotSupportedException();
                                        }
                                    }
                                    break;
                                case TokenName.Keyword:
                                    {
                                        string keyword = currentToken.OriginalText.ToLower();
                                        switch (keyword)
                                        {
                                            case "null":
                                                MergeExpression(new NullLiteralExpression(currentToken));
                                                state = 1;//post value
                                                break;
                                            case "true":
                                                MergeExpression(new BooleanLiteralExpression(currentToken));
                                                state = 1;//post value
                                                break;
                                            case "false":
                                                MergeExpression(new BooleanLiteralExpression(currentToken));
                                                state = 1;//post value
                                                break;
                                            default:
                                                //exit from current parsing context
                                                _tkstream.BackOneStep();
                                                return;
                                        }
                                    }
                                    break;
                                case TokenName.CloseParen:
                                    //exit from current parsing context
                                    _tkstream.BackOneStep();
                                    return;
                                case TokenName.CloseBrace:
                                    _tkstream.BackOneStep();
                                    return;
                                case TokenName.CloseBracket:
                                    _tkstream.BackOneStep();
                                    return;
                                case TokenName.Comma:
                                    _tkstream.BackOneStep();
                                    return;
                                case TokenName.LiteralBoolean:
                                    MergeExpression(new BooleanLiteralExpression(currentToken));
                                    state = 1;//post value
                                    break;
                                case TokenName.LiteralNumber:
                                    MergeExpression(new NumberLiteralExpression(currentToken));
                                    state = 1;//post value
                                    break;
                                case TokenName.LiteralString:
                                    MergeExpression(new StringLiteralExpression(currentToken));
                                    state = 1;//post value
                                    break;
                                case TokenName.Iden:
                                case TokenName.IdenWithEscape:
                                    MergeExpression(new IdenExpression(currentToken));
                                    state = 1;//post value
                                    break;
                                case TokenName.SemiColon:

                                    _tkstream.MoveToEnd();
                                    state = 100;//post value
                                    break;

                                //-----------------------
                                //our extensions
                                case TokenName.OpenBracket:
                                    {
                                        BracketArrayExpression arrObjExpression = new BracketArrayExpression();
                                        arrObjExpression.SetBeginLocation(CurrentToken);//***

                                    PARSE_NEXT_MEMBER:
                                        var subParser = new MySqlExpressionParser();
                                        subParser.Parse(tkstream);
                                        Expression arg = subParser.GetResultExpression();
                                        arrObjExpression.AddMember(arg);
                                        tkstream.Read();

                                        currentToken = _tkstream.CurrentToken;
                                        if (currentToken.TokenName == TokenName.Comma)
                                        {
                                            goto PARSE_NEXT_MEMBER;
                                        }
                                        else if (currentToken.TokenName == TokenName.CloseBracket)
                                        {
                                            arrObjExpression.SetEndLocation(currentToken);
                                            MergeExpression(arrObjExpression);
                                        }

                                        state = 0;
                                    }
                                    break;
                                case TokenName.OpenBrace:
                                    {
                                        //our extension,
                                        //support brace object selection, eg. json object

                                        BraceObjectExpression braceObject = new BraceObjectExpression();
                                        braceObject.SetBeginLocation(CurrentToken);

                                    PARSE_NEXT_MEMBER:

                                        var subParser = new MySqlExpressionParser();
                                        subParser.Parse(tkstream);
                                        Expression arg = subParser.GetResultExpression();
                                        if (arg is KeyValueExpression keyValueExpr)
                                        {
                                            braceObject.AddMember(keyValueExpr);
                                        }
                                        else
                                        {
                                            //TODO: 
                                            //create pseudo key-value expression
                                            throw new NotSupportedException();
                                        }

                                        tkstream.Read();

                                        currentToken = _tkstream.CurrentToken;
                                        if (currentToken.TokenName == TokenName.Comma)
                                        {
                                            goto PARSE_NEXT_MEMBER;
                                        }
                                        else if (currentToken.TokenName == TokenName.CloseBrace)
                                        {
                                            braceObject.SetEndLocation(currentToken);
                                            MergeExpression(braceObject);
                                        }

                                        state = 0;
                                    }
                                    break;
                            }
                        }
                        break;
                    case 1:
                        {
                            //post value
                            //may follow by some operator
                            //or end
                            switch (currentToken.TokenName)
                            {

                                default:
                                    {
                                        throw new NotSupportedException();
                                    }
                                case TokenName.NewLine:
                                case TokenName.LineComment:
                                    {
                                        return;
                                    }
                                case TokenName.Keyword:
                                    {
                                        //some key word are mysql keyword 
                                        _tkstream.BackOneStep();
                                        return;
                                    }
                                case TokenName.SemiColon:
                                    _tkstream.BackOneStep();
                                    return;
                                case TokenName.Comma:
                                    _tkstream.BackOneStep();
                                    return;
                                case TokenName.OpenParen:
                                    //func call
                                    {
                                        var funcCall = new FunctionCallExpression();
                                        MergeExpression(funcCall);
                                        state = 0;
                                    //switch to func arg list
                                    AGAIN:
                                        {
                                            var subParser = new MySqlExpressionParser();
                                            subParser.Parse(tkstream);
                                            tkstream.Read();
                                            Expression arg = subParser.GetResultExpression();
                                            if (arg != null)
                                            {
                                                funcCall.ArgList.AddExpression(arg);
                                            }
                                        }
                                        if (_tkstream.CurrentToken.TokenName == TokenName.Comma)
                                        {
                                            goto AGAIN;
                                        }
                                        else if (_tkstream.CurrentToken.TokenName == TokenName.CloseParen)
                                        {
                                            //post value, return to state1
                                            state = 1;
                                        }
                                    }
                                    break;
                                case TokenName.CloseBrace:
                                case TokenName.CloseParen:
                                case TokenName.CloseBracket:
                                    _tkstream.BackOneStep();
                                    return;
                                case TokenName.Dot:
                                    //member access expression
                                    if (Expect("*"))
                                    {
                                        //special mysql field list form
                                        //t1.* 
                                        var mbAccessExpr = new MemberAccessExpression();
                                        mbAccessExpr.Member = new IdenExpression(new Token("*", TokenName.StarIden, CurrentToken.Location));
                                        MergeExpression(mbAccessExpr);
                                        state = 0;
                                        return;//***
                                    }
                                    else
                                    {
                                        var mbAccessExpr = new MemberAccessExpression();
                                        MergeExpression(mbAccessExpr);
                                        state = 0;
                                    }
                                    break;
                                case TokenName.MySql_Not:
                                    {
                                        //not like
                                        //not in
                                        if (Expect("like"))
                                        {
                                            var binOpExpr = new BinaryOpExpression();
                                            Token not_like = new Token("not like", TokenName.NotLike, CurrentToken.Location);
                                            binOpExpr.Op = new ExprOperator(not_like);
                                            MergeExpression(binOpExpr);
                                            state = 0;
                                        }
                                        else if (Expect("in"))
                                        {
                                            var binOpExpr = new BinaryOpExpression();
                                            Token not_in = new Token("not in", TokenName.NotIn, CurrentToken.Location);
                                            binOpExpr.Op = new ExprOperator(not_in);
                                            MergeExpression(binOpExpr);
                                            state = 0;
                                        }
                                        else
                                        {
                                            throw new NotSupportedException();
                                        }
                                        //not between ... and ...
                                        //TODO: impl between
                                    }
                                    break;
                                case TokenName.Multiply:
                                case TokenName.Div:
                                case TokenName.Mod:
                                case TokenName.Plus:
                                case TokenName.Minus:
                                case TokenName.Caret:
                                case TokenName.MySqL_OR:
                                case TokenName.MySql_EQ:
                                case TokenName.MySqL_AND:
                                case TokenName.MySql_DIV:
                                case TokenName.MySql_Is:
                                case TokenName.MySql_In:
                                case TokenName.MySql_Like:
                                case TokenName.Lesser:
                                case TokenName.LesserOrEqual:
                                case TokenName.Greater:
                                case TokenName.GreaterOrEqual:
                                case TokenName.NotEqual1:
                                case TokenName.NotEqual2:
                                case TokenName.Assign: //
                                case TokenName.And:
                                case TokenName.Or:
                                case TokenName.CondAnd:
                                case TokenName.CondOr:
                                    {
                                        var binOpExpr = new BinaryOpExpression();
                                        binOpExpr.Op = new ExprOperator(currentToken);
                                        MergeExpression(binOpExpr);
                                        state = 0;
                                    }
                                    break;
                                case TokenName.Colon:
                                    {
                                        var keyValueExpression = new KeyValueExpression();
                                        MergeExpression(keyValueExpression);
                                        state = 0;//wait for value
                                    }
                                    break;

                            }
                        }
                        break;

                }



            }

        }
        void ParseExpression()
        {


        //expression
        AGAIN:
            if (ExpectIden())
            {
                if (_expressions.Count > 1)
                {
                    //apply latest iden to the latest expression
                    IdenExpression latestExpr = (IdenExpression)_expressions.Pop();
                    Expression prevExpr = _expressions.Pop();
                    //apply
                    if (prevExpr is MemberAccessExpression)
                    {
                        MemberAccessExpression mbAccessExpr = (MemberAccessExpression)prevExpr;
                        mbAccessExpr.Member = latestExpr;
                        _expressions.Push(mbAccessExpr);
                        goto AGAIN;
                    }
                    else if (prevExpr is BinaryOpExpression)
                    {
                        BinaryOpExpression binOpExpr = (BinaryOpExpression)prevExpr;
                        binOpExpr.Right = latestExpr;
                        _expressions.Push(binOpExpr);
                        goto AGAIN;
                    }
                    else
                    {

                    }
                }

                if (ExpectPunc(TokenName.Dot))
                {
                    //member access expression
                    MemberAccessExpression mbAccessExpr = new MemberAccessExpression();
                    Expression latestExpr = _expressions.Pop();
                    mbAccessExpr.Target = latestExpr;
                    _expressions.Push(mbAccessExpr);
                    goto AGAIN;
                }
                else if (ExpectPuncs(s_binaryOps))
                {
                    Expression latestExpr = (Expression)_expressions.Pop();
                    BinaryOpExpression binop_expr = new BinaryOpExpression();
                    binop_expr.Op = new ExprOperator(_tkstream.CurrentToken);
                    binop_expr.Left = latestExpr;
                    _expressions.Push(binop_expr);

                    //operator
                    goto AGAIN;
                }
                else if (ExpectPunc(TokenName.OpenParen))
                {
                //parse args
                AGAIN2:
                    ParseExpression();
                    if (ExpectPunc(TokenName.Comma))
                    {
                        goto AGAIN2;
                    }
                    if (ExpectPunc(TokenName.CloseParen))
                    {

                    }
                }
                else if (ExpectKeywords(new string[] { "and", "or" }))
                {
                    //operator
                    goto AGAIN;
                }
                else
                {

                }
            }
            else if (ExpectLiteralString())
            {

            }
            else if (ExpectLiteralNumber())
            {

            }
            else if (ExpectPunc(TokenName.OpenParen))
            {
            //expression list
            AGAIN2:

                Stack<Expression> tmp = _expressions;
                _expressions = new Stack<Expression>();

                ParseExpression();



                if (ExpectPunc(TokenName.Comma))
                {
                    goto AGAIN2;
                }

                if (ExpectPunc(TokenName.CloseParen))
                {
                    //collect all expression list
                }

                //apply func-call expr to existing 
                FunctionCallExpression funcExpr = new FunctionCallExpression();
                List<Expression> tmpList = new List<Expression>();
                foreach (Expression expr in _expressions)
                {
                    //since it is stack
                    //it iteral from top (latest) to bottom
                    tmpList.Add(expr);
                }
                for (int i = tmpList.Count - 1; i >= 0; --i)
                {
                    funcExpr.ArgList.AddExpression(tmpList[i]);
                }
                //--------------
                _expressions = tmp; //restore

                //find proper insert point
                Expression latest_e2 = _expressions.Pop();
                _expressions.Push(MergeExpression(latest_e2, funcExpr));
            }
            else if (ExpectPuncs(s_unaryOps))
            {
                //merge with prev expression,
                //check 'operator-precedence'
                Expression prevExpr = _expressions.Pop();
            }
            else
            {

            }
        }

        void MergeExpression(Expression expr)
        {
            if (_expressions.Count > 0)
            {
                Expression prev = _expressions.Pop();
                _expressions.Push(MergeExpression(prev, expr));
            }
            else
            {
                _expressions.Push(expr);
            }
        }
        static Expression MergeExpression(Expression e1, Expression e2)
        {
            if (e1 is KeyValueExpression keyValueExpression1)
            {
                if (keyValueExpression1.Key == null)
                {
                    //at this stage we must have key, but without value
                    throw new NotSupportedException();
                }
                else if (keyValueExpression1.Value != null)
                {
                    Expression prevValue = keyValueExpression1.Value;
                    prevValue.RemoveSelf();
                    keyValueExpression1.Value = MergeExpression(prevValue, e2);
                    return keyValueExpression1;
                }
                else
                {
                    keyValueExpression1.Value = e2;
                    return keyValueExpression1;
                }
            }
            else if (e2 is KeyValueExpression keyValueExpr2)
            {
                if (keyValueExpr2.Key == null)
                {
                    keyValueExpr2.Key = e1;
                    return keyValueExpr2;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (e1 is BinaryOpExpression binop_expr)
            {

                if (e2.Precedence <= e1.Precedence)
                {
                    //put e2 to 'right-side' of the binop_expr
                    if (binop_expr.Right == null)
                    {
                        binop_expr.Right = e2;
                        return binop_expr;
                    }
                    else
                    {
                        return MergeExpression(binop_expr.Right, e2);
                    }

                }
                else
                {
                    //put e1 to 'right-side' of e2

                    if (e2 is BinaryOpExpression e2_binOp)
                    {
                        if (e2_binOp.Left == null)
                        {
                            if (e1.Owner != null)
                            {
                                e1.Owner.ReplaceChild(e1, e2_binOp);
                            }
                            e2_binOp.Left = e1;
                            return e2_binOp;
                        }
                        else
                        {

                        }
                        //if (e2_binOp.Left == null)
                        //{
                        //    e2_binOp.Left = e1;
                        //    return e2;
                        //}
                        //else
                        //{

                        //}
                    }
                    else
                    {

                    }
                }
            }
            else if (e1 is PrimaryExpression)
            {
                if (e2.Precedence <= e1.Precedence)
                {
                    //add to right side of e1
                    //and return e1  
                    switch (e1)
                    {
                        default:
                            {
                                throw new NotSupportedException();
                            }

                        case IdenExpression idenExpr:
                            {
                                if (e2 is MemberAccessExpression mbAccessExpr2)
                                {
                                    mbAccessExpr2.Target = e1;
                                    return mbAccessExpr2;
                                }
                                else
                                {

                                }
                            }
                            break;
                        case MemberAccessExpression mbAccessExpr:
                            {
                                if (e2 is IdenExpression)
                                {
                                    mbAccessExpr.Member = (IdenExpression)e2;
                                    return mbAccessExpr;
                                }
                                else
                                {
                                }
                            }
                            break;
                        case FunctionCallExpression funcExpr:
                            {

                            }
                            break;
                    }


                    switch (e2)
                    {

                        default:
                            {

                                switch (e1)
                                {
                                    default:
                                        {
                                            throw new NotSupportedException();
                                        }
                                    case MemberAccessExpression mbAcessExpr1:
                                        {

                                        }
                                        break;
                                }


                                throw new NotSupportedException();
                            }
                        case FunctionCallExpression funcExpr:
                            {
                                //remove e1 from its parent 
                                if (e1.Owner != null)
                                {
                                    e1.Owner.ReplaceChild(e1, funcExpr);
                                }

                                funcExpr.Target = e1;
                                return funcExpr;
                            }
                        case MemberAccessExpression mbAccessExpr:
                            {
                                if (e1.Owner != null)
                                {
                                    e1.Owner.ReplaceChild(e1, mbAccessExpr);
                                }
                                mbAccessExpr.Target = e1;
                                return mbAccessExpr;
                            }
                    }
                }
                else
                {
                    if (e2 is BinaryOpExpression binop)
                    {
                        //put to left side
                        //
                        if (binop.Left == null)
                        {
                            if (e1.Owner != null)
                            {
                                e1.Owner.ReplaceChild(e1, binop);
                            }
                            binop.Left = e1;
                            return binop;
                        }
                        else
                        {

                        }
                    }
                    else
                    {

                    }
                }
            }
            else if (e1 is UnaryOpExpression e1_unaryExpr)
            {
                if (e2.Precedence <= e1.Precedence)
                {
                    //add to right side of e1
                    //and return e1
                    if (e1_unaryExpr.Expression == null)
                    {
                        e1_unaryExpr.Expression = e2;
                        return e1;
                    }
                    else
                    {

                    }
                }
                else
                {

                }
            }
            else
            {

            }

            throw new NotSupportedException();
        }
    }

    public class MySqlParser : MySqlParserBase
    {

        Statement _result;
        public MySqlParser()
        {
        }
        public bool EnableExtensions { get; set; } = true;
        public Statement GetResultStatement() => _result;
        public void ParseSql(string sql)
        {
            //1. tokenization => tokenizer
            Lexer lexer = new Lexer();
            lexer.Lex(sql);
            //2. parse
            _tkstream = lexer.ResultTokenStream;

            //3. semantic checking => semantic checker
            //---------
            Parse();
        }
        public void ParseTokenStream(TokenStream tokenStream)
        {
            _tkstream = tokenStream;

            Parse();
        }
        void Parse()
        {
            //parse sql stmt

            var lineComments = new List<Token>();
            StatementCollectionStatement collection = new StatementCollectionStatement();
            while (!_tkstream.IsEnd)
            {
                _tkstream.Read();
                //sql statement start with keyword
                Token currentToken = _tkstream.CurrentToken;
                //parse sql statement
                switch (currentToken.TokenName)
                {
                    case TokenName.SemiColon:
                        //for stop here!
                        collection.Add(_result);
                        if (lineComments.Count > 0)
                        {
                            //copy 
                            _result.LineComments = new List<Token>(lineComments);
                            lineComments.Clear();
                        }

                        _result = null;
                        //_tkstream.MoveToEnd();
                        break;
                    case TokenName.LineComment:
                        lineComments.Add(currentToken);
                        break;
                    case TokenName.Keyword:
                        {
                            switch (currentToken.OriginalText)
                            {
                                case "create":
                                    //parse create syntax
                                    ParseCreate();
                                    break;
                                case "select":
                                    ParseSelect();
                                    break;
                                case "insert":
                                    ParseInsert();
                                    break;
                                case "update":
                                    ParseUpdate();
                                    break;
                                case "delete":
                                    ParseDelete();
                                    break;
                                case "call":
                                    //mysql call store proc
                                    ParseProcCall();
                                    break;
                                case "procedure":
                                    //mysql procedure definition
                                    ParseProcedureDefinition();
                                    break;
                                //this is our extension***
                                case "table":

                                    if (EnableExtensions)
                                    {
                                        //inline table definition
                                        _tkstream.BackOneStep();
                                        ParseCreate();
                                        _result = new AbstractLocalTableStatement(_result as CreateTableStatement);
                                    }
                                    break;
                                case "let":
                                    {
                                        if (EnableExtensions)
                                        {
                                            ParseLet();
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }

            if (collection.Statements.Count > 0)
            {
                if (collection.Statements.Count > 1)
                {
                    _result = collection;
                }
                else// count=1
                {
                    _result = collection.Statements[0];
                }
            }
            if (_result != null && _result.LineComments == null)
            {
                _result.LineComments = lineComments;
            }
        }
        void ParseProcedureDefinition()
        {
            if (ExpectIden()) //proc name
            {
                ProcedureDefinitionStatement procDef = new ProcedureDefinitionStatement();
                procDef.StoreProcName = (IdenExpression)_expressions.Pop();
                //procedure's parameter list, similar to table definition
                ExpectPunc(TokenName.OpenParen);

            PROC_PARAMETERS:
                _expressions.Clear();
                if (ExpectIden())
                {
                    FieldPart fieldPart = new FieldPart();
                    fieldPart.FieldName = ((IdenExpression)_expressions.Pop()).Name;
                    ParseFieldTypeAndDefaultValue(fieldPart);
                    procDef.Parameters.Add(fieldPart);
                }

                if (ExpectPunc(TokenName.Comma))
                {
                    //read next field
                    goto PROC_PARAMETERS;
                }

                //parse field list
                ExpectPunc(TokenName.CloseParen);
                //our extensions!
                //after procedure

                object returnType = null;

                //parse return type (similar to parse table)
                if (ExpectKeyword("table"))
                {
                    //this procedure return a table...   
                    //inline table definition
                    _tkstream.BackOneStep();
                    ParseCreate();
                    //
                    CreateTableStatement cretateTable = _result as CreateTableStatement;
                    //
                    TableDefinition tableType = new TableDefinition(cretateTable.TableName.Name);
                    tableType.CreateTableStmt = cretateTable;
                    //
                    returnType = tableType;
                }
                else
                {
                    throw new NotSupportedException();
                }
                //

                procDef.ReturnType = returnType;
                _result = procDef;//***
            }
        }
        void ParseProcCall()
        {
            if (ExpectIden()) //proc name
            {
                CallProcedureStatement callStoreProcStmt = new CallProcedureStatement();
                callStoreProcStmt.StoreProcName = (IdenExpression)_expressions.Pop();
                ExpectPunc(TokenName.OpenParen);
            //args list, binding parameter or liter

            TRY_AGAIN:
                ParseExpression();
                Expression argExpr = _expressions.Pop();
                if (argExpr != null)
                {
                    callStoreProcStmt.Args.Add(argExpr);
                    if (ExpectPunc(TokenName.Comma))
                    {
                        goto TRY_AGAIN;
                    }
                }

                ExpectPunc(TokenName.CloseParen);

                _result = callStoreProcStmt;//***
            }
        }

        private void ParseLet()
        {
            _expressions.Clear();

            LetStatement letStatement = new LetStatement();

            ExpectIden();
            letStatement.LetName = (IdenExpression)_expressions.Pop();
            ExpectPunc(TokenName.Assign);

        FROM_AGAIN:
            if (ExpectKeyword("from"))
            {
                FromVarInExpression fromVarIn = new FromVarInExpression();
                _expressions.Clear();

                ExpectIden();
                fromVarIn.VarExpression = _expressions.Pop() as IdenExpression;
                //TODO: store name
                if (Expect("in"))
                {
                    _expressions.Clear();
                    ParseExpression();
                    fromVarIn.Expression = _expressions.Pop();
                }
                letStatement.DefinitionExprs.Add(fromVarIn);

                goto FROM_AGAIN;
            }
            else if (ExpectKeyword("let"))
            {
                //parse let clause
                LetExpression letExpr = new LetExpression();
                _expressions.Clear();
                ExpectIden();
                letExpr.VarExpression = _expressions.Pop() as IdenExpression;
                if (ExpectPunc(TokenName.Assign))
                {
                    _expressions.Clear();
                    ParseExpression();
                    letExpr.Expression = _expressions.Pop();
                }
                letStatement.DefinitionExprs.Add(letExpr);

                goto FROM_AGAIN;
            }



            //------------------------------------
            if (ExpectKeyword("select"))
            {
                //we use {} for output as 'object'
                //so no {} it will be select as a table
                if (Expect("{"))
                {
                    List<Expression> selectionList = ParseBraceObject();
                    if (!Expect("}"))
                    {
                        throw new NotSupportedException();
                    }

                    SelectStatement select = new SelectStatement();
                    select.IsObjectSelection = true;
                    select.SelectExpressionList = selectionList;
                    letStatement.SelectStmt = select;
                }
                else
                {
                    ParseSelect();
                    letStatement.SelectStmt = (_result as SelectStatement);
                }
            }

            _result = letStatement;
        }

        List<Expression> ParseBraceObject()
        {
            Stack<Expression> savedExpressions = _expressions;//***save

            //
            _expressions = new Stack<Expression>();

            //brace object
            //begin parse as 'object' output
            ParseCommaSepExpressionList();
            List<Expression> selectionList = new List<Expression>();
            foreach (Expression expr in _expressions)
            {
                selectionList.Insert(0, expr);
            }

            _expressions = savedExpressions;//***restore

            return selectionList;
        }

        static int s_anonymousTableNameCount = 0;
        void ParseCreate()
        {
            //create what?
            if (Expect("table"))
            {
                _expressions.Clear();

                CreateTableStatement createTable = new CreateTableStatement();

                //create table
                if (ExpectIden())
                {
                    //table name
                    createTable.TableName = (IdenExpression)_expressions.Pop();
                }
                else
                {
                    //anonymous table name
                    createTable.TableName = new IdenExpression("anonymous_" + s_anonymousTableNameCount++);
                    createTable.CompilerGeneratedTableName = true;
                }

                ExpectPunc(TokenName.OpenParen);

            //field list
            FIELD_LIST:
                _expressions.Clear();
                if (ExpectIden())
                {
                    FieldPart fieldPart = new FieldPart();
                    IdenExpression expr = ((IdenExpression)_expressions.Pop());
                    fieldPart.FieldName = expr.Name;
                    fieldPart.FieldExpr = expr;
                    ParseFieldTypeAndDefaultValue(fieldPart);
                    createTable.Fields.Add(fieldPart);
                }
                else if (Expect_CaseInsensitive("primary") &&
                         Expect_CaseInsensitive("key"))
                {
                    //primary key				
                    ExpectPunc(TokenName.OpenParen);

                    KeyPart keyPart = new KeyPart();
                    createTable.Keys.Add(keyPart);

                KEY_FILEDS:
                    if (ExpectIden())
                    {
                        IdenExpression indexColumnName = (IdenExpression)_expressions.Pop();
                        keyPart.IndexColumns.Add(indexColumnName.Name);
                        keyPart.IndexKind = "primary";
                        goto KEY_FILEDS;
                    }
                    ExpectPunc(TokenName.CloseParen);
                }

                if (ExpectPunc(TokenName.Comma))
                {
                    //read next field
                    goto FIELD_LIST;
                }

                //parse field list
                ExpectPunc(TokenName.CloseParen);

                //------
                if (ExpectKeyword("engine"))
                {
                    if (ExpectPunc(TokenName.Assign))
                    {
                        if (ExpectIden())
                        {
                            //engine name
                        }
                    }
                }
                if (ExpectKeyword("default") && ExpectKeyword("charset"))
                {
                    if (ExpectPunc(TokenName.Assign))
                    {
                        if (ExpectIden())
                        {
                            //default charset name
                        }
                    }
                }


                _result = createTable;
            }
            else if (Expect("database"))
            {
                //TODO
                throw new NotSupportedException();
            }
        }


        static readonly string[] s_mysqlTypeNames = new string[]
        {
			//TODO: review 
			"tinyint", //1 byte
            "smallint", //2 bytes
            "mediumint", //3 bytes
            "int", //4 bytes
            "bigint", //8 bytes
			//
			"decimal", //fixed-point
            "float","double", //float-point
			//
			"bit",//bit datatype
			//
			"time","datetime","timestamp","year",
			//
            "char","varchar",
            "binary","varbinary",

            "text","longtext",
            "blob","smallblob","mediumblob","longblob",
            "enum",

            //TODO: add support to set, json,

            //------ 
        };
        static readonly string[] s_myExtensionTypes = new string[]
        {
            "int","uint",
            "char","long","ulong",
            "short","ushort",
            "string"

        };
        void ParseFieldTypeAndDefaultValue(FieldPart fieldPart)
        {
            if (EnableExtensions)
            {
                if (!Expect_CaseInsensitive(s_mysqlTypeNames) &&
                    !Expect_CaseInsensitive(s_myExtensionTypes))
                {
                    return;
                }
            }
            else
            {
                if (!Expect_CaseInsensitive(s_mysqlTypeNames))
                {
                    return;
                }
            }
            //-----------------




            //parse field type
            string tkname = _tkstream.CurrentToken.OriginalText;
            bool isEnum = tkname == "enum";
            if (isEnum)
            {
                EnumFieldType enumFieldType = new EnumFieldType();
                fieldPart.FieldType = enumFieldType;
                //parse enum definition
                ExpectPunc(TokenName.OpenParen);

            ENUM_LIST:
                ExpectLiteralString();

                Expression enumMember = _expressions.Pop();

                enumFieldType.EnumMembers.Add(enumMember.ToString());
                if (ExpectPunc(TokenName.Comma))
                {
                    goto ENUM_LIST;
                }
                ExpectPunc(TokenName.CloseParen);
            }
            else
            {
                //
                SimpleFieldType simpleFieldType = new SimpleFieldType(tkname);
                fieldPart.FieldType = simpleFieldType;
                if (ExpectPunc(TokenName.OpenParen))
                {
                    _expressions.Clear();
                    ExpectLiteralNumber();
                    NumberLiteralExpression numLiteral = (NumberLiteralExpression)_expressions.Pop();
                    simpleFieldType.LengthIntegerPart = (int)numLiteral.GetNumberValue();
                    if (ExpectPunc(TokenName.Comma))
                    {
                        //eg. float(10,2)
                        _expressions.Clear();
                        ExpectLiteralNumber();
                        numLiteral = (NumberLiteralExpression)_expressions.Pop();
                        simpleFieldType.LengthDecimalPart = (int)numLiteral.GetNumberValue();
                    }


                    ExpectPunc(TokenName.CloseParen);

                }

            }

            //may follow by detail eg not null
            if (ExpectToken(TokenName.MySql_Not) &&
                ExpectKeyword("null"))
            {
                fieldPart.NotNull = true;
            }
            //
            if (Expect("default"))
            {
                _expressions.Clear();
                ParseExpression();
                fieldPart.DefaultValue = _expressions.Pop();
            }

        }


        void ParseExpression()
        {
            MySqlExpressionParser exprParser = new MySqlExpressionParser();
            exprParser.Parse(_tkstream);
            _expressions.Push(exprParser.GetResultExpression());
        }

        void ParseCommaSepExpressionList()
        {
        //select -> select expression 
        AGAIN1:
            ParseExpression();
            if (ExpectPunc(TokenName.Comma))
            {
                goto AGAIN1;
            }
        }
        void ParseCommaSepNameList()
        {
        //select -> select expression 
        AGAIN1:
            ParseExpression();
            if (ExpectPunc(TokenName.Comma))
            {
                goto AGAIN1;
            }
            else if (ExpectKeyword("as"))
            {
                //latest expression
                if (ExpectIden())
                {
                    IdenExpression idExpr = (IdenExpression)_expressions.Pop();
                    Expression prevExpr = _expressions.Pop();
                    _expressions.Push(new MySqlAliasExpression() { Expression = prevExpr, AsName = idExpr });
                    if (ExpectPunc(TokenName.Comma))
                    {
                        goto AGAIN1;
                    }
                }
            }
        }

        void ParseJoinClause(SelectStatement selectStmt, JoinKind joinKind)
        {
            JoinClause joinClause = new JoinClause();
            joinClause.JoinKind = joinKind;
            selectStmt.JoinClause = joinClause;

            if (ExpectPunc(TokenName.OpenParen))
            {
                ParseCommaSepNameList();
                if (ExpectPunc(TokenName.CloseParen))
                {
                    foreach (Expression expr in _expressions)
                    {
                        joinClause.JoinTableList.Insert(0, expr);
                    }
                }
            }
            else
            {
                ParseCommaSepNameList();
                foreach (Expression expr in _expressions)
                {
                    joinClause.JoinTableList.Insert(0, expr);
                }
            }

            //------------------------------
            _expressions.Clear();
            if (Expect("on"))
            {
                //parse on clause
                ParseExpression();
                joinClause.On = _expressions.Pop();
            }
            else if (Expect("using"))
            {
                if (ExpectPunc(TokenName.OpenParen))
                {
                    ParseCommaSepNameList();
                    if (ExpectPunc(TokenName.CloseParen))
                    {
                        foreach (Expression expr in _expressions)
                        {
                            joinClause.UsingColumnList.Insert(0, expr);
                        }
                    }
                }
            }
        }


        void ParseSelect()
        {
            //select 

            SelectStatement selectStmt = new SelectStatement();
            _result = selectStmt;


            if (ExpectPunc(TokenName.Multiply))
            {
                //select * 
                _expressions.Push(new StarExpression(_tkstream.CurrentToken));
            }
            else
            {
                //select -> select expression 
                ParseCommaSepNameList();
            }
            //--------------
            //collect all expression list

            foreach (Expression expr in _expressions)
            {
                selectStmt.SelectExpressionList.Insert(0, expr);
            }
            //--------------

            _expressions.Clear();
            if (ExpectKeyword("from"))
            {
                //field expression
                //from may contains more than 1 

                List<Expression> fromExprs = new List<Expression>();
                selectStmt.From = fromExprs;
            //
            TRY_AGAIN:
                ParseExpression();
                if (ExpectKeyword("as"))
                {
                    //latest expression
                    if (ExpectIden())
                    {
                        IdenExpression idExpr = (IdenExpression)_expressions.Pop();
                        Expression prevExpr = _expressions.Pop();
                        _expressions.Push(new MySqlAliasExpression() { Expression = prevExpr, AsName = idExpr });
                    }
                }
                fromExprs.Add(_expressions.Pop());
                if (ExpectPunc(TokenName.Comma))
                {
                    goto TRY_AGAIN;
                }
            }

            //--------------
            _expressions.Clear();
            if (Expect("left") && Expect("join"))
            {
                //left join
                ParseJoinClause(selectStmt, JoinKind.LeftJoin);
            }
            if (Expect("right") && Expect("join"))
            {
                ParseJoinClause(selectStmt, JoinKind.RightJoin);
            }
            else if (Expect("inner") && Expect("join"))
            {
                ParseJoinClause(selectStmt, JoinKind.InnerJoin);
            }
            else if (Expect("outer") && Expect("join"))
            {
                ParseJoinClause(selectStmt, JoinKind.OuterJoin);
            }
            else if (Expect("join"))
            {
                ParseJoinClause(selectStmt, JoinKind.Join);
            }
            //--------------
            _expressions.Clear();
            if (ExpectKeyword("where"))
            {
                //where expression
                ParseExpression();
                selectStmt.Where = _expressions.Pop();
            }
            _expressions.Clear();

            if (ExpectKeyword("group") && ExpectKeyword("by"))
            {
                //column list
                ParseCommaSepExpressionList();

                foreach (Expression expr in _expressions)
                {
                    selectStmt.GroupByList.Insert(0, expr);
                }
            }

            //--------------
            _expressions.Clear();
            if (ExpectKeyword("having"))
            {
                //having condition
                //similar to where
                ParseExpression();
                selectStmt.Having = _expressions.Pop();
            }
            //--------------
            _expressions.Clear();
            //parse order by clause
            if (ExpectKeyword("order") && ExpectKeyword("by"))
            {
                //column list
                selectStmt.OrderByClause = ParseOrderByClause();

            }

            //--------------
            _expressions.Clear();
            if (Expect("limit"))
            {
                ParseExpression();
                Expression expr1 = _expressions.Pop();
                //
                if (ExpectPunc(TokenName.Comma))
                {
                    _expressions.Clear();
                    ParseExpression();
                    Expression expr2 = _expressions.Pop();

                    selectStmt.LimitOffsetStartAt = expr1;
                    selectStmt.LimitCount = expr2;
                }
                else
                {
                    selectStmt.LimitCount = expr1;
                }
            }
        }

        void ParseUpdate()
        {
            //update table-name

            UpdateStatement updateStmt = new UpdateStatement();

            _expressions.Clear();
            ParseExpression();
            //we should receive data as func-call expr, it is similar             
            updateStmt.TableName = _expressions.Pop();

            if (ExpectKeyword("set"))
            {

            AGAIN:

                _expressions.Clear();
                //we should get assignment expr
                ExpectIden();

                var assignmentExpr = new AssignmentExpression();
                assignmentExpr.Left = (IdenExpression)_expressions.Pop();
                if (ExpectPunc(TokenName.Assign))
                {
                    _expressions.Clear();
                    ParseExpression();
                    assignmentExpr.Right = _expressions.Pop();
                }

                updateStmt.AssignmentList.Add(assignmentExpr);

                if (ExpectPunc(TokenName.Comma))
                {
                    goto AGAIN;
                }
            }

            //
            if (ExpectKeyword("where"))
            {
                _expressions.Clear();
                ParseExpression();
                updateStmt.Where = _expressions.Pop();
            }

            //-------------------
            _expressions.Clear();
            if (ExpectKeyword("order") && ExpectKeyword("by"))
            {
                //column list
                updateStmt.OrderByClause = ParseOrderByClause();
            }
            _expressions.Clear();
            if (Expect("limit"))
            {
                ParseExpression();
                updateStmt.LimitCount = _expressions.Pop();
            }

            _result = updateStmt;

            //-------------------------------------
            //These are our 'Insert Extensions'
            //(use reconstruction technique)
            if (updateStmt.AssignmentList.Count > 0)
            {

                List<Expression> newAssignmentExprList = new List<Expression>();
                foreach (BinaryOpExpression assignment in updateStmt.AssignmentList)
                {
                    if (assignment.Left is IdenExpression && assignment.Right == null)
                    {
                        IdenExpression iden = (IdenExpression)assignment.Left;
                        if (!iden.Name.StartsWith("?"))
                        {
                            throw new NotSupportedException();
                        }
                        else
                        {
                            AssignmentExpression newAssignmentExpr = new AssignmentExpression();
                            newAssignmentExpr.Left = new IdenExpression(iden.Name.Substring(1));
                            newAssignmentExpr.Right = new IdenExpression(iden.Name);
                            newAssignmentExprList.Add(newAssignmentExpr);
                        }
                    }
                    else
                    {
                        newAssignmentExprList.Add(assignment);
                    }
                }
                updateStmt.AssignmentList = newAssignmentExprList;
            }
        }
        void ParseDelete()
        {
            DeleteStatement deleteStmt = new DeleteStatement();
            if (Expect("from"))
            {
                ParseExpression();
                deleteStmt.TableName = _expressions.Pop();

                if (ExpectKeyword("where"))
                {
                    _expressions.Clear();
                    ParseExpression();
                    deleteStmt.Where = _expressions.Pop(); //must be careful here!!!
                }
                if (Expect("order") && Expect("by"))
                {
                    deleteStmt.OrderBy = ParseOrderByClause();
                }
                if (Expect("limit"))
                {
                    _expressions.Clear();
                    ParseExpression();
                    deleteStmt.LimitCount = _expressions.Pop();
                }

                _result = deleteStmt;
            }

        }

        OrderByClause ParseOrderByClause()
        {
            _expressions.Clear();
            ParseCommaSepExpressionList();
            OrderByClause orderByClause = new OrderByClause();
            foreach (Expression expr in _expressions)
            {
                orderByClause.OrderByList.Insert(0, expr);
            }
            //
            if (Expect("desc"))
            {
                orderByClause.OrderByDirection = OrderByDirection.Desc;
            }
            else if (Expect("asc"))
            {
                orderByClause.OrderByDirection = OrderByDirection.Asc;
            }
            return orderByClause;
        }



        void ParseInsert()
        {
            InsertStatement insertStmt = new InsertStatement();
            if (Expect("into"))
            {
                ParseExpression();

                Expression table = _expressions.Pop();
                switch (table)
                {
                    case FunctionCallExpression tbl_with_cols:
                        {
                            //we receive data as func-call expr, it is similar
                            insertStmt.TableName = tbl_with_cols.Target;
                            foreach (Expression expr in tbl_with_cols.ArgList.GetArgIter())
                            {
                                insertStmt.ColumnList.Add(expr);
                            }
                        }
                        break;
                    case IdenExpression tbl_as_iden:
                        {
                            //no column list
                            //only table name
                            insertStmt.TableName = tbl_as_iden;
                        }
                        break;
                }
            }
            if (ExpectKeyword("values"))
            {
                _expressions.Clear();
                if (ExpectPunc(TokenName.OpenParen))
                {
                    //field list 
                    ParseCommaSepExpressionList();
                    foreach (Expression expr in _expressions)
                    {
                        insertStmt.ValuesList.Insert(0, expr);
                    }
                }
            }
            else if (ExpectKeyword("select"))
            {
                //insert into x select ...
                ParseSelect();
                insertStmt.SelectStmt = (SelectStatement)_result;
            }
            else if (ExpectKeyword("set"))
            {
            //assignment list (similar to update statement) 
            AGAIN:
                _expressions.Clear();
                //we should get assignment expr
                ExpectIden();
                var assignmentExpr = new AssignmentExpression();
                assignmentExpr.Left = (IdenExpression)_expressions.Pop();
                if (ExpectPunc(TokenName.Assign))
                {
                    _expressions.Clear();
                    ParseExpression();
                    assignmentExpr.Right = _expressions.Pop();
                }

                insertStmt.AssignmentList.Add(assignmentExpr);

                if (ExpectPunc(TokenName.Comma))
                {
                    goto AGAIN;
                }
            }

            //-----------
            if (ExpectKeyword("on") &&
                Expect("duplicate") &&
                Expect("key") &&
                ExpectKeyword("update"))
            {
            //after on-duplicate-key-up followed by assignment list
            AGAIN:
                _expressions.Clear();
                //we should get assignment expr
                ExpectIden();
                var assignmentExpr = new AssignmentExpression();
                assignmentExpr.Left = (IdenExpression)_expressions.Pop();
                if (ExpectPunc(TokenName.Assign))
                {
                    _expressions.Clear();
                    ParseExpression();
                    assignmentExpr.Right = _expressions.Pop();
                }

                insertStmt.OnDuplicatKeyUpdateAssignmentList.Add(assignmentExpr);

                if (ExpectPunc(TokenName.Comma))
                {
                    goto AGAIN;
                }
            }

            _result = insertStmt;


            //-------------------------------------
            //These are our 'Insert Extensions'
            //(use reconstruction technique)
            if (insertStmt.ColumnList.Count > 0 &&
                insertStmt.ValuesList.Count == 0 &&
                insertStmt.AssignmentList.Count == 0)
            {
                //eg. insert into city(?name,population=1);
                //will be translated into...
                //insert into city(name,population) values(?name,1);

                List<Expression> newColNameList = new List<Expression>();
                List<Expression> newValueList = new List<Expression>();

                foreach (Expression colName in insertStmt.ColumnList)
                {
                    switch (colName)
                    {
                        default: throw new NotSupportedException();
                        case IdenExpression iden:
                            {
                                if (!iden.Name.StartsWith("?"))
                                {
                                    //syntax error
                                    throw new NotSupportedException();
                                }
                                else
                                {
                                    //create new iden
                                    IdenExpression newColName = new IdenExpression(iden.Name.Substring(1));
                                    IdenExpression valueExpr = new IdenExpression("?" + iden.Name.Substring(1));

                                    newColNameList.Add(newColName);
                                    newValueList.Add(valueExpr);
                                }
                            }
                            break;
                        case BinaryOpExpression binOpAssignment:
                            {
                                if (binOpAssignment.Op.TokenName == TokenName.Assign)
                                {
                                    newColNameList.Add(binOpAssignment.Left);
                                    newValueList.Add(binOpAssignment.Right);
                                }
                                else
                                {
                                    throw new NotSupportedException();
                                }
                            }
                            break;
                    }
                }
                insertStmt.ColumnList = newColNameList;
                insertStmt.ValuesList = newValueList;
            }
        }
    }
}