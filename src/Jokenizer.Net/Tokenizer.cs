using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Jokenizer.Net {

    public class Tokenizer {
        private readonly string exp;
        private readonly int len;
        private int idx = 0;
        private char ch;

        private static char[] unary = { '-', '+', '!', '~' };
        private static Dictionary < string, (int Precedence, ExpressionType Type) > binary = new Dictionary < string, (int, ExpressionType) > { { "&&", (0, ExpressionType.And) },
            { "||", (0, ExpressionType.OrElse) },
            { "??", (0, ExpressionType.Coalesce) },
            { "|", (1, ExpressionType.Or) },
            { "^", (1, ExpressionType.ExclusiveOr) },
            { "&", (1, ExpressionType.And) },
            { "==", (2, ExpressionType.Equal) },
            { "!=", (2, ExpressionType.NotEqual) },
            { "<=", (3, ExpressionType.LessThanOrEqual) },
            { ">=", (3, ExpressionType.GreaterThanOrEqual) },
            { "<", (3, ExpressionType.LessThan) },
            { ">", (3, ExpressionType.GreaterThan) },
            { "<<", (4, ExpressionType.LeftShift) },
            { ">>", (4, ExpressionType.RightShift) },
            { "+", (5, ExpressionType.Add) },
            { "-", (5, ExpressionType.Subtract) },
            { "*", (6, ExpressionType.Multiply) },
            { "/", (6, ExpressionType.Divide) },
            { "%", (6, ExpressionType.Modulo) }
        };
        private static string separator = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        private Tokenizer(string exp) {
            if (exp == null)
                throw new ArgumentNullException(nameof(exp));
            if (string.IsNullOrWhiteSpace(exp))
                throw new ArgumentException(nameof(exp));

            this.exp = exp;
            this.len = exp.Length;
            ch = exp.ElementAt(0);
        }

        public static Expression Parse(string exp) {
            return new Tokenizer(exp).GetExp();
        }

        public static T Parse<T>(string exp) where T : Expression {
            return (T) Parse(exp);
        }

        Expression GetExp() {
            Skip();

            Expression e = TryLiteral() 
                ?? TryVariable()
                ?? TryUnary()
                ?? TryGroup()
                ?? TryObject()
                ?? TryArray();

            if (Done() || e == null) return e;

            Expression r;
            do {
                Skip();

                r = e;
                e = TryMember(e)
                    ?? TryIndexer(e)
                    ?? TryLambda(e)
                    ?? TryCall(e)
                    ?? TryKnown(e)
                    ?? TryTernary(e)
                    ?? TryBinary(e);
            } while (e != null);

            return r;
        }

        Expression TryLiteral() {

            ConstantExpression tryNumber() {
                var n = "";

                void x() {
                    while (Char.IsNumber(ch)) {
                        n += ch;
                        Move();
                    }
                }

                x();
                bool isFloat = false;
                if (Get(separator)) {
                    n += separator;
                    x();
                    isFloat = true;
                }

                if (n != "") {
                    if (IsVariableStart())
                        throw new Exception($"Unexpected character (${ch}) at index ${idx}");

                    var val = isFloat ? float.Parse(n) : int.Parse(n);
                    return Expression.Constant(val, val.GetType());
                }

                return null;            
            }

            Expression tryString() {
                bool inter = false;
                if (ch == '$') {
                    inter = true;
                    Move();
                }
                if (ch != '"') return null;

                var q = ch;
                var es = new List<Expression>();
                var s = "";

                for (char c = Move(); !Done(); c = Move()) {
                    if (c == q) {
                        Move();

                        if (es.Count > 0) {
                            if (s != "") {
                                es.Add(Expression.Constant(s));
                            }

                            return es.Aggregate(
                                (Expression)Expression.Constant(""),
                                (p, n) => Expression.MakeBinary(ExpressionType.Add, p, n)
                            );
                        }

                        return Expression.Constant(s);
                    }

                    if (c == '\\') {
                        c = Move();
                        switch (c) {
                            case 'a': s += '\a'; break;
                            case 'b': s += '\b'; break;
                            case 'f': s += '\f'; break;
                            case 'n': s += '\n'; break;
                            case 'r': s += '\r'; break;
                            case 't': s += '\t'; break;
                            case 'v': s += '\v'; break;
                            case '0': s += '\0'; break;
                            case '\\': s += '\\'; break;
                            case '"': s += '"'; break;
                            default: s += '\\' + c; break;
                        }
                    } 
                    else if (inter && Get("${")) {
                        if (s != "") {
                            es.Add(Expression.Constant(s));
                            s = "";
                        }
                        es.Add(GetExp());

                        Skip();
                        if (ch != '}')
                            throw new Exception($"Unterminated template literal at {idx}");
                    } else {
                        s += c;
                    }
                }

                throw new Exception($"Unclosed quote after {s}");
            }

            return tryNumber() ?? tryString();
        }

        Expression TryVariable() {
            throw new NotImplementedException();
        }

        Expression TryUnary() {
            throw new NotImplementedException();
        }

        Expression TryGroup() {
            throw new NotImplementedException();
        }

        Expression TryObject() {
            throw new NotImplementedException();
        }

        Expression TryArray() {
            throw new NotImplementedException();
        }

        Expression TryBinary(Expression e) {
            throw new NotImplementedException();
        }

        Expression TryMember(Expression e) {
            throw new NotImplementedException();
        }

        Expression TryIndexer(Expression e) {
            throw new NotImplementedException();
        }

        Expression TryLambda(Expression e) {
            throw new NotImplementedException();
        }

        Expression TryCall(Expression e) {
            throw new NotImplementedException();
        }

        Expression TryTernary(Expression e) {
            throw new NotImplementedException();
        }

        Expression TryKnown(Expression e) {
            throw new NotImplementedException();
        }

        bool IsSpace() {
            return Char.IsWhiteSpace(ch);
        }

        bool IsNumber() {
            return char.IsNumber(ch);
        }

        bool IsVariableStart() {
            return (ch == 95) || // `_`
                (ch >= 65 && ch <= 90) || // A...Z
                (ch >= 97 && ch <= 122); // a...z
        }

        bool StillVariable(char c) {
            return IsVariableStart() || Char.IsNumber(ch);
        }

        bool Done() {
            return idx >= len;
        }

        char Move(int count = 1) {
            idx += count;
            var d = Done();
            return ch = d ? '\0' : exp.ElementAt(idx);
        }

        bool Get(string s) {
            if (Eq(idx, s)) {
                Move(s.Length);
                return true;
            }

            return false;
        }

        void Skip() {
            while (IsSpace()) Move();
        }

        bool Eq(int idx, string target) {
            if (idx + target.Length > exp.Length) return false;
            return exp.Substring(idx, target.Length) == target;
        }

        void To(string c) {
            Skip();

            if (!Eq(idx, c))
                throw new Exception($"Expected {c} at index {idx}, found {exp[idx]}");

            Move(c.Length);
        }

        Expression FixPrecedence(Expression left, string leftOp, BinaryExpression right) {
            var p1 = Tokenizer.binary[leftOp];
            var p2 = Tokenizer.binary.First(b => b.Value.Type == right.NodeType).Value;

            return p2.Precedence < p1.Precedence ?
                Expression.MakeBinary(p2.Type, Expression.MakeBinary(p1.Type, left, right.Left), right.Right) :
                Expression.MakeBinary(p1.Type, left, right);
        }
    }
}
