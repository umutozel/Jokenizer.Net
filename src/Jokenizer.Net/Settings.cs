using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Jokenizer.Net;

public class Settings {
    private static readonly Lazy<Settings> _default = new();
    public static Settings Default => _default.Value;

    public bool IgnoreMemberCase { get; set; }
    private readonly ConcurrentDictionary<string, object?> _knowns = new();
    public IEnumerable<string> KnownIdentifiers => _knowns.Keys;

    private readonly ConcurrentDictionary<string, Type> _knownTypes = new();
    public IEnumerable<string> KnownTypeNames => _knownTypes.Keys;

    private readonly ConcurrentDictionary<char, UnaryExpressionConverter> _unary = new();
    public IEnumerable<char> UnaryOperators => _unary.Keys;

    private readonly ConcurrentDictionary<string, BinaryOperatorInfo> _binary = new();
    public IEnumerable<string> BinaryOperators => _binary
                                                  .OrderByDescending(b => b.Key.Length)
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

    /// <summary>
    /// Registers a CLR type whose constructors and static methods become callable
    /// from inside parsed expressions. Built-in: DateTime, DateTimeOffset, TimeSpan
    /// are always available; AddKnownType extends the registry. Once registered,
    /// "TimescaleFunctions.TimeBucket('5 minutes', ts)" parses as a static call,
    /// and "new MyType(...)" parses as a constructor invocation.
    /// </summary>
    public Settings AddKnownType(string name, Type type) {
        _knownTypes.AddOrUpdate(name, _ => type, (_, _) => type);
        return this;
    }

    public bool TryGetKnownType(string name, out Type? type) {
        if (_knownTypes.TryGetValue(name, out var t)) {
            type = t;
            return true;
        }

        type = null;
        return false;
    }

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

    private static readonly MethodInfo _stringCompareMethod =
        typeof(string).GetMethod(nameof(string.Compare), [typeof(string), typeof(string)])!;

    private static BinaryExpressionConverter DefaultBinaryExpressionConverter(ExpressionType type) =>
        (left, right) => {
            FixTypes(ref left, ref right);

            // Ordering comparisons (<, <=, >, >=) are not defined for strings in the
            // expression API — Expression.MakeBinary throws InvalidOperationException.
            // Rewrite to the canonical "string.Compare(a, b) OP 0" form, which is also
            // the shape EF Core recognises and translates to a SQL collation comparison.
            if (IsOrderingComparison(type) && left.Type == typeof(string) && right.Type == typeof(string))
                return Expression.MakeBinary(type,
                    Expression.Call(_stringCompareMethod, left, right),
                    Expression.Constant(0));

            return Expression.MakeBinary(type, left, right);
        };

    private static bool IsOrderingComparison(ExpressionType type) =>
        type is ExpressionType.LessThan or ExpressionType.LessThanOrEqual
             or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual;

    private static void FixTypes(ref Expression left, ref Expression right) {
        if (left.Type == right.Type)
            return;

        FixNullable(left, ref right);
        FixNullable(right, ref left);

        var ok =
            TryFixForGuid(left, ref right) ||
            TryFixForGuid(right, ref left) ||
            TryFixForDateTime(left, ref right) ||
            TryFixForDateTime(right, ref left);

        if (ok)
            return;

        if (Helper.CanConvert(left.Type, right.Type)) {
            right = Expression.Convert(right, left.Type);
        }
        else {
            left = Expression.Convert(left, right.Type);
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

    private static bool TryFixForGuid(Expression e1, ref Expression e2) {
        if ((e1.Type != typeof(Guid?) && e1.Type != typeof(Guid)) || e2.Type != typeof(string) || e2 is not ConstantExpression ce2)
            return false;

        var guidValue = Guid.Parse(ce2.Value.ToString());
        e2 = e1.Type == typeof(Guid?)
            ? Expression.Constant((Guid?)guidValue, typeof(Guid?))
            : Expression.Constant(guidValue, typeof(Guid));

        return true;
    }

    private static bool TryFixForDateTime(Expression e1, ref Expression e2) {
        if ((e1.Type != typeof(DateTime?) && e1.Type != typeof(DateTime)) || e2.Type != typeof(string) || e2 is not ConstantExpression ce2)
            return false;

        var dateStr = ce2.Value.ToString();
        var dateValue = DateTime.Parse(dateStr);
        if (dateStr.EndsWith("Z")) {
            dateValue = dateValue.ToUniversalTime();
        }
        e2 = e1.Type == typeof(DateTime?)
            ? Expression.Constant((DateTime?)dateValue, typeof(DateTime?))
            : Expression.Constant(dateValue, typeof(DateTime));

        return true;
    }
}

public delegate Expression UnaryExpressionConverter(Expression exp);

public delegate Expression BinaryExpressionConverter(Expression left, Expression right);