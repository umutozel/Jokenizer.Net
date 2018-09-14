using System;
using System.Linq.Expressions;
using Xunit;

namespace Jokenizer.Net.Tests {

    public class TokenizerTests {

        [Fact]
        public void ShouldReturnNull() {
            Assert.Throws<ArgumentNullException>(() => Tokenize.From(null));
            Assert.Throws<ArgumentException>(() => Tokenize.From(" "));
        }

        [Fact]
        public void ShouldReturnNumberLiteralExpression() {
            var e = Tokenize.From("42");
            Assert.Equal(ExpressionType.Constant, e.NodeType);

            var ce = (ConstantExpression)e;
            Assert.Equal(42, ce.Value);
        }
    }
}
