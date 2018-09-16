using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Xunit;

namespace Jokenizer.Net.Tests {
    using Fixture;
    using Tokens;

    public class TokenizerTests {

        [Fact]
        public void ShouldReturnNull() {
            Assert.Throws<ArgumentNullException>(() => Tokenizer.Parse(null));
            Assert.Throws<ArgumentException>(() => Tokenizer.Parse(" "));
        }

        [Fact]
        public void ShouldReturnNumberLiteralToken() {
            var e = Tokenizer.Parse<LiteralToken>("42");
            Assert.Equal(42f, e.Value);
        }

        [Fact]
        public void ShouldReturnFloatLiteralToken() {
            var e = Tokenizer.Parse<LiteralToken>("42.4242");
            Assert.Equal(42.4242f, e.Value);
        }

        [Fact]
        public void ShouldReturnStringLiteralToken() {
            var e1 = Tokenizer.Parse<LiteralToken>("\"4\\\"2\"");
            Assert.Equal("4\"2", e1.Value);

            Assert.Throws<Exception>(() => Tokenizer.Parse("\"blow"));

            var e2 = Tokenizer.Parse<LiteralToken>("\"\\a\\b\\f\\n\\r\\t\\v\\0\\\"\\\\\"");
            Assert.Equal("\a\b\f\n\r\t\v\0\"\\", e2.Value);
        }

        [Fact]
        public void ShouldReturnLiteralTokenForKnownVariables() {
            var e1 = Tokenizer.Parse<LiteralToken>("true");
            Assert.Equal(true, e1.Value);

            var e2 = Tokenizer.Parse<LiteralToken>("false");
            Assert.Equal(false, e2.Value);

            var e3 = Tokenizer.Parse<LiteralToken>("null");
            Assert.Equal(null, e3.Value);
        }

        [Fact]
        public void ShouldReturnVariableToken() {
            var e1 = Tokenizer.Parse<VariableToken>("Name");
            Assert.Equal("Name", e1.Name);
        }

        [Fact]
        public void ShouldReturnUnaryToken() {
            var e = Tokenizer.Parse<UnaryToken>("!IsActive");

            Assert.Equal('!', e.Operator);
            Assert.Equal(TokenType.Variable, e.Target.Type);

            var oe = (VariableToken)e.Target;
            Assert.Equal("IsActive", oe.Name);
        }

        [Fact]
        public void ShouldReturnObjectToken() {
            var e = Tokenizer.Parse<ObjectToken>("new { a = 4, b }");

            Assert.Equal(2, e.Members.Length);
            Assert.Equal("a", e.Members[0].Name);
            Assert.Equal("b", e.Members[1].Name);
        }

        [Fact]
        public void ShouldReturnMemberToken() {
            var e = Tokenizer.Parse("Company.Name");
            Assert.Equal(TokenType.Member, e.Type);

            var me = e as MemberToken;
            Assert.Equal(TokenType.Variable, me.Owner.Type);

            var ve = me.Owner as VariableToken;
            Assert.Equal("Company", ve.Name);

            Assert.Equal("Name", me.Member.Name);
        }

        [Fact]
        public void ShouldReturnIndexerExpression() {
            var e = Tokenizer.Parse("Company[\"Name\"]");
            Assert.Equal(TokenType.Indexer, e.Type);

            var me = e as IndexerToken;
            Assert.Equal(TokenType.Variable, me.Owner.Type);
            Assert.Equal(TokenType.Literal, me.Key.Type);

            var ve = me.Owner as VariableToken;
            Assert.Equal("Company", ve.Name);

            var ve2 = me.Key as LiteralToken;
            Assert.Equal("Name", ve2.Value);
        }
    }
}
