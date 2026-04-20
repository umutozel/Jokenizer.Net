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
            case VariableToken vt when TryResolveKnownType(vt.Name, out var knownType): {
                var ctorArgs = token.Args.Select(a => Visit(a, parameters)).ToArray();
                var ctor = knownType!.GetConstructor(ctorArgs.Select(a => a.Type).ToArray());
                if (ctor == null)
                    throw new InvalidTokenException($"Cannot find constructor for {vt.Name} matching the given arguments");
                return Expression.New(ctor, ctorArgs);
            }
            case VariableToken vt when parameters.Count() == 1:
                owner = parameters.First();
                methodName = vt.Name;
                break;
            default:
                throw new InvalidTokenException("Unsupported method call");
        }

        return GetMethodCall(owner, methodName, token.Args, parameters);
    }

    private static bool TryResolveKnownType(string name, out Type? type) {
        type = name switch {
            "DateTime" => typeof(DateTime),
            "DateTimeOffset" => typeof(DateTimeOffset),
            "TimeSpan" => typeof(TimeSpan),
            _ => null
        };
        return type != null;
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
        var flags = BindingFlags.Public | BindingFlags.Instance
                    | (Settings.IgnoreMemberCase ? BindingFlags.IgnoreCase : 0);

        var prop = owner.Type.GetProperty(name, flags);
        if (prop != null)
            return Expression.Property(owner, prop);

        var field = owner.Type.GetField(name, flags);
        if (field != null)
            return Expression.Field(owner, field);

        // Dynamic-LINQ convention: member lookup on Nullable<T> auto-unwraps to T via .Value
        // so "OrderedAt.Year" on DateTime? emits "OrderedAt.Value.Year". C# would reject this
        // but query authors consistently expect the convenience.
        var underlying = Nullable.GetUnderlyingType(owner.Type);
        if (underlying != null) {
            var underlyingProp = underlying.GetProperty(name, flags);
            if (underlyingProp != null)
                return Expression.Property(Expression.Property(owner, "Value"), underlyingProp);

            var underlyingField = underlying.GetField(name, flags);
            if (underlyingField != null)
                return Expression.Field(Expression.Property(owner, "Value"), underlyingField);
        }

        return CreateIndexer(owner, Expression.Constant(name));
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
            if (method != null && TryBuildInstanceCall(owner, method, methodArgs, args, parameters, out var result))
                return result!;
        }

        foreach (var method in owner.Type.GetMethods().Where(m => m.Name == methodName)) {
            if (TryBuildInstanceCall(owner, method, methodArgs, args, parameters, out var result))
                return result!;
        }

        foreach (var candidate in ExtensionMethods.Search(owner.Type, methodName)) {
            if (TryBuildExtensionCall(owner, candidate, methodArgs, args, parameters, out var result))
                return result!;
        }

        // Dynamic-LINQ convention: method lookup on Nullable<T> auto-unwraps to T via .Value
        // so "OrderedAt.ToString(\"yyyy\")" on DateTime? emits "OrderedAt.Value.ToString(\"yyyy\")".
        // Only one unwrap level: after the retry the owner's type is T, so the recursion terminates.
        if (Nullable.GetUnderlyingType(owner.Type) != null) {
            return GetMethodCall(Expression.Property(owner, "Value"), methodName, args, parameters);
        }

        throw new InvalidTokenException($"Could not find instance or extension method for {methodName} for {owner.Type}");
    }

    private bool TryBuildInstanceCall(Expression owner, MethodInfo method, Expression?[] args, Token[] tokens,
                                      ParameterExpression[] parameters, out MethodCallExpression? result) {
        result = null;
        try {
            var prms = method.GetParameters();
            var prmTypes = prms.Select(p => p.ParameterType).ToArray();
            result = BuildCall(owner, method, prms, prmTypes, args, tokens, isExtension: false, parameters);
            return result != null;
        }
        catch {
            return false;
        }
    }

    private bool TryBuildExtensionCall(Expression owner, ExtensionMethods.ExtensionCandidate candidate,
                                       Expression?[] args, Token[] tokens,
                                       ParameterExpression[] parameters, out MethodCallExpression? result) {
        result = null;

        var method = candidate.Method;
        if (!method.IsGenericMethodDefinition) {
            try {
                result = BuildCall(owner, method, candidate.RawParameters, candidate.ParameterTypes,
                                   args, tokens, isExtension: true, parameters);
                return result != null;
            }
            catch (ArgumentException) { return false; }
            catch (InvalidOperationException) { return false; }
        }

        // Generic method still needs full type inference from actual arguments.
        // Only type binding happens here; concrete param-type conversion is deferred to the
        // final loop once all type parameters are resolved via MakeGenericMethod.
        var substitution = new Dictionary<Type, Type>(candidate.Substitution);
        var resolvedArgs = new Expression?[args.Length];

        try {
            for (var i = 0; i < args.Length; i++) {
                if (tokens[i] is LambdaToken lt) {
                    var prmType = ExtensionMethods.Substitute(candidate.ParameterTypes[i], substitution);
                    // Queryable extensions take Expression<TDelegate>; Enumerable takes TDelegate directly.
                    var delegateType = UnwrapExpressionDelegate(prmType);
                    if (!typeof(Delegate).IsAssignableFrom(delegateType))
                        return false;

                    var invoke = delegateType.GetMethod("Invoke");
                    if (invoke == null)
                        return false;

                    var lambdaPrms = invoke.GetParameters();
                    if (lambdaPrms.Any(p => p.ParameterType.ContainsGenericParameters))
                        return false;

                    var visited = VisitLambda(lt, lambdaPrms.Select(p => p.ParameterType), parameters);
                    resolvedArgs[i] = visited;

                    if (invoke.ReturnType.ContainsGenericParameters
                        && !ExtensionMethods.BindGenericArgs(invoke.ReturnType, visited.ReturnType, substitution))
                        return false;
                }
                else {
                    var arg = args[i]!;
                    if (!ExtensionMethods.BindGenericArgs(candidate.ParameterTypes[i], arg.Type, substitution))
                        return false;
                    resolvedArgs[i] = arg;
                }
            }

            var typeArgs = method.GetGenericArguments();
            var concrete = new Type[typeArgs.Length];
            for (var i = 0; i < typeArgs.Length; i++) {
                if (!substitution.TryGetValue(typeArgs[i], out var t))
                    return false;
                concrete[i] = t;
            }

            var specialized = method.MakeGenericMethod(concrete);
            var finalPrms = specialized.GetParameters();
            var finalCallArgs = new Expression[args.Length];
            for (var i = 0; i < args.Length; i++) {
                var targetType = finalPrms[i + 1].ParameterType;
                var converted = AdaptArgument(resolvedArgs[i]!, targetType);
                if (converted == null) return false;
                finalCallArgs[i] = converted;
            }

            result = Expression.Call(null, specialized, new[] { owner }.Concat(finalCallArgs));
            return true;
        }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
    }

    // Adapts a visited argument expression to the target parameter type:
    //   - If the arg is a LambdaExpression and target wants Expression<TDelegate>, wrap via Quote.
    //   - If types already match, return as-is.
    //   - If convertible (numeric widening / assignable), emit Convert.
    //   - Otherwise signal mismatch (null).
    private static Expression? AdaptArgument(Expression arg, Type targetType) {
        if (arg is LambdaExpression lambda
            && targetType.IsGenericType
            && !targetType.IsGenericTypeDefinition
            && targetType.GetGenericTypeDefinition() == typeof(Expression<>)) {
            return Expression.Quote(lambda);
        }

        if (arg.Type == targetType)
            return arg;

        if (Helper.CanConvert(targetType, arg.Type) || targetType.IsAssignableFrom(arg.Type))
            return Expression.Convert(arg, targetType);

        return null;
    }

    private static Type UnwrapExpressionDelegate(Type type) {
        if (type.IsGenericType && !type.IsGenericTypeDefinition
            && type.GetGenericTypeDefinition() == typeof(Expression<>)) {
            return type.GetGenericArguments()[0];
        }
        return type;
    }

    private MethodCallExpression? BuildCall(Expression owner, MethodInfo method,
                                            IReadOnlyList<ParameterInfo> prms, IReadOnlyList<Type> prmTypes,
                                            Expression?[] args, Token[] tokens, bool isExtension,
                                            ParameterExpression[] parameters) {
        var isArgumentCountCorrect = prms.Count == args.Length;
        var isExtraParameterWithDefault = prms.Count > args.Length && prms[args.Length].HasDefaultValue;
        if (!(isArgumentCountCorrect || isExtraParameterWithDefault))
            return null;

        for (var i = 0; i < args.Length; i++) {
            var prmType = prmTypes[i];

            if (tokens[i] is LambdaToken lt) {
                var delegateType = UnwrapExpressionDelegate(prmType);
                if (!typeof(Delegate).IsAssignableFrom(delegateType) || delegateType.ContainsGenericParameters)
                    return null;

                var lambdaPrms = delegateType.GetMethod("Invoke")!.GetParameters();
                args[i] = VisitLambda(lt, lambdaPrms.Select(p => p.ParameterType), parameters);
            }

            var adapted = AdaptArgument(args[i]!, prmType);
            if (adapted == null) return null;
            args[i] = adapted;
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
