using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Jokenizer.Net {
    using Tokens;

    public class Settings {
        private static Lazy<Settings> _default = new Lazy<Settings>();
        public static Settings Default => _default.Value;

        static ConcurrentDictionary<string, object> _knowns = new ConcurrentDictionary<string, object>();
        public IEnumerable<string> KnownIdentifiers => _knowns.Keys;

        private ConcurrentDictionary<char, UnaryExpressionConverter> _unary
            = new ConcurrentDictionary<char, UnaryExpressionConverter>();
        public IEnumerable<char> UnaryExpressions => _unary.Keys;

        private ConcurrentDictionary<string, BinaryOperatorInfo> _binary
            = new ConcurrentDictionary<string, BinaryOperatorInfo>();
        public IEnumerable<string> BinaryExpressions => _binary.Keys;

        public Settings() {
            AddKnownValue("null", null);
            AddKnownValue("true", true);
            AddKnownValue("false", false);
            
            AddUnaryOperator('-', ExpressionType.Negate);
            AddUnaryOperator('+', ExpressionType.UnaryPlus);
            AddUnaryOperator('!', ExpressionType.Not);
            AddUnaryOperator('~', ExpressionType.OnesComplement);

            AddBinaryOperator("&&", 0, ExpressionType.And);
            AddBinaryOperator("||", 0, ExpressionType.OrElse);
            AddBinaryOperator("??", 0, ExpressionType.Coalesce);
            AddBinaryOperator("|", 1, ExpressionType.Or);
            AddBinaryOperator("^", 1, ExpressionType.ExclusiveOr);
            AddBinaryOperator("&", 1, ExpressionType.And);
            AddBinaryOperator("==", 2, ExpressionType.Equal);
            AddBinaryOperator("!=", 2, ExpressionType.NotEqual);
            AddBinaryOperator("<=", 3, ExpressionType.LessThanOrEqual);
            AddBinaryOperator(">=", 3, ExpressionType.GreaterThanOrEqual);
            AddBinaryOperator("<", 3, ExpressionType.LessThan);
            AddBinaryOperator(">", 3, ExpressionType.GreaterThan);
            AddBinaryOperator("<<", 4, ExpressionType.LeftShift);
            AddBinaryOperator(">>", 4, ExpressionType.RightShift);
            AddBinaryOperator("+", 5, ExpressionType.Add);
            AddBinaryOperator("-", 5, ExpressionType.Subtract);
            AddBinaryOperator("*", 6, ExpressionType.Multiply);
            AddBinaryOperator("/", 6, ExpressionType.Divide);
            AddBinaryOperator("%", 6, ExpressionType.Modulo);
        }

        public Settings AddKnownValue(string identifier, object value) {
            _knowns.AddOrUpdate(identifier, i => value, (i, v) => value);
            return this;
        }

        public bool ContainsKnown(string identifier) => _knowns.ContainsKey(identifier);
        
        public bool TryGetKnownValue(string identifier, out object value) => _knowns.TryGetValue(identifier, out value);
        
        public Settings AddUnaryOperator(char op, ExpressionType type)
            => AddUnaryOperator(op, DefaultUnaryExpressionConverter(type));

        public Settings AddUnaryOperator(char op, UnaryExpressionConverter converter) {
            _unary.AddOrUpdate(op, converter, (o, c) => converter);
            return this;
        }

        public bool ContainsUnary(char op) => _unary.ContainsKey(op);

        public bool TryGetUnaryConverter(char op, out UnaryExpressionConverter converter) => _unary.TryGetValue(op, out converter);

        public Settings AddBinaryOperator(string op, byte precedence, ExpressionType type) {
            return AddBinaryOperator(op, precedence, DefaultBinaryExpressionConverter(type));
        }

        public Settings AddBinaryOperator(string op, byte precedence, BinaryExpressionConverter converter) {
            var info = new BinaryOperatorInfo(precedence, converter);
            _binary.AddOrUpdate(op, o => info, (o, i) => info);
            return this;
        }

        public bool ContainsBinary(string op) => _binary.ContainsKey(op);

        public bool TryGetBinaryInfo(string op, out BinaryOperatorInfo info) => _binary.TryGetValue(op, out info);

        private static UnaryExpressionConverter DefaultUnaryExpressionConverter(ExpressionType type) {
            return (Expression exp) => Expression.MakeUnary(type, exp, null);
        }

        private static BinaryExpressionConverter DefaultBinaryExpressionConverter(ExpressionType type) {
            return (Expression left, Expression right) => Expression.MakeBinary(type, left, right);
        }
    }

    public delegate Expression UnaryExpressionConverter(Expression exp);

    public delegate Expression BinaryExpressionConverter(Expression left, Expression right);

    public class BinaryOperatorInfo {

        internal BinaryOperatorInfo(byte precedence, BinaryExpressionConverter expressionConverter) {
            Precedence = precedence;
            ExpressionConverter = expressionConverter;
        }

        public byte Precedence { get; }
        public BinaryExpressionConverter ExpressionConverter { get; }
    }
}
