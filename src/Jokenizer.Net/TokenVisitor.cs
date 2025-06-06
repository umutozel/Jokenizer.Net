using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Jokenizer.Net;

using Dynamic;
using Tokens;

public class TokenVisitor {
    private static readonly MethodInfo _concatMethod = typeof(string).GetMethod("Concat", [typeof(string), typeof(string)])!;

    protected readonly Settings Settings;
    protected readonly IDictionary<string, object?> Variables;

    public TokenVisitor(IDictionary<string, object?>? variables, IEnumerable<object?>? values, Settings? settings = null) {
        Variables = variables ?? new Dictionary<string, object?>();
        Settings = settings ?? Settings.Default;

        var i = 0;
        values?.ToList()
            .ForEach(e => {
                var k = $"@{i++}";
                if (!Variables.ContainsKey(k)) {
                    Variables.Add(k, e);
                }
            });
    }

    public virtual LambdaExpression Process(Token token, IEnumerable<Type>? typeParameters, IEnumerable<ParameterExpression>? parameters = null) {
        typeParameters ??= [];
        var prmArray = parameters != null ? parameters.ToArray() : [];

        if (token is LambdaToken lt)
            return VisitLambda(lt, typeParameters, prmArray);

        var prms = typeParameters.Select(Expression.Parameter).ToList();
        var body = Visit(token, prmArray.Concat(prms).ToArray());

        return Expression.Lambda(body, prms);
    }

    protected virtual Expression Visit(Token token, ParameterExpression[] parameters) {
        if (token is GroupToken { Tokens.Length: 1 } gt)
            return Visit(gt.Tokens[0], parameters);

        return token switch {
            BinaryToken bt   => VisitBinary(bt, parameters),
            CallToken ct     => VisitCall(ct, parameters),
            IndexerToken it  => VisitIndexer(it, parameters),
            LiteralToken lit => VisitLiteral(lit, parameters),
            MemberToken mt   => VisitMember(mt, parameters),
            ObjectToken ot   => VisitObject(ot, parameters),
            ArrayToken at    => VisitArray(at, parameters),
            TernaryToken tt  => VisitTernary(tt, parameters),
            UnaryToken ut    => VisitUnary(ut, parameters),
            VariableToken vt => VisitVariable(vt, parameters),
            GroupToken or AssignToken or LambdaToken => throw new InvalidTokenException($"Invalid {token.Type} expression usage"),
            _ => throw new InvalidTokenException($"Unsupported token type {token.Type}")
        };
    }

    protected virtual Expression VisitBinary(BinaryToken token, ParameterExpression[] parameters) {
        var left = Visit(token.Left, parameters);
        var right = Visit(token.Right, parameters);

        if (left.Type == typeof(string) && token.Operator == "+")
            return Expression.Add(left, right, _concatMethod);

        return GetBinary(token.Operator, left, right);
    }

    protected virtual Expression VisitCall(CallToken token, ParameterExpression[] parameters) {
        Expression owner;
        string methodName;

        switch (token.Callee) {
            case MemberToken mt:
                owner = Visit(mt.Owner, parameters);
                methodName = mt.Name;
                break;
            case VariableToken vt when parameters.Count() == 1:
                owner = parameters.First();
                methodName = vt.Name;
                break;
            default:
                throw new InvalidTokenException("Unsupported method call");
        }

        return GetMethodCall(owner, methodName, token.Args, parameters);
    }

    protected virtual Expression VisitIndexer(IndexerToken token, ParameterExpression[] parameters) {
        var owner = Visit(token.Owner, parameters);
        var key = Visit(token.Key, parameters);

        return CreateIndexer(owner, key);
    }

    protected Expression CreateIndexer(Expression owner, Expression key) {
        if (owner.Type.IsArray && key.Type == typeof(int))
            return Expression.ArrayIndex(owner, key);

        PropertyInfo? indexer;
        if (owner.Type == typeof(ExpandoObject)) {
            owner = Expression.Convert(owner, typeof(IDictionary<string, object>));
            indexer = owner.Type.GetProperty("Item");
        } else {
            var defaultMemberAttr = (DefaultMemberAttribute)owner.Type.GetCustomAttribute(typeof(DefaultMemberAttribute));
            indexer = owner.Type.GetProperty(defaultMemberAttr?.MemberName ?? "Item");
        }

        if (indexer == null)
            throw new InvalidTokenException($"Cannot find indexer on type {owner.Type}");

        return Expression.MakeIndex(owner, indexer, [key]);
    }

    protected virtual LambdaExpression VisitLambda(LambdaToken token, IEnumerable<Type> lambdaParameters, ParameterExpression[] parameters) {
        var prms = lambdaParameters.Zip(token.Parameters, Expression.Parameter).ToList();
        var body = Visit(token.Body, parameters.Concat(prms).ToArray());

        return Expression.Lambda(body, prms);
    }

    // ReSharper disable once UnusedParameter.Global
    protected virtual Expression VisitLiteral(LiteralToken token, ParameterExpression[] parameters) {
        return Expression.Constant(token.Value, token.Value != null ? token.Value.GetType() : typeof(object));
    }

    // ReSharper disable once UnusedParameter.Global
    protected virtual Expression VisitMember(MemberToken token, ParameterExpression[] parameters) {
        var owner = Visit(token.Owner, parameters);
        return GetMember(owner, token.Name, parameters);
    }

    // ReSharper disable once UnusedParameter.Global
    protected Expression GetMember(Expression owner, string name, ParameterExpression[] parameters) {
        var prop = Settings.IgnoreMemberCase
            ? owner.Type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            : owner.Type.GetProperty(name);
        if (prop != null)
            return Expression.Property(owner, prop);

        var field = Settings.IgnoreMemberCase
         ? owner.Type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
         : owner.Type.GetField(name);
        return field != null ? Expression.Field(owner, field) : CreateIndexer(owner, Expression.Constant(name));
    }

    protected virtual Expression VisitObject(ObjectToken token, ParameterExpression[] parameters) {
        var props = token.Members.Select(m => new { m.Name, Right = Visit(m.Right, parameters) }).ToArray();
        var type = ClassFactory.Instance.GetDynamicClass(props.Select(p => new DynamicProperty(p.Name, p.Right.Type)));
        var newExp = Expression.New(type.GetConstructors().First());
        var bindings = props
            .Select(p =>
                Expression.Bind(
                    type.GetProperty(p.Name) ?? throw new ArgumentException($"Cannot find property {p.Name}"),
                    p.Right
                )
            );

        return Expression.MemberInit(newExp, bindings);
    }

    protected virtual Expression VisitArray(ArrayToken token, ParameterExpression[] parameters) {
        var expressions = token.Items.Select(i => Visit(i, parameters)).ToList();
        var type = expressions.Any() ? expressions[0].Type : typeof(object);
        return Expression.NewArrayInit(type, expressions);
    }

    protected virtual Expression VisitTernary(TernaryToken token, ParameterExpression[] parameters) {
        return Expression.Condition(Visit(token.Predicate, parameters), Visit(token.WhenTrue, parameters), Visit(token.WhenFalse, parameters));
    }

    protected virtual Expression VisitUnary(UnaryToken token, ParameterExpression[] parameters) {
        return GetUnary(token.Operator, Visit(token.Target, parameters));
    }

    protected virtual Expression VisitVariable(VariableToken token, ParameterExpression[] parameters) {
        var name = token.Name;

        if (Variables.TryGetValue(name, out var value))
            return Expression.Constant(value, value != null ? value.GetType() : typeof(object));

        var prm = parameters.FirstOrDefault(p => p.Name == name);
        if (prm != null)
            return prm;

        switch (name) {
            case "Math":
                return Expression.Parameter(typeof(Math));
            case "DateTime":
                return Expression.Parameter(typeof(DateTime));
        }

        if (parameters.Count() != 1) throw new InvalidTokenException($"Unknown variable {name}");

        var owner = parameters.First();
        return GetMember(owner, name, parameters);
    }

    protected Expression GetBinary(string op, Expression left, Expression right) {
        if (Settings.TryGetBinaryInfo(op, out var bi))
            return bi.ExpressionConverter(left, right);

        throw new InvalidTokenException($"Unknown binary operator {op}");
    }

    protected Expression GetUnary(char op, Expression exp) {
        if (Settings.TryGetUnaryConverter(op, out var uc))
            return uc(exp);

        throw new InvalidTokenException($"Unknown unary operator {op}");
    }

    protected MethodCallExpression GetMethodCall(Expression owner, string methodName, Token[] args, ParameterExpression[] parameters) {
        if (methodName == "GetType")
            throw new InvalidOperationException("GetType cannot be called");

        var hasLambda = false;
        var methodArgs = new Expression?[args.Length];
        for (var i = 0; i < args.Length; i++) {
            var arg = args[i];
            if (arg is LambdaToken) {
                hasLambda = true;
            }
            else {
                methodArgs[i] = Visit(arg, parameters);
            }
        }

        if (!hasLambda) {
            var method = owner.Type.GetMethod(methodName, methodArgs.Select(m => m!.Type).ToArray());
            if (method != null && TryBuildCall(owner, method, method.GetParameters(), methodArgs, args, false, parameters, out var result))
                return result!;
        }

        foreach (var method in owner.Type.GetMethods().Where(m => m.Name == methodName)) {
            if (TryBuildCall(owner, method, method.GetParameters(), methodArgs, args, false, parameters, out var result))
                return result!;
        }

        foreach (var (m, p) in ExtensionMethods.Search(owner.Type, methodName)) {
            if (TryBuildCall(owner, m, p, methodArgs,  args, true, parameters, out var result))
                return result!;
        }

        throw new InvalidTokenException($"Could not find instance or extension method for {methodName} for {owner.Type}");
    }

    private bool TryBuildCall(Expression owner, MethodInfo method, IReadOnlyList<ParameterInfo> prms,
                              Expression?[] args, Token[] tokens, bool isExtension,
                              ParameterExpression[] parameters, out MethodCallExpression? result) {
        result = null;
        try {
            result = BuildCall(owner, method, prms, args, tokens, isExtension, parameters);
            return result != null;
        }
        catch {  // ignored
            return false;
        }
    }

    private MethodCallExpression? BuildCall(Expression owner, MethodInfo method, IReadOnlyList<ParameterInfo> prms,
                                            Expression?[] args, Token[] tokens, bool isExtension,
                                            ParameterExpression[] parameters) {
        var isArgumentCountCorrect = prms.Count == args.Length;
        var isExtraParameterWithDefault = prms.Count > args.Length && prms[args.Length].HasDefaultValue;
        if (!(isArgumentCountCorrect || isExtraParameterWithDefault))
            return null;

        for (var i = 0; i < args.Length; i++) {
            var prm = prms[i];

            if (tokens[i] is LambdaToken lt) {
                if (!typeof(Delegate).IsAssignableFrom(prm.ParameterType))
                    return null;
                var lambdaPrms = prm.ParameterType.GetMethod("Invoke")!.GetParameters();
                args[i] = VisitLambda(lt, lambdaPrms.Select(p => p.ParameterType), parameters);
            }
            else {
                // if token is not lambda, it must have been visited and arg must be generated
                var arg = args[i]!;

                // we also check if the arg is assignable to prm
                if (arg.Type == prm.ParameterType)
                    continue;
                if (!Helper.CanConvert(prm.ParameterType, arg.Type))
                    return null;

                // if it can be assignable but not the same type, we cast it to the target type
                args[i] = Expression.Convert(arg, prm.ParameterType);
            }
        }

        if (isExtraParameterWithDefault) {
            var defaultValues = prms
                .Skip(args.Length)
                .Select(p => Expression.Constant(p.DefaultValue));
            args = args.Concat(defaultValues).ToArray();
        }

        return isExtension
            ? Expression.Call(null, method, new[] { owner }.Concat(args))
            : Expression.Call(method.IsStatic ? null : owner, method, args);
    }
}
