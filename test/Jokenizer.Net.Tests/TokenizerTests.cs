using System;
using System.Linq.Expressions;
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
            var e1 = Tokenizer.Parse<ConstantExpression>("'4\"2'");
            Assert.Equal("4\"2", e1.Value);

            Assert.Throws<Exception>(() => Tokenizer.Parse("'blow"));

            var e2 = Tokenizer.Parse<ConstantExpression>("'\\a\\b\\f\\n\\r\\t\\v\\0\\'\\\"\\\\'");
            Assert.Equal("\a\b\f\n\r\t\v\0'\"\\", e2.Value);
        }
    }
}
