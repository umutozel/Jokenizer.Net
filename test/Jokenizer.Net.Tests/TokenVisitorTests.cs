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
            var v1 = TokenVisitor.ToFunc<string>(Tokenizer.Parse("\"4\\\"2\""));
            Assert.Equal("4\"2", v1());

            var v2 = TokenVisitor.ToFunc<string>(Tokenizer.Parse("\"\\a\\b\\f\\n\\r\\t\\v\\0\\\"\\\\\""));
            Assert.Equal("\a\b\f\n\r\t\v\0\"\\", v2());
        }

        [Fact]
        public void ShouldEvaluteToKnownVariable() {
            var v1 = TokenVisitor.ToFunc<bool>(Tokenizer.Parse("true"));
            Assert.Equal(true, v1());

            var v2 = TokenVisitor.ToFunc<bool>(Tokenizer.Parse("false"));
            Assert.Equal(false, v2());

            var v3 = TokenVisitor.ToFunc<object>(Tokenizer.Parse("null"));
            Assert.Equal(null, v3());
        }

        [Fact]
        public void ShouldEvaluateVariable() {
            var v1 = TokenVisitor.ToFunc<string>(Tokenizer.Parse("Name"), new Dictionary<string, object> { { "Name", "Rick" } });
            Assert.Equal("Rick", v1());

            var v2 = TokenVisitor.ToFunc<string>(Tokenizer.Parse("@0"), "Rick");
            Assert.Equal("Rick", v1());
        }

        [Fact]
        public void ShouldEvaluateUnary() {
            var v = TokenVisitor.ToFunc<bool>(Tokenizer.Parse("!IsActive"), new Dictionary<string, object> { { "IsActive", true } });

            Assert.Equal(false, v());
        }
    }
}
