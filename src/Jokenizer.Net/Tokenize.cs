using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Jokenizer.Net {

    public class Tokenize {
        private readonly string exp;
        private readonly int len;
        private int idx = 0;

        private static char[] unary = { '-', '+', '!', '~' };
        private static Dictionary <string, (int, ExpressionType)> binary = new Dictionary <string, (int, ExpressionType)> { 
            { "&&", (0, ExpressionType.And) },
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

        private Tokenize(string exp) {
            if (string.IsNullOrWhiteSpace(exp))
                throw new ArgumentNullException(nameof(exp));

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
            throw new NotImplementedException();
        }

        Expression TryLiteral() {
            throw new NotImplementedException();
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

        Expression TryBinary() {
            throw new NotImplementedException();
        }

        Expression TryMember() {
            throw new NotImplementedException();
        }

        Expression TryIndexer() {
            throw new NotImplementedException();
        }

        Expression TryLambda() {
            throw new NotImplementedException();
        }

        Expression TryCall() {
            throw new NotImplementedException();
        }

        Expression TryTernary() {
            throw new NotImplementedException();
        }

        Expression TryKnown() {
            throw new NotImplementedException();
        }

        bool Get(string s) {
            if (Eq(exp, idx, s)) {
                Move(s.Length);
                return true;
            }

            return false;
        }

        void Move(int count = 1) {
            idx += count;
        }

        string Nxt() {
            Move();
            return idx < len ? Ch.ToString() : null;
        }

        void Skip() {
            while (IsSpace(Ch)) Move();
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

        bool IsSpace(char ch) {
            return Char.IsWhiteSpace(ch);
        }

        bool IsNumber(char c) {
            return (c >= 48 && c <= 57);
        }

        bool IsVariableStart(char c) {
            return (c == 36) || (c == 95) || // `$`, `_`
                (c >= 65 && c <= 90) || // A...Z
                (c >= 97 && c <= 122); // a...z
        }

        bool stillVariable(char c) {
            return IsVariableStart(c) || IsNumber(c);
        }

        Expression FixPrecedence(Expression left, string leftOp, BinaryExpression right) {
            var p1 = Tokenize.binary[leftOp];
            var p2 = Tokenize.binary.First(b => b.Value.Item2 == right.NodeType).Value;

            return p2.Item1 < p1.Item1
                ? Expression.MakeBinary(p2.Item2, Expression.MakeBinary(p1.Item2, left, right.Left), right.Right)
                : Expression.MakeBinary(p1.Item2, left, right);
        }
    }
}
