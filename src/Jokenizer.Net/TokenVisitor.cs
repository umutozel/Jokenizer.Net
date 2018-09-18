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

        public virtual LambdaExpression Visit(Token token, IEnumerable<Type> types, IEnumerable<ParameterExpression> parameters = null) {
            parameters = parameters ?? Enumerable.Empty<ParameterExpression>();

            if (token is LambdaToken lt) {
                var prms = types.Zip(lt.Parameters, (pt, ps) => Expression.Parameter(pt, ps));
                return Expression.Lambda(Visit(lt.Body, parameters.Concat(prms)), prms);
            }

            return Expression.Lambda(Visit(token, parameters));
        }

        Expression Visit(Token token, IEnumerable<ParameterExpression> parameters) {
            switch (token) {
                case BinaryToken bt:
                    return VisitBinary(bt, parameters);
                case CallToken ct:
                    return VisitCall(ct, parameters);
                case IndexerToken it:
                    return VisitIndexer(it, parameters);
                case LambdaToken lt:
                    throw new Exception($"Invalid lambda usage");
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
                default:
                    throw new Exception($"Unsupported token type {token.Type}");
            }
        }

        protected virtual Expression VisitBinary(BinaryToken token, IEnumerable<ParameterExpression> parameters) {
            return Expression.MakeBinary(GetBinaryOp(token.Operator), Visit(token.Left, parameters), Visit(token.Right, parameters));
        }

        protected virtual Expression VisitCall(CallToken token, IEnumerable<ParameterExpression> parameters) {
            // todo: method? lambda?
            var callee = Visit(token.Callee, parameters);
            if (callee is MemberExpression me)
                return Expression.Call(me.Expression, (MethodInfo)me.Member, token.Args.Select(a => Visit(a, parameters)));

            throw new Exception($"Invalid method call");
        }

        protected virtual Expression VisitIndexer(IndexerToken token, IEnumerable<ParameterExpression> parameters) {
            // todo: other than array?
            return Expression.ArrayIndex(Visit(token.Owner, parameters), Visit(token.Key, parameters));
        }

        protected virtual Expression VisitLiteral(LiteralToken token, IEnumerable<ParameterExpression> parameters) {
            return Expression.Constant(token.Value, token.Value != null ? token.Value.GetType() : typeof(object));
        }

        protected virtual Expression VisitMember(MemberToken token, IEnumerable<ParameterExpression> parameters) {
            return Expression.PropertyOrField(Visit(token.Owner, parameters), token.Member.Name);
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
            var prm = parameters.FirstOrDefault(p => p.Name == name);
            if (prm != null)
                return prm;

            if (this.variables.TryGetValue(name, out var value))
                return Expression.Constant(value, value != null ? value.GetType() : typeof(object));

            throw new Exception($"Unknown variable {name}");
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
    }
}
