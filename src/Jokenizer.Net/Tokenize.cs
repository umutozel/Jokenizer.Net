using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Jokenizer.Net {

    public class Tokenize {
        private readonly string exp;
        private readonly int len;
        private int idx = 0;

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

        private Tokenize(string exp) {
            if (exp == null)
                throw new ArgumentNullException(nameof(exp));
            if (string.IsNullOrWhiteSpace(exp))
                throw new ArgumentException(nameof(exp));

            this.exp = exp;
            this.len = exp.Length;
        }

        public static Expression From(string exp) {
            return new Tokenize(exp).GetExpression();
        }

        public static Expression From<T>(string exp) where T : Expression {
            return (T) From(exp);
        }

        private char Ch => exp[idx];

        Expression GetExpression() {
            Skip();

            Expression e = TryLiteral() 
                ?? TryVariable()
                ?? TryUnary()
                ?? TryGroup()
                ?? TryObject()
                ?? TryArray();

            if (e == null) return e;

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

            ConstantExpression TryNumber() {
                return null;
            }

            ConstantExpression TryString() {
                return null;
            }

            return TryNumber() ?? TryString();
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

        bool Get(string s) {
            if (Eq(exp, idx, s)) {
                Move(s.Length);
                return true;
            }

            return false;
        }

        bool Move(int count = 1) {
            idx += count;
            return idx < len;
        }

        (char ch, bool done) Nxt() {
            return Move() ? (Ch, false) : ('\0', true);
        }

        void Skip() {
            while (Char.IsWhiteSpace(Ch) && Move());
        }

        void To(string c) {
            Skip();

            if (!Eq(exp, idx, c))
                throw new Exception($"Expected {c} at index {idx}, found {exp[idx]}");

            Move(c.Length);
        }

        static bool Eq(string source, int idx, string target) {
            return source.Substring(idx, target.Length) == target;
        }

        bool IsVariableStart(char c) {
            return (c == 95) || // `_`
                (c >= 65 && c <= 90) || // A...Z
                (c >= 97 && c <= 122); // a...z
        }

        bool StillVariable(char c) {
            return IsVariableStart(c) || Char.IsNumber(c);
        }

        Expression FixPrecedence(Expression left, string leftOp, BinaryExpression right) {
            var p1 = Tokenize.binary[leftOp];
            var p2 = Tokenize.binary.First(b => b.Value.Type == right.NodeType).Value;

            return p2.Precedence < p1.Precedence ?
                Expression.MakeBinary(p2.Type, Expression.MakeBinary(p1.Type, left, right.Left), right.Right) :
                Expression.MakeBinary(p1.Type, left, right);
        }
    }
}
