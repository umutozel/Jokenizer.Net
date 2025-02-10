using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Jokenizer.Net;

public class Settings {
    private static readonly Lazy<Settings> _default = new();
    public static Settings Default => _default.Value;

    private readonly ConcurrentDictionary<string, object?> _knowns = new();
    public IEnumerable<string> KnownIdentifiers => _knowns.Keys;

    private readonly ConcurrentDictionary<char, UnaryExpressionConverter> _unary = new();
    public IEnumerable<char> UnaryOperators => _unary.Keys;

    private readonly ConcurrentDictionary<string, BinaryOperatorInfo> _binary = new();
    public IEnumerable<string> BinaryOperators => _binary
                                                  .OrderBy(b => b.Value.Precedence)
                                                  .Select(b => b.Key);

    public Settings() {
        AddKnownValue("null", null);
        AddKnownValue("true", true);
        AddKnownValue("false", false);

        AddUnaryOperator('-', ExpressionType.Negate);
        AddUnaryOperator('+', ExpressionType.UnaryPlus);
        AddUnaryOperator('!', ExpressionType.Not);
        AddUnaryOperator('~', ExpressionType.OnesComplement);

        AddBinaryOperator("&&", ExpressionType.And, 0);
        AddBinaryOperator("||", ExpressionType.OrElse, 0);
        AddBinaryOperator("??", ExpressionType.Coalesce, 0);
        AddBinaryOperator("|", ExpressionType.Or, 1);
        AddBinaryOperator("^", ExpressionType.ExclusiveOr, 1);
        AddBinaryOperator("&", ExpressionType.And, 1);
        AddBinaryOperator("==", ExpressionType.Equal, 2);
        AddBinaryOperator("!=", ExpressionType.NotEqual, 2);
        AddBinaryOperator("<=", ExpressionType.LessThanOrEqual, 3);
        AddBinaryOperator(">=", ExpressionType.GreaterThanOrEqual, 3);
        AddBinaryOperator("<", ExpressionType.LessThan, 3);
        AddBinaryOperator(">", ExpressionType.GreaterThan, 3);
        AddBinaryOperator("<<", ExpressionType.LeftShift, 4);
        AddBinaryOperator(">>", ExpressionType.RightShift, 4);
        AddBinaryOperator("+", ExpressionType.Add, 5);
        AddBinaryOperator("-", ExpressionType.Subtract, 5);
        AddBinaryOperator("*", ExpressionType.Multiply, 6);
        AddBinaryOperator("/", ExpressionType.Divide, 6);
        AddBinaryOperator("%", ExpressionType.Modulo, 6);
    }

    public Settings AddKnownValue(string identifier, object? value) {
        _knowns.AddOrUpdate(identifier, _ => value, (_, _) => value);
        return this;
    }

    public bool ContainsKnown(string identifier) => _knowns.ContainsKey(identifier);

    public bool TryGetKnownValue(string identifier, out object? value) => _knowns.TryGetValue(identifier, out value);

    public Settings AddUnaryOperator(char op, ExpressionType type) =>
        AddUnaryOperator(op, DefaultUnaryExpressionConverter(type));

    public Settings AddUnaryOperator(char op, UnaryExpressionConverter converter) {
        _unary.AddOrUpdate(op, converter, (_, _) => converter);
        return this;
    }

    public bool ContainsUnary(char op) => _unary.ContainsKey(op);

    public bool TryGetUnaryConverter(char op, out UnaryExpressionConverter converter) =>
        _unary.TryGetValue(op, out converter);

    public Settings AddBinaryOperator(string op, ExpressionType type, byte precedence = 7) =>
        AddBinaryOperator(op, DefaultBinaryExpressionConverter(type), precedence);

    public Settings AddBinaryOperator(string op, BinaryExpressionConverter converter, byte precedence = 7) {
        var info = new BinaryOperatorInfo(precedence, converter);
        _binary.AddOrUpdate(op, _ => info, (_, _) => info);
        return this;
    }

    public bool ContainsBinary(string op) => _binary.ContainsKey(op);

    public bool TryGetBinaryInfo(string op, out BinaryOperatorInfo info) => _binary.TryGetValue(op, out info);

    private static UnaryExpressionConverter DefaultUnaryExpressionConverter(ExpressionType type) =>
        exp => Expression.MakeUnary(type, exp, null);

    private static BinaryExpressionConverter DefaultBinaryExpressionConverter(ExpressionType type) =>
        (left, right) => {
            FixTypes(ref left, ref right);
            return Expression.MakeBinary(type, left, right);
        };

    private static void FixTypes(ref Expression left, ref Expression right) {
        if (left.Type == right.Type) return;

        FixNullable(left, ref right);
        FixNullable(right, ref left);

        var ok =
            TryFixNumeric(left, ref right) ||
            TryFixNumeric(right, ref left) ||
            TryFixForGuid(left, ref right) ||
            TryFixForGuid(right, ref left) ||
            TryFixForDateTime(left, ref right) ||
            TryFixForDateTime(right, ref left);

        if (!ok) {
            // let CLR throw exception if types are not compatible
            right = Expression.Convert(right, left.Type);
        }
    }

    private static void FixNullable(Expression e1, ref Expression e2) {
        if (e1 is ConstantExpression { Value: null } ||
            (e1.Type.IsConstructedGenericType
             && e1.Type.GetGenericTypeDefinition() == typeof(Nullable<>)
             && e1.Type.GetGenericArguments()[0] == e2.Type)) {
            e2 = Expression.Convert(e2, e1.Type);
        }
    }

    private static bool TryFixNumeric(Expression left, ref Expression right) {
        var t1 = left.Type;
        if (t1 != typeof(float) && t1 != typeof(double) && t1 != typeof(decimal)) return false;

        right = Expression.Convert(right, t1);
        return true;
    }

    private static bool TryFixForGuid(Expression e1, ref Expression e2) {
        if ((e1.Type != typeof(Guid?) && e1.Type != typeof(Guid)) || e2.Type != typeof(string) || e2 is not ConstantExpression ce2)
            return false;

        var guidValue = Guid.Parse(ce2.Value.ToString());
        Guid? nullableGuidValue = guidValue;
        e2 = e1.Type == typeof(Guid?)
            ? Expression.Constant(nullableGuidValue, typeof(Guid?))
            : Expression.Constant(guidValue, typeof(Guid));

        return true;
    }

    private static bool TryFixForDateTime(Expression e1, ref Expression e2) {
        if ((e1.Type != typeof(DateTime?) && e1.Type != typeof(DateTime)) || e2.Type != typeof(string) || !(e2 is ConstantExpression ce2))
            return false;

        var dateValue = DateTime.Parse(ce2.Value.ToString());
        DateTime? nullableDateValue = dateValue;
        e2 = e1.Type == typeof(DateTime?)
            ? Expression.Constant(nullableDateValue, typeof(DateTime?))
            : Expression.Constant(dateValue, typeof(DateTime));

        return true;
    }

    private static bool IsFloatingPoint(Type type) =>
        type == typeof(float) || type == typeof(double) || type == typeof(decimal);
}

public delegate Expression UnaryExpressionConverter(Expression exp);

public delegate Expression BinaryExpressionConverter(Expression left, Expression right);