using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Jokenizer.Net {
    using Dynamic;
    using Tokens;

    public class Evaluator<T> {

        private Evaluator(IEnumerable<ParameterExpression> parameters, IEnumerable<object> externals) {
            these = new Stack<Type>(new[] { typeof(T) });
            this.parameters = parameters == null ? new ParameterExpression[0] : parameters.ToArray();
            this.externals = externals == null ? new object[0] : externals.ToArray();
        }

        private readonly Stack<Type> these;
        private ParameterExpression[] parameters;
        private object[] externals;
        private Type @this => these.Peek();

        private LambdaExpression VisitLambda(Token token) {
            throw new NotImplementedException();
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
                    throw new Exception($"Wrong usage of Lambda");
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
                    // todo: 
                    return Expression.PropertyOrField(null, vt.Name);
                default:
                    throw new Exception($"Unsupported token type {token.Type}");
            }
        }

        private static ExpressionType GetBinaryOp(string op) {
            // todo:
            return ExpressionType.Add;
        }

        private static ExpressionType GetUnaryOp(char op) {
            // todo:
            return ExpressionType.UnaryPlus;
        }

        public static LambdaExpression ToLambda<T>(Token token, IEnumerable<ParameterExpression> parameters, IEnumerable<object> externals) {
            var body = new Evaluator<T>(parameters, externals).Visit(token);

            return body is LambdaExpression
                ? (LambdaExpression)body
                : Expression.Lambda(body, parameters);
        }
    }
}
