using System;
using System.Linq.Expressions;

namespace Jokenizer.Net {

    public class Expression {
        private readonly string _exp;
        private int _index;

        private Expression(string exp) {
            if (string.IsNullOrWhiteSpace(exp))
                throw new ArgumentNullException(nameof(exp));
                
            _exp = exp;
        }

        public static Expression From(string exp) {
            throw new NotImplementedException();
        }

        Expression TryExpression() {
            throw new NotImplementedException();
        }

        Expression TryLiteral() {
            throw new NotImplementedException();
        }

        Expression TryVariable() {
            throw new NotImplementedException();
        }

        Expression TryUnary() {
            throw new NotImplementedException();
        }

        Expression TryGroup() {
            throw new NotImplementedException();
        }

        Expression TryObject() {
            throw new NotImplementedException();
        }

        Expression TryArray() {
            throw new NotImplementedException();
        }

        Expression TryBinary() {
            throw new NotImplementedException();
        }

        Expression TryMember() {
            throw new NotImplementedException();
        }

        Expression TryIndexer() {
            throw new NotImplementedException();
        }

        Expression TryLambda() {
            throw new NotImplementedException();
        }

        Expression TryCall() {
            throw new NotImplementedException();
        }

        Expression TryTernary() {
            throw new NotImplementedException();
        }

        Expression TryKnown() {
            throw new NotImplementedException();
        }
    }
}
