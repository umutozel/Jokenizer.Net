using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Jokenizer.Net {
    using Dynamic;
    using Tokens;

    public class TokenVisitor {

        private TokenVisitor(Dictionary<string, object> variables, IEnumerable<object> parameters) {
            this.variables = variables ?? new Dictionary<string, object>();
            
            if (parameters != null) {
                var i = 0;
                parameters.ToList().ForEach(e => {
                    var k = $"@{i++}";
                    if (!this.variables.ContainsKey(k)) {
                        this.variables.Add(k, e);
                    }
                });
            }
        }

        private readonly Dictionary<string, object> variables;
        private IEnumerable<ParameterExpression> parameters = Enumerable.Empty<ParameterExpression>();

        public LambdaExpression Visit(Token token, IEnumerable<Type> parameters) {
            var oldParameters = this.parameters;

            var prms = token is LambdaToken lt
                ? parameters.Zip(lt.Parameters, (pt, ps) => Expression.Parameter(pt, ps))
                : Enumerable.Empty<ParameterExpression>();

            this.parameters = this.parameters.Concat(prms);
            var retVal = Expression.Lambda(Visit(token), this.parameters);
            this.parameters = oldParameters;

            return retVal;
        }

        private Expression Visit(Token token) {
            switch (token) {
                case BinaryToken bt:
                    return Expression.MakeBinary(GetBinaryOp(bt.Operator), Visit(bt.Left), Visit(bt.Right));
                case CallToken ct:
                    var callee = Visit(ct.Callee);
                    // todo: method?
                    return Expression.Call(callee, null, ct.Args.Select(a => Visit(a)));
                case IndexerToken it:
                    return Expression.ArrayIndex(Visit(it.Owner), Visit(it.Key));
                case LambdaToken lt:
                    throw new Exception($"Invalid Lambda usage");
                case LiteralToken lit:
                    return Expression.Constant(lit.Value);
                case MemberToken mt:
                    return Expression.PropertyOrField(Visit(mt.Owner), mt.Member.Name);
                case ObjectToken ot:
                    var props = ot.Members.Select(m => new { m.Name, Right = Visit(m.Right) });
                    var type = ClassFactory.Instance.GetDynamicClass(props.Select(p => new DynamicProperty(p.Name, p.Right.Type)));
                    var newExp = Expression.New(type.GetConstructors().First());
                    var bindings = props.Select(p => Expression.Bind(type.GetProperty(p.Name), p.Right));
                    return Expression.MemberInit(newExp, bindings);
                case TernaryToken tt:
                    return Expression.Condition(Visit(tt.Predicate), Visit(tt.WhenTrue), Visit(tt.WhenFalse));
                case UnaryToken ut:
                    return Expression.MakeUnary(GetUnaryOp(ut.Operator), Visit(ut.Target), null);
                case VariableToken vt:
                    return GetVariable(vt.Name);
                default:
                    throw new Exception($"Unsupported token type {token.Type}");
            }
        }

        private Expression GetVariable(string name) {
            var prm = this.parameters.FirstOrDefault(p => p.Name == name);
            if (prm != null)
                return prm;
            
            if (this.variables.TryGetValue(name, out var value))
                return Expression.Constant(value);
            
            throw new Exception($"Unknown variable {name}");
        }

        private static ExpressionType GetBinaryOp(string op) {
            // todo:
            return ExpressionType.Add;
        }

        private static ExpressionType GetUnaryOp(char op) {
            // todo:
            return ExpressionType.UnaryPlus;
        }

        public static LambdaExpression ToLambda(Token token, IEnumerable<Type> typeParameters, Dictionary<string, object> variables, 
                                                params object[] parameters) {
            return new TokenVisitor(variables, parameters).Visit(token, typeParameters);
        }

        public static LambdaExpression ToLambda(Token token, IEnumerable<Type> typeParameters, params object[] externals) {
            return ToLambda(token, typeParameters, null, externals);
        }

        public static Expression<Func<TResult>> ToLambda<TResult>(Token token, IDictionary<string, object> variables, 
                                                                  params object[] parameters) {
            return (Expression<Func<TResult>>)ToLambda(token, null, variables, parameters);
        }

        public static Expression<Func<TResult>> ToLambda<TResult>(Token token, params object[] parameters) {
            return (Expression<Func<TResult>>)ToLambda(token, null, null, parameters);
        }

        public static Expression<Func<T, TResult>> ToLambda<T, TResult>(Token token, IDictionary<string, object> variables, 
                                                                        params object[] parameters) {
            return (Expression<Func<T, TResult>>)ToLambda(token, new[] { typeof(T) }, variables, parameters);
        }

        public static Expression<Func<T, TResult>> ToLambda<T, TResult>(Token token, params object[] parameters) {
            return (Expression<Func<T, TResult>>)ToLambda(token, new[] { typeof(T) }, null, parameters);
        }

        public static Expression<Func<T1, T2, TResult>> ToLambda<T1, T2, TResult>(Token token, IDictionary<string, object> variables, 
                                                                                  params object[] parameters) {
            return (Expression<Func<T1, T2, TResult>>)ToLambda(token, new[] { typeof(T1), typeof(T2) }, variables, parameters);
        }

        public static Expression<Func<T1, T2, TResult>> ToLambda<T1, T2, TResult>(Token token, params object[] parameters) {
            return (Expression<Func<T1, T2, TResult>>)ToLambda(token, new[] { typeof(T1), typeof(T2) }, parameters);
        }

        public static Expression<Func<T1, T2, T3, TResult>> ToLambda<T1, T2, T3, TResult>(Token token, IDictionary<string, object> variables, 
                                                                                          params object[] parameters) {
            return (Expression<Func<T1, T2, T3, TResult>>)ToLambda(token, new[] { typeof(T1), typeof(T2), typeof(T3) }, variables, parameters);
        }

        public static Expression<Func<T1, T2, T3, TResult>> ToLambda<T1, T2, T3, TResult>(Token token, params object[] parameters) {
            return (Expression<Func<T1, T2, T3, TResult>>)ToLambda(token, new[] { typeof(T1), typeof(T2), typeof(T3) }, parameters);
        }
    }
}