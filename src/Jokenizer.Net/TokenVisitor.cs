using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Jokenizer.Net {
    using Dynamic;
    using Tokens;

    public class TokenVisitor {

        static readonly Dictionary<char, ExpressionType> unary = new Dictionary<char, ExpressionType> {
            { '-', ExpressionType.Negate },
            { '+', ExpressionType.UnaryPlus },
            { '!', ExpressionType.Not },
            { '~', ExpressionType.OnesComplement }
        };

        static readonly Dictionary<string, ExpressionType> binary = new Dictionary<string, ExpressionType> {
            { "&&", ExpressionType.And },
            { "||", ExpressionType.OrElse },
            { "??", ExpressionType.Coalesce },
            { "|", ExpressionType.Or },
            { "^", ExpressionType.ExclusiveOr },
            { "&", ExpressionType.And },
            { "==", ExpressionType.Equal },
            { "!=", ExpressionType.NotEqual },
            { "<=", ExpressionType.LessThanOrEqual },
            { ">=", ExpressionType.GreaterThanOrEqual },
            { "<", ExpressionType.LessThan },
            { ">", ExpressionType.GreaterThan },
            { "<<", ExpressionType.LeftShift },
            { ">>", ExpressionType.RightShift },
            { "+", ExpressionType.Add },
            { "-", ExpressionType.Subtract },
            { "*", ExpressionType.Multiply },
            { "/", ExpressionType.Divide },
            { "%", ExpressionType.Modulo }
        };
        static readonly MethodInfo concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });

        readonly IDictionary<string, object> variables;

        public TokenVisitor(IDictionary<string, object> variables, IEnumerable<object> parameters) {
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

        public virtual LambdaExpression Process(Token token, IEnumerable<Type> typeParameters, IEnumerable<ParameterExpression> parameters = null) {
            typeParameters = typeParameters ?? Enumerable.Empty<Type>();
            parameters = parameters ?? Enumerable.Empty<ParameterExpression>();

            if (token is LambdaToken lt)
                return VisitLambda(lt, typeParameters, parameters);

            var prms = typeParameters.Select(t => Expression.Parameter(t)).ToList();
            var body = Visit(token, parameters.Concat(prms).ToList());

            return Expression.Lambda(body, prms);
        }

        public virtual Expression Visit(Token token, IEnumerable<ParameterExpression> parameters) {
            switch (token) {
                case BinaryToken bt:
                    return VisitBinary(bt, parameters);
                case CallToken ct:
                    return VisitCall(ct, parameters);
                case IndexerToken it:
                    return VisitIndexer(it, parameters);
                case LiteralToken lit:
                    return VisitLiteral(lit, parameters);
                case MemberToken mt:
                    return VisitMember(mt, parameters);
                case ObjectToken ot:
                    return VisitObject(ot, parameters);
                case TernaryToken tt:
                    return VisitTernary(tt, parameters);
                case UnaryToken ut:
                    return VisitUnary(ut, parameters);
                case VariableToken vt:
                    return VisitVariable(vt, parameters);
                case AssignToken at:
                case GroupToken gt:
                case LambdaToken lt:
                    throw new Exception($"Invalid {token.Type} expression usage");
                default:
                    throw new Exception($"Unsupported token type {token.Type}");
            }
        }

        protected virtual Expression VisitBinary(BinaryToken token, IEnumerable<ParameterExpression> parameters) {
            var left = Visit(token.Left, parameters);
            var right = Visit(token.Right, parameters);

            FixTypes(ref left, ref right);

            if (left.Type == typeof(string) && token.Operator == "+")
                return Expression.Add(left, right, concatMethod);

            return Expression.MakeBinary(GetBinaryOp(token.Operator), left, right);
        }

        protected virtual Expression VisitCall(CallToken token, IEnumerable<ParameterExpression> parameters) {
            Expression instance;
            string methodName;

            if (token.Callee is MemberToken mt) {
                instance = Visit(mt.Owner, parameters);
                methodName = mt.Member;
            } else if (token.Callee is VariableToken vt && parameters.Count() == 1) {
                instance = parameters.First();
                methodName = vt.Name;
            } else
                throw new Exception("Unsupported method call");

            var (method, isExtension) = GetMethod(instance, methodName, token.Args.Length);

            var args = method.GetParameters().Skip(isExtension ? 1 : 0).Zip(token.Args, (p, a) => {
                if (!(a is LambdaToken lt))
                    return Visit(a, parameters);

                var g = p.ParameterType.GetGenericArguments();
                return VisitLambda(lt, g.Take(g.Length - 1), parameters);
            });

            return isExtension
                ? Expression.Call(null, method, new[] { instance }.Concat(args))
                : Expression.Call(instance, method, args);
        }

        protected virtual Expression VisitIndexer(IndexerToken token, IEnumerable<ParameterExpression> parameters) {
            // todo: other than array?
            return Expression.ArrayIndex(Visit(token.Owner, parameters), Visit(token.Key, parameters));
        }

        protected virtual LambdaExpression VisitLambda(LambdaToken token, IEnumerable<Type> typeParameters, IEnumerable<ParameterExpression> parameters = null) {
            var prms = typeParameters.Zip(token.Parameters, (pt, ps) => Expression.Parameter(pt, ps)).ToList();
            var body = Visit(token.Body, parameters.Concat(prms).ToList());

            return Expression.Lambda(body, prms);
        }

        protected virtual Expression VisitLiteral(LiteralToken token, IEnumerable<ParameterExpression> parameters) {
            return Expression.Constant(token.Value, token.Value != null ? token.Value.GetType() : typeof(object));
        }

        protected virtual Expression VisitMember(MemberToken token, IEnumerable<ParameterExpression> parameters) {
            return Expression.PropertyOrField(Visit(token.Owner, parameters), token.Member);
        }

        protected virtual Expression VisitObject(ObjectToken token, IEnumerable<ParameterExpression> parameters) {
            var props = token.Members.Select(m => new { m.Name, Right = Visit(m.Right, parameters) });
            var type = ClassFactory.Instance.GetDynamicClass(props.Select(p => new DynamicProperty(p.Name, p.Right.Type)));
            var newExp = Expression.New(type.GetConstructors().First());
            var bindings = props.Select(p => Expression.Bind(type.GetProperty(p.Name), p.Right));

            return Expression.MemberInit(newExp, bindings);
        }

        protected virtual Expression VisitTernary(TernaryToken token, IEnumerable<ParameterExpression> parameters) {
            return Expression.Condition(Visit(token.Predicate, parameters), Visit(token.WhenTrue, parameters), Visit(token.WhenFalse, parameters));
        }

        protected virtual Expression VisitUnary(UnaryToken token, IEnumerable<ParameterExpression> parameters) {
            return Expression.MakeUnary(GetUnaryOp(token.Operator), Visit(token.Target, parameters), null);
        }

        protected virtual Expression VisitVariable(VariableToken token, IEnumerable<ParameterExpression> parameters) {
            var name = token.Name;

            if (this.variables.TryGetValue(name, out var value))
                return Expression.Constant(value, value != null ? value.GetType() : typeof(object));

            var prm = parameters.FirstOrDefault(p => p.Name == name);
            if (prm != null)
                return prm;

            if (parameters.Count() == 1) {
                var owner = parameters.First();
                return Expression.PropertyOrField(owner, token.Name);
            }

            throw new Exception($"Unknown variable {name}");
        }

        (MethodInfo method, bool isExtension) GetMethod(Expression owner, string name, int parameterCount) {
            var method = owner.Type.GetMethods().FirstOrDefault(m => m.Name == name && parameterCount == m.GetParameters().Count());
            if (method != null) return (method, false);

            var extension = ExtensionMethods.GetExtensionMethod(owner.Type, name, parameterCount);
            if (extension == null)
                throw new Exception($"Could not find instance or extension method for {name} for {owner.Type}");

            return (extension, true);
        }

        static ExpressionType GetBinaryOp(string op) {
            if (binary.TryGetValue(op, out var et))
                return et;

            throw new Exception($"Unknown binary operator {op}");
        }

        static ExpressionType GetUnaryOp(char op) {
            if (unary.TryGetValue(op, out var ut))
                return ut;

            throw new Exception($"Unknown unary operator {op}");
        }

        static void FixTypes(ref Expression left, ref Expression right) {
            if (left.Type == right.Type) return;

            var ok =
            TryFixNullable(left, ref right) ||
            TryFixNullable(right, ref left) ||
            TryFixForGuid(left, ref right) ||
            TryFixForGuid(right, ref left) ||
            TryFixForDateTime(left, ref right) ||
            TryFixForDateTime(right, ref left);

            if (!ok) {
                // let CLR throw exception if types are not compatible
                right = Expression.Convert(right, left.Type);
            }
        }

        static bool TryFixNullable(Expression e1, ref Expression e2) {
            if (!e2.Type.IsConstructedGenericType || e2.Type.GetGenericArguments()[0] != e1.Type)
                return false;

            e2 = Expression.Convert(e2, e1.Type);

            return true;
        }

        static bool TryFixForGuid(Expression e1, ref Expression e2) {
            if ((e1.Type != typeof(Guid?) && e1.Type != typeof(Guid)) || e2.Type != typeof(string) || !(e2 is ConstantExpression ce2))
                return false;

            var guidValue = Guid.Parse(ce2.Value.ToString());
            Guid? nullableGuidValue = guidValue;
            e2 = e1.Type == typeof(Guid?)
                ? Expression.Constant(nullableGuidValue, typeof(Guid?))
                : Expression.Constant(guidValue, typeof(Guid));

            return true;
        }

        static bool TryFixForDateTime(Expression e1, ref Expression e2) {
            if ((e1.Type != typeof(DateTime?) && e1.Type != typeof(DateTime)) || e2.Type != typeof(string) || !(e2 is ConstantExpression ce2))
                return false;

            var dateValue = DateTime.Parse(ce2.Value.ToString());
            DateTime? nullableDateValue = dateValue;
            e2 = e1.Type == typeof(DateTime?)
                ? Expression.Constant(nullableDateValue, typeof(DateTime?))
                : Expression.Constant(dateValue, typeof(DateTime));

            return true;
        }
    }
}
