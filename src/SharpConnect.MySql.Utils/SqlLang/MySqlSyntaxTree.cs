//MIT, 2019-present, Brezza92, EngineKit
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpConnect.MySql.SqlLang
{
    public class LangException : Exception
    {
        public LangException(string msg) : base(msg) { }
    }
    public abstract class CodeObject
    {
        CodeObject _owner;
        int _ownerAssignedRole;
#if DEBUG
        public CodeObject()
        {
        }
#endif
        public void SetOwnerExpression(CodeObject owner, int ownerAssignedRole)
        {
            if (owner != null && _owner != null)
            {
                throw new NotSupportedException();
            }
            _owner = owner;
            _ownerAssignedRole = ownerAssignedRole;
        }
        public void ClearOwner()
        {
            SetOwnerExpression(null, 0);
        }
        public void RemoveSelf()
        {
            if (_owner != null)
            {
                _owner.RemoveChild(this);
                _owner = null;
            }
        }
        internal int OwnerAssignRole => _ownerAssignedRole;
        public CodeObject Owner => _owner;
        public abstract void RemoveChild(CodeObject expr);
        public abstract void ReplaceChild(CodeObject old, CodeObject newCodeObject);
        public abstract void WriteTo(CodeStringBuilder stbuilder);

        public object SemanticType { get; set; }
#if DEBUG
        static int s_dbugTotalId;
        public readonly int dbugId = s_dbugTotalId++;
        public override string ToString()
        {
            CodeStringBuilder codeStrBuilder = new CodeStringBuilder();
            WriteTo(codeStrBuilder);
            return codeStrBuilder.GetStringContent();
        }
#endif
    }
    public abstract class CodeVisitor
    {
    }
    public class CodeStringBuilder : CodeVisitor
    {
        StringBuilder _stbuilder = new StringBuilder();
        public string GetStringContent() => _stbuilder.ToString();
        public void Clear() => _stbuilder.Length = 0;

        public void Append(string str) => _stbuilder.Append(str);
        public void Append(char c) => _stbuilder.Append(c);
        public void AppendLine(string str) => _stbuilder.AppendLine(str);
        public void Append(int number) => _stbuilder.Append(number);
        public void Append(double number) => _stbuilder.Append(number);
    }



    public abstract class Expression : CodeObject
    {
        public abstract ExpressionKind ExpressionKind { get; }
        public abstract int Precedence { get; }
        public bool IsComplierGenerated { get; protected set; }

        //------------------
        public virtual Location BeginAt { get; set; }
        public virtual Location EndAt { get; set; }

        internal void SetLocation(Token singleToken)
        {
            BeginAt = singleToken.Location;
            EndAt = BeginAt.GetNewLocation(singleToken.OriginalText.Length);
        }
        internal void SetLocation(Token beginToken, Token endToken)
        {
            BeginAt = beginToken.Location;
            EndAt = endToken.Location.GetNewLocation(endToken.OriginalText.Length);
        }
        internal void SetBeginLocation(Token token)
        {
            BeginAt = token.Location;
        }
        internal void SetEndLocation(Token token)
        {
            EndAt = token.Location.GetNewLocation(token.OriginalText.Length);
        }
    }
    static class CodeOperatorPrecendence
    {
        //see operator-precedence

        //unary
        public const int BANG = 1; // bank ,! , not
        public const int MYSQL_NOT = 1;
        //
        public const int MINUS_UNARY = 2;
        public const int PLUS_UNARY = 2;
        public const int TILDE = 2;//~
        //
        public const int CARET = 3;//^

        //arithmetic, multiplicative
        public const int MULTIPLY = 4;
        public const int DIVIDE = 4; // / DIV
        public const int MODULO = 4; //% , MOD

        //arithmetic, additive
        public const int ADDITIVE = 5;
        public const int SUBTRACTION = 5;

        //shift
        public const int SHIFT_LEFT = 6;
        public const int SHIFT_RIGHT = 6;

        public const int LOGICAL_AND = 7;
        public const int LOGICAL_OR = 8;


        //relation
        public const int GT = 9;
        public const int LT = 9;
        public const int GE = 9;
        public const int LE = 9;

        //comparison
        public const int EQ = 9; //equal
        public const int NE = 9; //not equal
                                 //logical and, or, xor

        //TODO: add IS, LIKE, REGEXP,IN
        //
        public const int BETWEEN = 10;
        public const int CASE = 10;
        public const int WHEN = 10;
        public const int THEN = 10;
        public const int ELSE = 10;



        public const int COND_AND = 12; //&&
        public const int XOR = 13;
        public const int COND_OR = 14; //||

        public const int ASSIGN = 15; //=, :=
        public static int GetUnaryOperatorPrecedence(TokenName tkname)
        {
            switch (tkname)
            {
                default: throw new NotSupportedException();
                case TokenName.Plus: return PLUS_UNARY;
                case TokenName.Minus: return MINUS_UNARY;
                case TokenName.BANG: return BANG;
                case TokenName.Tilde: return TILDE;
                case TokenName.MySql_Not: return MYSQL_NOT;
            }

        }
        public static int GetBinaryOperatorPrecedence(TokenName tkname)
        {
            switch (tkname)
            {
                default: throw new NotSupportedException();
                //only binary operator precedence


                case TokenName.Multiply: return MULTIPLY;
                case TokenName.Div: return DIVIDE;
                case TokenName.MySql_DIV: return DIVIDE;
                case TokenName.Mod: return MODULO;
                case TokenName.MySql_MOD: return MODULO;
                //
                case TokenName.Plus: return ADDITIVE;
                case TokenName.Minus: return SUBTRACTION;
                //
                case TokenName.ShiftLeft: return SHIFT_LEFT;
                case TokenName.ShiftRight: return SHIFT_RIGHT;
                //
                case TokenName.And: return LOGICAL_AND;
                case TokenName.Or: return LOGICAL_OR;

                case TokenName.Greater: return GT;
                case TokenName.Lesser: return LT;
                case TokenName.GreaterOrEqual: return GE;
                case TokenName.LesserOrEqual: return LE;

                //
                case TokenName.NotEqual1:
                case TokenName.NotEqual2:
                case TokenName.NotLike:
                case TokenName.NotIn:
                case TokenName.NotBetween: return NE;
                //
                case TokenName.MySql_EQ:
                case TokenName.Assign:
                case TokenName.MySql_Is:
                case TokenName.MySql_Like:
                case TokenName.MySql_In: return EQ;
                //
                //

                case TokenName.CondAnd: return COND_AND;
                case TokenName.MySqL_AND: return COND_AND;
                case TokenName.MySqL_OR: return COND_OR;
                case TokenName.CondOr: return COND_OR;
            }
        }
    }

    public class ExpressionListExpression : Expression
    {
        public Expression Original { get; private set; }

        public List<Expression> Expressions { get; private set; }

        public override ExpressionKind ExpressionKind => ExpressionKind.List;

        public override int Precedence => Original.Precedence;

        public ExpressionListExpression(Expression original, IEnumerable<Expression> expressions)
        {
            Original = original;
            IsComplierGenerated = true;
            Expressions = new List<Expression>();
            foreach (var expr in expressions)
            {
                Expressions.Add(expr);
            }
        }

        public override void RemoveChild(CodeObject expr)
        {
            throw new NotImplementedException();
        }

        public override void ReplaceChild(CodeObject old, CodeObject newCodeObject)
        {
            throw new NotImplementedException();
        }

        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            int lenght = Expressions.Count;
            for (int i = 0; i < lenght; i++)
            {
                Expression expr = Expressions[i];
                stbuilder.Append(expr.ToString());
                if (i < lenght - 1)
                {
                    stbuilder.Append(", ");
                }
            }
        }
    }

    public class FromVarInExpression : Expression
    {
        public IdenExpression VarExpression { get; set; }
        public Expression Expression { get; set; }

        public override ExpressionKind ExpressionKind => ExpressionKind.FromInExpression;
        public override int Precedence => Expression.Precedence;
        public string VarName => VarExpression.Name;
        public override void RemoveChild(CodeObject expr)
        {
            //NO CHILD
            throw new LangException("no child");
        }

        public override void ReplaceChild(CodeObject old, CodeObject newCodeObject)
        {
            //NO CHILD
            throw new LangException("no child");
        }

        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append("from ");
            stbuilder.Append(VarExpression.ToString());
            stbuilder.Append(" in ");
            stbuilder.Append(Expression.ToString());
        }
    }
    public class LetExpression : Expression
    {
        public IdenExpression VarExpression { get; set; }
        public Expression Expression { get; set; }

        public override ExpressionKind ExpressionKind => ExpressionKind.LetExpression;
        public override int Precedence => Expression.Precedence;
        public string VarName => VarExpression.Name;
        public override void RemoveChild(CodeObject expr)
        {
            //NO CHILD
            throw new LangException("no child");
        }

        public override void ReplaceChild(CodeObject old, CodeObject newCodeObject)
        {
            //NO CHILD
            throw new LangException("no child");
        }

        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append("let ");
            stbuilder.Append(VarExpression.ToString());
            stbuilder.Append(" = ");
            stbuilder.Append(Expression.ToString());
        }
    }




    /// <summary>
    /// identifier expression
    /// </summary>
    public class IdenExpression : PrimaryExpression
    {
        public override ExpressionKind ExpressionKind => ExpressionKind.Iden;
        public Location FirstDefLocation { get; set; }
        private Token _tk;
        public IdenExpression(Token token)
        {
            _tk = token;

            BeginAt = token.Location;
            EndAt = BeginAt.GetNewLocation(_tk.OriginalText.Length);
        }
        public IdenExpression(string iden)
        {
            _tk = new Token(iden, new Location());//NO location
        }
        public void UpdateToken(Token update)
        {
            _tk = update;

            BeginAt = update.Location;
            EndAt = BeginAt.GetNewLocation(_tk.OriginalText.Length);
        }
        public string Name => _tk.OriginalText;
        public override void RemoveChild(CodeObject expr)
        {
            //NO CHILD
            throw new LangException("no child");
        }
        public override void ReplaceChild(CodeObject old, CodeObject newCodeObject)
        {
            //NO CHILD
            throw new LangException("no child");
        }
        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append(_tk.OriginalText);
        }

    }

    public class ParenExpression : PrimaryExpression
    {
        const int PAREN_EXPR = 1;
        Expression _expr;
        public override ExpressionKind ExpressionKind => ExpressionKind.Paren;
        public ParenExpression()
        {
        }
        public Expression Expression
        {
            get => _expr;
            set
            {
                if (value != null && _expr != null)
                {
                    throw new NotSupportedException();
                }
                if (value != null && value.Owner != null)
                {
                    throw new NotSupportedException();
                }
                _expr = value;
                if (value != null)
                {
                    value.SetOwnerExpression(this, PAREN_EXPR);
                }
            }
        }
        public override void RemoveChild(CodeObject expr)
        {
            if (expr.Owner == this && expr.OwnerAssignRole == PAREN_EXPR)
            {
                _expr = null;
            }
        }
        public override void ReplaceChild(CodeObject old, CodeObject newCodeObject)
        {
            if (newCodeObject != null && newCodeObject.Owner != null) throw new NotSupportedException();

            if (old.Owner == this)
            {
                switch (old.OwnerAssignRole)
                {
                    default: throw new NotFiniteNumberException();
                    case PAREN_EXPR:
                        _expr.ClearOwner();
                        _expr = null;
                        Expression = (Expression)newCodeObject;
                        break;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append("(" + Expression.ToString() + ")");
        }

    }
    public abstract class LiteralExpression : PrimaryExpression
    {
        protected readonly Token _tk;
        public LiteralExpression(Token tk) => _tk = tk;

        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append(_tk.ToString());
        }
        public override void RemoveChild(CodeObject expr)
        {
            //NO CHILD
            throw new LangException("no child");
        }
        public override void ReplaceChild(CodeObject old, CodeObject newCodeObject)
        {
            //NO CHILD
            throw new LangException("no child");
        }
    }

    public class StringLiteralExpression : LiteralExpression
    {
        public override ExpressionKind ExpressionKind => ExpressionKind.StringLiteral;
        public StringLiteralExpression(Token token) : base(token)
        {
            SetLocation(token);
        }
        public string GetStringValue() => _tk.OriginalText;
        public string GetStringValueUnQuote()
        {
            string orgValue = GetStringValue();
            if (orgValue.StartsWith("\"") &&
                orgValue.EndsWith("\""))
            {
                return orgValue.Substring(1, orgValue.Length - 2);
            }
            else if (orgValue.StartsWith("'") &&
                orgValue.EndsWith("'"))
            {
                return orgValue.Substring(1, orgValue.Length - 2);
            }
            else
            {
                return orgValue;
            }
        }
    }

    public class NumberLiteralExpression : LiteralExpression
    {
        public override ExpressionKind ExpressionKind => ExpressionKind.NumberLiteral;
        public NumberLiteralExpression(Token token) : base(token)
        {
            SetLocation(token);
        }
        public double GetNumberValue() => double.Parse(_tk.OriginalText);

    }
    public class BooleanLiteralExpression : LiteralExpression
    {
        public override ExpressionKind ExpressionKind => ExpressionKind.BooleanLiteral;
        public BooleanLiteralExpression(Token token) : base(token)
        {
            SetLocation(token);
        }
    }
    public class NullLiteralExpression : LiteralExpression
    {
        public override ExpressionKind ExpressionKind => ExpressionKind.Null;
        public NullLiteralExpression(Token token) : base(token)
        {
            SetLocation(token);
        }
    }


    //-----------------

    public abstract class PrimaryExpression : Expression
    {
        sealed public override int Precedence => 0;
    }

    public class StarExpression : PrimaryExpression
    {
        public override ExpressionKind ExpressionKind => ExpressionKind.StarExpression;
        readonly Token _tk;

        public StarExpression(Token token)
        {
            _tk = token;
            SetLocation(token);
        }

        public bool IsExpanded { get; set; }
        public List<IdenExpression> ExpandedFields { get; set; } = new List<IdenExpression>();

        public override void RemoveChild(CodeObject expr)
        {
            //NO CHILD
            throw new LangException("no child");
        }
        public override void ReplaceChild(CodeObject old, CodeObject newCodeObject)
        {
            //NO CHILD
            throw new LangException("no child");
        }
        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append(_tk.OriginalText);
        }
    }

    public class MemberAccessExpression : PrimaryExpression
    {
        Expression _target;
        IdenExpression _mb;

        const int TARGET = 1;
        const int MEMBER = 2;

        public override ExpressionKind ExpressionKind => ExpressionKind.MemberAccess;
        public Expression Target
        {
            get => _target;
            set
            {
                if (value != null && _target != null) throw new NotSupportedException();
                _target = value;
                value.SetOwnerExpression(this, TARGET);
            }
        }
        public IdenExpression Member
        {
            get => _mb;
            set
            {
                if (value != null && _mb != null) throw new NotSupportedException();
                _mb = value;
                value.SetOwnerExpression(this, MEMBER);
            }
        }

        public override void RemoveChild(CodeObject expr)
        {
            if (expr.Owner == this)
            {
                switch (expr.OwnerAssignRole)
                {
                    default: throw new NotFiniteNumberException();
                    case TARGET:
                        _target = null;
                        break;
                    case MEMBER:
                        _mb = null;
                        break;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public override void ReplaceChild(CodeObject old, CodeObject newCodeObject)
        {
            if (newCodeObject != null && newCodeObject.Owner != null) throw new NotSupportedException();

            if (old.Owner == this)
            {
                switch (old.OwnerAssignRole)
                {
                    default: throw new NotFiniteNumberException();
                    case TARGET:
                        _target.ClearOwner();
                        _target = null;
                        Target = (Expression)newCodeObject;
                        break;
                    case MEMBER:
                        _mb.ClearOwner();
                        _mb = null;
                        Member = (IdenExpression)newCodeObject;
                        break;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            _target.WriteTo(stbuilder);
            stbuilder.Append('.');
            _mb.WriteTo(stbuilder);
        }
    }

    public class ExpressionList : CodeObject
    {
        List<Expression> _exprList = new List<Expression>();
        Expression _ownerExpr;

        const int ARG = 1;
        public ExpressionList(Expression ownerExpression)
        {
            _ownerExpr = ownerExpression;
        }
        public int Count => _exprList.Count;
        public void Clear()
        {
            foreach (Expression expr in _exprList)
            {
                expr.ClearOwner();
            }
            _exprList.Clear();
        }
        public void AddExpression(Expression expr)
        {
            expr.SetOwnerExpression(this, ARG);
            _exprList.Add(expr);
        }
        public override void RemoveChild(CodeObject expr)
        {
            if (expr.Owner == this)
            {
                _exprList.Remove((Expression)expr);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public override void ReplaceChild(CodeObject old, CodeObject newCodeObject)
        {
            if (newCodeObject != null && newCodeObject.Owner != null) throw new NotSupportedException();

            if (old.Owner == this)
            {
                int index = _exprList.IndexOf((Expression)old);
                if (index < 0) throw new NotSupportedException();

                _exprList.RemoveAt(index);
                old.ClearOwner();
                _exprList.Insert(index, (Expression)newCodeObject);
                newCodeObject.SetOwnerExpression(this, ARG);
            }
            else
            {
                throw new NotImplementedException();
            }

        }

        public IEnumerable<Expression> GetArgIter()
        {
            foreach (Expression expr in _exprList)
            {
                yield return expr;
            }
        }
        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            int i = 0;
            foreach (Expression arg in _exprList)
            {
                if (i > 0)
                {
                    stbuilder.Append(',');
                }
                stbuilder.Append(arg.ToString());
                i++;
            }
        }
    }
    public class FunctionCallExpression : PrimaryExpression
    {
        ExpressionList _exprList;
        Expression _target;

        const int ROLE_TARGET = 1;

        public FunctionCallExpression()
        {
            _exprList = new ExpressionList(this);
        }
        public string FuncName
        {
            get
            {
                if (_target is IdenExpression idenExpr)
                {
                    return idenExpr.Name;
                }
                else
                {
                    //TODO: check member access expr
                    throw new NotSupportedException();
                }
            }
        }
        public Expression Target
        {
            get => _target;
            set
            {
                if (value != null && _target != null) throw new NotSupportedException();
                _target = value;
                _target.SetOwnerExpression(this, ROLE_TARGET);
            }
        }
        public override void RemoveChild(CodeObject expr)
        {
            if (expr.Owner == this)
            {
                switch (expr.OwnerAssignRole)
                {
                    default: throw new NotSupportedException();
                    case ROLE_TARGET:
                        _target.ClearOwner();
                        _target = null;
                        break;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public override void ReplaceChild(CodeObject old, CodeObject newCodeObject)
        {
            if (newCodeObject != null && newCodeObject.Owner != null) throw new NotSupportedException();

            if (old.Owner == this)
            {
                switch (old.OwnerAssignRole)
                {
                    default: throw new NotSupportedException();
                    case ROLE_TARGET:
                        _target.ClearOwner();
                        _target = null;
                        Target = (Expression)newCodeObject;
                        break;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public ExpressionList ArgList => _exprList;

        public override ExpressionKind ExpressionKind => ExpressionKind.FunctionCall;
        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            _target.WriteTo(stbuilder);
            stbuilder.Append('(');
            _exprList.WriteTo(stbuilder);
            stbuilder.Append(')');
        }
    }

    public class ExprOperator
    {
        readonly Token _token;
        public ExprOperator(Token token) => _token = token;
        public TokenName TokenName => _token.TokenName;
        public void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append(" " + _token.OriginalText + " ");
        }
#if DEBUG
        public override string ToString()
        {
            return _token.OriginalText;
        }
#endif
    }


    public class UnaryOpExpression : Expression
    {
        Expression _expr;
        ExprOperator _op;
        int _opPrecedence;

        const int EXPR = 1;

        public UnaryOpExpression() { }
        public override ExpressionKind ExpressionKind => ExpressionKind.UnaryOpExpression;

        public override int Precedence => _opPrecedence;
        public Expression Expression
        {
            get => _expr;
            set
            {
                if (value != null && _expr != null)
                {
                    throw new NotSupportedException();
                }
                _expr = value;
                if (value != null)
                {
                    _expr.SetOwnerExpression(this, EXPR);
                }
            }
        }

        public ExprOperator Op
        {
            get => _op;
            set
            {
                if (value != null && _op != null) throw new NotSupportedException();
                _op = value;
                //check binary operator precedence
                _opPrecedence = CodeOperatorPrecendence.GetUnaryOperatorPrecedence(_op.TokenName);
            }
        }

        public override void RemoveChild(CodeObject expr)
        {
            if (expr.Owner == this)
            {
                switch (expr.OwnerAssignRole)
                {
                    default: throw new NotSupportedException();
                    case EXPR: _expr = null; break;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public override void ReplaceChild(CodeObject old, CodeObject newCodeObject)
        {
            if (newCodeObject != null && newCodeObject.Owner != null) throw new NotSupportedException();

            if (old.Owner == this)
            {
                switch (old.OwnerAssignRole)
                {
                    default: throw new NotSupportedException();
                    case EXPR:
                        _expr.ClearOwner();
                        _expr = null;
                        Expression = (Expression)newCodeObject;
                        break;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            Op.WriteTo(stbuilder);
            _expr.WriteTo(stbuilder);
        }
        public override Location EndAt
        {
            get => _expr.EndAt;
            set { }
        }
    }

    public class AssignmentExpression : BinaryOpExpression
    {
        public override int Precedence => CodeOperatorPrecendence.ASSIGN;
        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            Left.WriteTo(stbuilder);
            stbuilder.Append("=");
            Right.WriteTo(stbuilder);
        }
    }

    public class BinaryOpExpression : Expression
    {
        Expression _left;
        Expression _right;
        ExprOperator _op;
        int _opPrecedence;

        const int LEFT_ROLE = 1;
        const int RIGHT_ROLE = 2;

        public override ExpressionKind ExpressionKind => ExpressionKind.BinaryOpExpression;

        public override int Precedence => _opPrecedence;
        public Expression Left
        {
            get => _left;
            set
            {
                if (value != null && _left != null)
                {
                    throw new NotSupportedException();
                }
                _left = value;
                if (value != null)
                {
                    _left.SetOwnerExpression(this, LEFT_ROLE);
                }
            }
        }
        public ExprOperator Op
        {
            get => _op;
            set
            {
                if (value != null && _op != null) throw new NotSupportedException();
                _op = value;
                //check binary operator precedence
                _opPrecedence = CodeOperatorPrecendence.GetBinaryOperatorPrecedence(_op.TokenName);
            }
        }
        public Expression Right
        {
            get => _right;
            set
            {
                if (value != null && _right != null)
                {
                    throw new NotSupportedException();
                }
                _right = value;
                if (value != null)
                {
                    _right.SetOwnerExpression(this, RIGHT_ROLE);
                }
            }
        }
        public override void RemoveChild(CodeObject expr)
        {
            if (expr.Owner == this)
            {
                switch (expr.OwnerAssignRole)
                {
                    default: throw new NotSupportedException();
                    case RIGHT_ROLE: _right = null; break;
                    case LEFT_ROLE: _left = null; break;
                }

            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public override void ReplaceChild(CodeObject old, CodeObject newCodeObject)
        {
            if (newCodeObject != null && newCodeObject.Owner != null) throw new NotSupportedException();

            if (old.Owner == this)
            {
                switch (old.OwnerAssignRole)
                {
                    default: throw new NotSupportedException();
                    case LEFT_ROLE:
                        _left.ClearOwner();
                        _left = null;
                        Left = (Expression)newCodeObject;
                        break;
                    case RIGHT_ROLE:
                        _right.ClearOwner();
                        _right = null;
                        Right = (Expression)newCodeObject;
                        break;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            Left.WriteTo(stbuilder);
            Op.WriteTo(stbuilder);
            Right.WriteTo(stbuilder);
        }


        public override Location BeginAt
        {
            get => _left.BeginAt;
            set { }//nothing
        }
        public override Location EndAt
        {
            get => _right.EndAt;
            set { }//nothing
        }
    }


    public class MySqlAliasExpression : PrimaryExpression
    {
        const int EXPR = 1;
        const int AS_NAME = 2;
        Expression _expr;
        IdenExpression _asNameExpr;
        public override ExpressionKind ExpressionKind => ExpressionKind.MySqlAliasExpression;
        public Expression Expression
        {
            get => _expr;
            set
            {
                if (value != null && _expr != null)
                {
                    throw new NotSupportedException();
                }
                if (value != null && value.Owner != null)
                {
                    throw new NotSupportedException();
                }
                _expr = value;
                if (value != null)
                {
                    value.SetOwnerExpression(this, EXPR);
                }
            }
        }

        public IdenExpression AsName
        {
            get => _asNameExpr;
            set
            {
                if (value != null && _asNameExpr != null)
                {
                    throw new NotSupportedException();
                }
                if (value != null && value.Owner != null)
                {
                    throw new NotSupportedException();
                }
                _asNameExpr = value;
                if (value != null)
                {
                    value.SetOwnerExpression(this, AS_NAME);
                }
            }

        }
        public override void RemoveChild(CodeObject expr)
        {
            if (expr.Owner == this)
            {
                switch (expr.OwnerAssignRole)
                {
                    default: throw new NotSupportedException();
                    case EXPR:
                        _expr = null;
                        break;
                    case AS_NAME:
                        _asNameExpr = (IdenExpression)expr;
                        break;
                }
            }
        }
        public override void ReplaceChild(CodeObject old, CodeObject newCodeObject)
        {
            if (newCodeObject != null && newCodeObject.Owner != null) throw new NotSupportedException();

            if (old.Owner == this)
            {
                switch (old.OwnerAssignRole)
                {
                    default: throw new NotFiniteNumberException();
                    case EXPR:
                        _expr.ClearOwner();
                        _expr = null;
                        Expression = (Expression)newCodeObject;
                        break;
                    case AS_NAME:
                        _expr.ClearOwner();
                        _asNameExpr = null;
                        AsName = (IdenExpression)newCodeObject;
                        break;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            _expr.WriteTo(stbuilder);
            stbuilder.Append(" as ");
            _asNameExpr.WriteTo(stbuilder);
        }
        public override Location BeginAt
        {
            get => _expr.BeginAt;
            set { }//nothing
        }
        public override Location EndAt
        {
            get => _asNameExpr.EndAt;
            set { }//nothing
        }
#if DEBUG
        public override string ToString()
        {
            return Expression.ToString() + " as " + _asNameExpr;
        }
#endif
    }

    public class BracketArrayExpression : Expression
    {
        //our 'object selection extension'
        public List<Expression> Elements = new List<Expression>();
        public BracketArrayExpression() { }
        public override ExpressionKind ExpressionKind => ExpressionKind.BracketArrayExpression;
        public void AddMember(Expression expr) => Elements.Add(expr);
        public override int Precedence => 0;

        public override void RemoveChild(CodeObject expr)
        {
            throw new NotImplementedException();
        }

        public override void ReplaceChild(CodeObject old, CodeObject newCodeObject)
        {
            throw new NotImplementedException();
        }

        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append("[");
            bool hasFirstElem = false;
            foreach (Expression elem in Elements)
            {
                if (hasFirstElem) stbuilder.Append(',');
                elem.WriteTo(stbuilder);
                hasFirstElem = true;
            }
            stbuilder.Append("]");
        }

    }

    public class BraceObjectExpression : Expression
    {
        //our 'object selection extension'
        public List<KeyValueExpression> Members = new List<KeyValueExpression>();


        public BraceObjectExpression() { }
        public override ExpressionKind ExpressionKind => ExpressionKind.BraceObjectExpression;

        public override int Precedence => 0;

        public void AddMember(KeyValueExpression expr)
        {
            Members.Add(expr);
        }

        public override void RemoveChild(CodeObject expr)
        {
            throw new NotImplementedException();
        }

        public override void ReplaceChild(CodeObject old, CodeObject newCodeObject)
        {
            throw new NotImplementedException();
        }

        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append("{");
            bool hasFirstElem = false;
            foreach (KeyValueExpression keyValueMb in Members)
            {
                if (hasFirstElem) stbuilder.Append(',');
                keyValueMb.WriteTo(stbuilder);
                hasFirstElem = true;
            }
            stbuilder.Append("}");
        }
    }
    public class KeyValueExpression : Expression
    {
        //our 'object selection extension'
        //similar to sql as
        const int KEY_EXPR = 1;
        const int VALUE_EXPR = 2;

        Expression _keyExpr;
        Expression _valueExpr;

        public override ExpressionKind ExpressionKind => ExpressionKind.KeyValueExpression;
        public Expression Key
        {
            get => _keyExpr;
            set
            {
                if (value != null && _keyExpr != null)
                {
                    throw new NotSupportedException();
                }
                if (value != null && value.Owner != null)
                {
                    throw new NotSupportedException();
                }
                _keyExpr = value;
                if (value != null)
                {
                    value.SetOwnerExpression(this, KEY_EXPR);
                }
            }
        }
        public Expression Value
        {
            get => _valueExpr;
            set
            {
                if (value != null && _valueExpr != null)
                {
                    throw new NotSupportedException();
                }
                if (value != null && value.Owner != null)
                {
                    throw new NotSupportedException();
                }
                _valueExpr = value;
                if (value != null)
                {
                    value.SetOwnerExpression(this, VALUE_EXPR);
                }
            }

        }

        public string KeyName
        {
            get
            {
                if (_keyExpr != null)
                {
                    if (_keyExpr is IdenExpression iden)
                    {
                        return iden.Name;
                    }
                    else if (_keyExpr is StringLiteralExpression str)
                    {
                        return str.GetStringValueUnQuote();
                    }
                }
                return "";//TODO:...
            }
        }
        public override int Precedence => 0;

        public override void RemoveChild(CodeObject expr)
        {
            if (expr.Owner == this)
            {
                switch (expr.OwnerAssignRole)
                {
                    default: throw new NotSupportedException();
                    case KEY_EXPR: _keyExpr = null; break;
                    case VALUE_EXPR: _valueExpr = null; break;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override void ReplaceChild(CodeObject old, CodeObject newCodeObject)
        {
            if (newCodeObject != null && newCodeObject.Owner != null) throw new NotSupportedException();

            if (old.Owner == this)
            {
                switch (old.OwnerAssignRole)
                {
                    default: throw new NotFiniteNumberException();
                    case KEY_EXPR:
                        _keyExpr.ClearOwner();
                        _keyExpr = null;
                        Key = (Expression)newCodeObject;
                        break;
                    case VALUE_EXPR:
                        _valueExpr.ClearOwner();
                        _valueExpr = null;
                        Value = (IdenExpression)newCodeObject;
                        break;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            _keyExpr.WriteTo(stbuilder);
            stbuilder.Append(" : ");
            _valueExpr.WriteTo(stbuilder);
        }
        public override Location BeginAt
        {
            get => _keyExpr.BeginAt;
            set { }//nothing
        }
        public override Location EndAt
        {
            get => _valueExpr.EndAt;
            set { }//nothing
        }
#if DEBUG
        public override string ToString()
        {
            return _keyExpr.ToString() + " : " + _valueExpr.ToString();
        }
#endif
    }


    //========================================================= 
    public abstract class Statement
    {
        public abstract StatementKind StatementKind { get; }
        public abstract void WriteTo(CodeStringBuilder stbuilder);
        public List<Token> LineComments { get; set; }
#if DEBUG
        public override string ToString()
        {
            CodeStringBuilder stbuilder = new CodeStringBuilder();
            WriteTo(stbuilder);
            return stbuilder.GetStringContent();
        }
#endif
    }

    public enum JoinKind
    {
        Join,
        InnerJoin,
        OuterJoin,
        LeftJoin,
        RightJoin
    }
    public class JoinClause
    {
        public JoinKind JoinKind { get; set; }
        public List<Expression> JoinTableList = new List<Expression>();
        public List<Expression> UsingColumnList = new List<Expression>();
        public Expression On { get; set; }

        public void WriteTo(CodeStringBuilder stbuilder)
        {


            switch (JoinKind)
            {
                default: throw new NotSupportedException();
                case JoinKind.LeftJoin: stbuilder.Append(" left join "); break;
                case JoinKind.RightJoin: stbuilder.Append(" right join "); break;
                case JoinKind.InnerJoin: stbuilder.Append(" inner join "); break;
                case JoinKind.OuterJoin: stbuilder.Append(" outer join "); break;
                case JoinKind.Join: stbuilder.Append(" join "); break;
            }
            if (JoinTableList.Count > 1)
            {
                int i = 0;
                stbuilder.Append('(');
                foreach (Expression expr in JoinTableList)
                {
                    if (i > 0) stbuilder.Append(',');
                    stbuilder.Append(JoinTableList[i].ToString());
                    i++;
                }
                stbuilder.Append(')');
            }
            else if (JoinTableList.Count == 1)
            {
                stbuilder.Append(JoinTableList[0].ToString());
            }

            if (On != null)
            {
                stbuilder.Append(" on ");
                stbuilder.Append(On.ToString());
            }

            if (UsingColumnList != null && UsingColumnList.Count > 0)
            {
                stbuilder.Append(" using( ");
                int i = 0;
                foreach (Expression expr in UsingColumnList)
                {
                    if (i > 0) stbuilder.Append(',');
                    stbuilder.Append(UsingColumnList[i].ToString());
                    i++;
                }
                stbuilder.Append(")");
            }
        }

        public override string ToString()
        {
            CodeStringBuilder stBuilder = new CodeStringBuilder();
            WriteTo(stBuilder);
            return stBuilder.GetStringContent();
        }
    }

    public class SelectStatement : Statement
    {
        public List<Expression> SelectExpressionList = new List<Expression>();
        public List<Expression> From { get; set; }
        public Expression Where { get; set; }
        public List<Expression> GroupByList = new List<Expression>();
        public Expression Having { get; set; }
        public OrderByClause OrderByClause { get; set; }
        public JoinClause JoinClause { get; set; }
        public override StatementKind StatementKind => StatementKind.Select;

        public Expression LimitOffsetStartAt { get; set; }
        public Expression LimitCount { get; set; }

        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append("select ");
            {
                int i = 0;
                foreach (Expression expr in SelectExpressionList)
                {
                    if (i > 0) { stbuilder.Append(','); }
                    stbuilder.Append(expr.ToString());
                    i++;
                }
            }
            if (From != null)
            {
                stbuilder.Append(" from ");
                int i = 0;
                foreach (Expression expr in From)
                {
                    if (i > 0) { stbuilder.Append(','); }
                    stbuilder.Append(expr.ToString());
                    i++;
                }
            }

            if (JoinClause != null)
            {
                stbuilder.Append(JoinClause.ToString());
            }

            if (Where != null)
            {
                stbuilder.Append(" where ");
                Where.WriteTo(stbuilder);
            }

            if (GroupByList != null && GroupByList.Count > 0)
            {
                stbuilder.Append(" group by ");
                int i = 0;
                foreach (Expression expr in GroupByList)
                {
                    if (i > 0) { stbuilder.Append(','); }
                    expr.WriteTo(stbuilder);
                    i++;
                }
            }
            if (Having != null)
            {
                stbuilder.Append(" having ");
                Having.WriteTo(stbuilder);
            }


            if (LimitCount != null)
            {
                stbuilder.Append(" limit ");
                if (LimitOffsetStartAt != null)
                {
                    LimitOffsetStartAt.WriteTo(stbuilder);
                    stbuilder.Append(',');
                }
                LimitCount.WriteTo(stbuilder);
            }
        }

        /// <summary>
        /// our extensions
        /// </summary>
        public bool IsObjectSelection { get; set; }
    }

    public class InsertStatement : Statement
    {
        public override StatementKind StatementKind => StatementKind.Insert;
        public Expression TableName { get; set; }
        public List<Expression> ColumnList { get; set; } = new List<Expression>();
        public List<Expression> ValuesList { get; set; } = new List<Expression>();
        public List<Expression> AssignmentList { get; set; } = new List<Expression>();
        public List<Expression> OnDuplicatKeyUpdateAssignmentList { get; set; } = new List<Expression>();
        public SelectStatement SelectStmt { get; set; }
        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append("insert into ");
            TableName.WriteTo(stbuilder);

            if (ColumnList.Count > 0)
            {
                stbuilder.Append('(');
                int i = 0;
                foreach (Expression expr in ColumnList)
                {
                    if (i > 0) stbuilder.Append(',');
                    expr.WriteTo(stbuilder);
                    i++;
                }
                stbuilder.Append(')');
            }
            //
            if (AssignmentList.Count > 0)
            {
                int i = 0;
                foreach (Expression expr in AssignmentList)
                {
                    if (i > 0) stbuilder.Append(',');
                    expr.WriteTo(stbuilder);
                    i++;
                }
            }
            //
            if (ValuesList.Count > 0)
            {
                stbuilder.Append(" values (");
                int i = 0;
                foreach (Expression expr in ValuesList)
                {
                    if (i > 0) stbuilder.Append(',');
                    expr.WriteTo(stbuilder);
                    i++;
                }

                stbuilder.Append(')');
            }
            if (SelectStmt != null)
            {
                stbuilder.Append(' ');
                stbuilder.Append(SelectStmt.ToString());
            }

            if (OnDuplicatKeyUpdateAssignmentList.Count > 0)
            {
                stbuilder.AppendLine(" on duplicate key update ");
                int i = 0;
                foreach (Expression expr in OnDuplicatKeyUpdateAssignmentList)
                {
                    if (i > 0) stbuilder.Append(',');
                    expr.WriteTo(stbuilder);
                    i++;
                }
            }
        }
    }

    public enum OrderByDirection
    {
        Default,
        Asc,
        Desc,
    }

    public class OrderByClause
    {
        public List<Expression> OrderByList = new List<Expression>();
        public OrderByDirection OrderByDirection { get; set; }
        public void WriteTo(CodeStringBuilder stbuilder)
        {

            if (OrderByList != null && OrderByList.Count > 0)
            {
                stbuilder.Append(" order by ");
                int i = 0;
                foreach (Expression expr in OrderByList)
                {
                    if (i > 0) { stbuilder.Append(','); }
                    expr.WriteTo(stbuilder);
                    i++;
                }
                switch (OrderByDirection)
                {
                    default: throw new NotSupportedException();
                    case OrderByDirection.Default: break;
                    case OrderByDirection.Asc: stbuilder.Append(" asc "); break;
                    case OrderByDirection.Desc: stbuilder.Append(" desc "); break;
                }
            }
        }
    }
    public class DeleteStatement : Statement
    {
        public override StatementKind StatementKind => StatementKind.Delete;
        public Expression TableName { get; set; }
        public Expression Where { get; set; }
        public OrderByClause OrderBy { get; set; }
        public Expression LimitCount { get; set; }
        public override void WriteTo(CodeStringBuilder stbuilder)
        {

            stbuilder.Append("delete from ");
            TableName.WriteTo(stbuilder);
            if (Where != null)
            {
                stbuilder.Append(" where ");
                Where.WriteTo(stbuilder);
            }
            if (OrderBy != null)
            {
                OrderBy.WriteTo(stbuilder);
            }
            if (LimitCount != null)
            {
                stbuilder.Append(" limit ");
                LimitCount.WriteTo(stbuilder);
            }
        }

    }
    public class UpdateStatement : Statement
    {
        public Expression TableName { get; set; }
        public List<Expression> AssignmentList { get; set; } = new List<Expression>();
        public Expression Where { get; set; }
        public OrderByClause OrderByClause { get; set; }
        public Expression LimitCount { get; set; }
        public override StatementKind StatementKind => StatementKind.Update;

        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append("update ");
            TableName.WriteTo(stbuilder);
            stbuilder.Append(" set ");
            if (AssignmentList.Count > 0)
            {
                int i = 0;
                foreach (Expression expr in AssignmentList)
                {
                    if (i > 0) stbuilder.Append(',');
                    expr.WriteTo(stbuilder);
                    i++;
                }
            }
            if (Where != null)
            {
                stbuilder.Append(" where ");
                Where.WriteTo(stbuilder);
            }

            if (OrderByClause != null)
            {
                OrderByClause.WriteTo(stbuilder);
            }

            if (LimitCount != null)
            {
                stbuilder.Append(" limit ");
                LimitCount.WriteTo(stbuilder);
            }

        }
    }

    public class TableDefinition
    {
        public TableDefinition(string tableName)
        {
            Name = tableName;
        }
        public string Name { get; set; }
        public CreateTableStatement CreateTableStmt { get; set; }
    }

    public class CreateTableStatement : Statement
    {
        public bool CompilerGeneratedTableName { get; set; }
        public IdenExpression TableName { get; set; }
        public List<FieldPart> Fields { get; set; } = new List<FieldPart>();
        public List<KeyPart> Keys { get; set; } = new List<KeyPart>();
        public override StatementKind StatementKind => StatementKind.CreateTable;

        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append("create table ");
            TableName.WriteTo(stbuilder);

            stbuilder.Append('(');
            {
                int i = 0;
                foreach (FieldPart fieldPart in Fields)
                {
                    if (i > 0) stbuilder.Append(',');
                    //
                    fieldPart.WriteTo(stbuilder);
                    i++;
                }
            }
            stbuilder.Append(')');
            //
            if (Keys.Count > 0)
            {
                //write each key
                int i = 0;
                foreach (KeyPart keypart in Keys)
                {
                    if (i > 0) stbuilder.Append(',');
                    //                    
                    keypart.WriteTo(stbuilder);
                    i++;
                }
            }
        }
    }

    public class AbstractLocalTableStatement : Statement
    {
        public CreateTableStatement CreateTableInfo { get; }

        public override StatementKind StatementKind => StatementKind.AbstractTable;

        public AbstractLocalTableStatement(CreateTableStatement createTable)
        {
            CreateTableInfo = createTable;
        }

        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            CreateTableInfo.WriteTo(stbuilder);
        }
    }

    public class BindingParameter
    {
        public BindingParameter(string name)
        {
            Name = name;
        }
        public string Name { get; private set; }
        public object SemanticType { get; set; }
        public int Index { get; internal set; }
    }
    public class BindingParameterCollection
    {
        Dictionary<string, BindingParameter> _dic = new Dictionary<string, BindingParameter>();
        List<BindingParameter> _list = new List<BindingParameter>();
        public void Add(BindingParameter par)
        {
            par.Index = _list.Count;
            _list.Add(par);
            _dic.Add(par.Name, par);
        }
        public BindingParameter this[int index] => _list[index];
        public BindingParameter this[string parName] => _dic[parName];
        public int Count => _list.Count;
    }

    public class LetStatement : Statement
    {
        public IdenExpression LetName { get; set; }

        public List<Expression> DefinitionExprs { get; set; } = new List<Expression>();

        public SelectStatement SelectStmt { get; set; }

        public BindingParameterCollection BindingParameters { get; set; } = new BindingParameterCollection();

        public object BraceObjectType { get; set; }

        public LetStatement()
        {
        }
        public string VarName => LetName.Name;

        public override StatementKind StatementKind => StatementKind.Let;

        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append("let " + LetName + " = ");
            foreach (Expression expr in DefinitionExprs)
            {
                stbuilder.AppendLine(expr.ToString());
            }
            if (SelectStmt.From != null)
            {
                stbuilder.Append(SelectStmt.ToString());
            }
            else
            {
                stbuilder.Append("select { ");
                int count = SelectStmt.SelectExpressionList.Count;
                for (int i = 0; i < count; i++)
                {
                    Expression expr = SelectStmt.SelectExpressionList[i];
                    stbuilder.Append(expr.ToString());
                    if (i < count - 1)
                        stbuilder.Append(", ");
                }
                stbuilder.Append(" }");
            }
        }
    }

    public class StatementCollectionStatement : Statement
    {
        public List<Statement> Statements = new List<Statement>();

        public override StatementKind StatementKind => StatementKind.Collection;// throw new NotImplementedException();

        public void Add(Statement statement)
        {
            Statements.Add(statement);
        }

        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            throw new NotImplementedException();
        }
    }

    public enum StatementKind
    {
        Unknown,
        Select,
        Insert,
        Update,
        CreateTable,
        Delete,
        AbstractTable,
        CallProcedure,
        ProcedureDefinition,
        Collection,
        Let,
    }
    public enum ExpressionKind
    {
        Unknown,
        StringLiteral,
        NumberLiteral,
        Null,
        BooleanLiteral,//true false
        Iden,
        MemberAccess,
        StarExpression,
        FunctionCall,//method invoke        
        UnaryOpExpression,

        BinaryOpExpression,
        AssignmentExpression,
        Paren,

        MySqlAliasExpression,//eg. select a as m

        //Extension
        FromInExpression,
        LetExpression,
        KeyValueExpression,
        BraceObjectExpression,
        BracketArrayExpression,
        List,
    }



    public class ProcedureDefinitionStatement : Statement
    {
        public override StatementKind StatementKind => StatementKind.ProcedureDefinition;
        public IdenExpression StoreProcName { get; set; }
        public List<FieldPart> Parameters { get; set; } = new List<FieldPart>();
        public object ReturnType { get; set; }//in this version, return type as table
        public override void WriteTo(CodeStringBuilder stbuilder)
        {

            stbuilder.Append("procedure ");
            stbuilder.Append(StoreProcName.Name);
            stbuilder.Append("(");

            bool hasSomeArgs = false;
            foreach (FieldPart par in Parameters)
            {
                if (hasSomeArgs) { stbuilder.Append(','); }
                par.WriteTo(stbuilder);
                hasSomeArgs = true;
            }
            stbuilder.Append(")");
        }
    }

    public class CallProcedureStatement : Statement
    {
        public override StatementKind StatementKind => StatementKind.CallProcedure;
        public IdenExpression StoreProcName { get; set; }
        public List<Expression> Args { get; set; } = new List<Expression>();
        public object MapToProcedure { get; set; }
        public override void WriteTo(CodeStringBuilder stbuilder)
        {
            stbuilder.Append("call ");
            stbuilder.Append(StoreProcName.Name);
            stbuilder.Append("(");

            bool hasSomeArgs = false;
            foreach (Expression arg in Args)
            {
                if (hasSomeArgs) { stbuilder.Append(','); }
                arg.WriteTo(stbuilder);
                hasSomeArgs = true;
            }

            stbuilder.Append(")");
        }
    }
}