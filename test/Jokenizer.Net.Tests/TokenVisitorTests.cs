using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Jokenizer.Net.Tests {
    using Fixture;
    using Tokens;

    public class TokenVisitorTests {

        [Fact]
        public void ShouldEvaluateNumber() {
            var v = TokenVisitor.ToFunc<int>(Tokenizer.Parse("42"));
            Assert.Equal(42, v());
        }

        [Fact]
        public void ShouldEvaluateFloatNumber() {
            var v = TokenVisitor.ToFunc<float>(Tokenizer.Parse("42.4242"));
            Assert.Equal(42.4242, v(), 1);
        }

        [Fact]
        public void ShouldEvaluateInterpolatedString() {
            var v1 = TokenVisitor.ToFunc<string>(Tokenizer.Parse<LiteralToken>("\"4\\\"2\""));
            Assert.Equal("4\"2", v1());

            var v2 = TokenVisitor.ToFunc<string>(Tokenizer.Parse<LiteralToken>("\"\\a\\b\\f\\n\\r\\t\\v\\0\\\"\\\\\""));
            Assert.Equal("\a\b\f\n\r\t\v\0\"\\", v2());
        }
    }
}
