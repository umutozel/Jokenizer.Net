using System.Collections.Concurrent;
using System.Linq.Expressions;
using Jokenizer.Net.Tokens;

namespace Jokenizer.Net {

    public static class Operators {
        private static ConcurrentDictionary<char, UnaryExpressionConverter> _unary;
        private static ConcurrentDictionary<string, BinaryOperatorInfo> _binary;

        static Operators() {
            _unary = new ConcurrentDictionary<char, UnaryExpressionConverter>();
            AddUnaryOperator('-', ExpressionType.Negate);
            AddUnaryOperator('+', ExpressionType.UnaryPlus);
            AddUnaryOperator('!', ExpressionType.Not);
            AddUnaryOperator('~', ExpressionType.OnesComplement);
            
            _binary = new ConcurrentDictionary<string, BinaryOperatorInfo>();
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

        public static void AddUnaryOperator(char op, ExpressionType type) {
            AddUnaryOperator(op, DefaultUnaryExpressionConverter(type));
        }

        public static void AddUnaryOperator(char op, UnaryExpressionConverter converter) {
            _unary.AddOrUpdate(op, converter, (o, c) => converter);
        }

        public static bool TryGetUnaryConverter(char op, out UnaryExpressionConverter converter) => _unary.TryGetValue(op, out converter);

        public static void AddBinaryOperator(string op, byte precedence, ExpressionType type) {
            AddBinaryOperator(op, precedence, DefaultBinaryExpressionConverter(type));
        }

        public static void AddBinaryOperator(string op, byte precedence, BinaryExpressionConverter converter) {
            var info = new BinaryOperatorInfo(precedence, converter);
            _binary.AddOrUpdate(op, o => info, (o, i) => info);
        }

        public static bool TryGetBinaryInfo(string op, out BinaryOperatorInfo info) => _binary.TryGetValue(op, out info);

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
