using System;
using System.Linq;
using System.Linq.Expressions;
using Jokenizer.Net.Tests.Fixture;
using Xunit;

namespace Jokenizer.Net.Tests {

    public class TokenizerTests {

        [Fact]
        public void ShouldReturnNull() {
            Assert.Throws<ArgumentNullException>(() => Tokenizer.Parse(null));
            Assert.Throws<ArgumentException>(() => Tokenizer.Parse(" "));
        }

        [Fact]
        public void ShouldReturnNumberLiteralExpression() {
            var e = Tokenizer.Parse<ConstantExpression>("42");
            Assert.Equal(42f, e.Value);
        }

        [Fact]
        public void ShouldReturnFloatConstantExpression() {
            var e = Tokenizer.Parse<ConstantExpression>("42.4242");
            Assert.Equal(42.4242f, e.Value);
        }

        [Fact]
        public void ShouldReturnStringConstantExpression() {
            var e1 = Tokenizer.Parse<ConstantExpression>("\"4\\\"2\"");
            Assert.Equal("4\"2", e1.Value);

            Assert.Throws<Exception>(() => Tokenizer.Parse("\"blow"));

            var e2 = Tokenizer.Parse<ConstantExpression>("\"\\a\\b\\f\\n\\r\\t\\v\\0\\\"\\\\\"");
            Assert.Equal("\a\b\f\n\r\t\v\0\"\\", e2.Value);
        }

        [Fact]
        public void ShouldReturnConstantExpressionForKnownVariables() {
            var e1 = Tokenizer.Parse<ConstantExpression>("true");
            Assert.Equal(true, e1.Value);

            var e2 = Tokenizer.Parse<ConstantExpression>("false");
            Assert.Equal(false, e2.Value);

            var e3 = Tokenizer.Parse<ConstantExpression>("null");
            Assert.Equal(null, e3.Value);
        }

        [Fact]
        public void ShouldReturnVariableExpression() {
            var e = Tokenizer.Parse<MemberExpression>("Name", new { Name = "Rick" });
            Assert.Equal("Name", e.Member.Name);
        }

        [Fact]
        public void ShouldReturnUnaryExpression() {
            var e = Tokenizer.Parse<UnaryExpression>("!IsActive", new { IsActive = false });

            Assert.Equal(ExpressionType.Not, e.NodeType);
            Assert.Equal(ExpressionType.MemberAccess, e.Operand.NodeType);

            var oe = e.Operand as MemberExpression;
            Assert.Equal("IsActive", oe.Member.Name);
        }

        [Fact]
        public void ShouldReturnNewExpression() {
            var e = Tokenizer.Parse<NewExpression>("new { a = 4, b }", new { b = 2 });

            Assert.Equal(2, e.Arguments.Count);
            Assert.Equal(ExpressionType.Constant, e.Arguments[0].NodeType);
            Assert.Equal(4, ((ConstantExpression)e.Arguments[0]).Value);
            Assert.Equal(ExpressionType.Constant, e.Arguments[1].NodeType);
            Assert.Equal(2, ((ConstantExpression)e.Arguments[0]).Value);

            Assert.Equal(2, e.Members.Count);
            Assert.Equal("a", e.Members[0].Name);
            Assert.Equal("b", e.Members[1].Name);
        }
    }
}
