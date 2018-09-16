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
        public void ShouldReturnIndexerToken() {
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

        [Fact]
        public void ShouldReturnFuncToken() {
            var e = Tokenizer.Parse("(a, b) => a < b");
            Assert.Equal(TokenType.Lambda, e.Type);

            var fe = e as LambdaToken;
            Assert.Equal(2, fe.Parameters.Count());
            Assert.Equal(new[] { "a", "b" }, fe.Parameters);
            Assert.Equal(TokenType.Binary, fe.Body.Type);

            var be = fe.Body as BinaryToken;
            Assert.Equal("<", be.Operator);
            Assert.Equal(TokenType.Variable, be.Left.Type);
            Assert.Equal(TokenType.Variable, be.Right.Type);

            var le = be.Left as VariableToken;
            Assert.Equal("a", le.Name);

            var re = be.Right as VariableToken;
            Assert.Equal("b", re.Name);
        }

        [Fact]
        public void ShouldReturnCallToken() {
            var e = Tokenizer.Parse("Test(42, a)");
            Assert.Equal(TokenType.Call, e.Type);

            var ce = e as CallToken;
            Assert.Equal(TokenType.Variable, ce.Callee.Type);
            Assert.Equal(2, ce.Args.Length);
            Assert.Equal(TokenType.Literal, ce.Args[0].Type);
            Assert.Equal(TokenType.Variable, ce.Args[1].Type);

            var le = ce.Args[0] as LiteralToken;
            Assert.Equal(42f, le.Value);

            var ve = ce.Args[1] as VariableToken;
            Assert.Equal("a", ve.Name);
        }

        [Fact]
        public void ShouldReturnTernaryToken() {
            var e = Tokenizer.Parse("check ? 42 : 21");
            Assert.Equal(TokenType.Ternary, e.Type);

            var te = e as TernaryToken;
            Assert.Equal(TokenType.Variable, te.Predicate.Type);
            Assert.Equal(TokenType.Literal, te.WhenTrue.Type);
            Assert.Equal(TokenType.Literal, te.WhenFalse.Type);

            var pe = te.Predicate as VariableToken;
            Assert.Equal("check", pe.Name);

            var wt = te.WhenTrue as LiteralToken;
            Assert.Equal(42f, wt.Value);

            var wf = te.WhenFalse as LiteralToken;
            Assert.Equal(21f, wf.Value);
        }

        [Fact]
        public void ShouldReturnBinaryToken() {
            var e = Tokenizer.Parse("v1 > v2");
            Assert.Equal(TokenType.Binary, e.Type);

            var be = e as BinaryToken;
            Assert.Equal(">", be.Operator);
            Assert.Equal(TokenType.Variable, be.Left.Type);
            Assert.Equal(TokenType.Variable, be.Right.Type);

            var le = be.Left as VariableToken;
            Assert.Equal("v1", le.Name);

            var re = be.Right as VariableToken;
            Assert.Equal("v2", re.Name);

            var ie = Tokenizer.Parse("$\"don't {w}, 42\"");
            Assert.Equal(TokenType.Binary, ie.Type);

            var bie = ie as BinaryToken;
            Assert.Equal("+", bie.Operator);
            Assert.Equal(TokenType.Binary, bie.Left.Type);
            Assert.Equal(TokenType.Literal, bie.Right.Type);

            Assert.Throws<Exception>(() => Tokenizer.Parse("$\"don't {w, 42\""));
        }

        [Fact]
        public void ShouldReturnBinaryTokenWithCorrectPrecedence() {
            var e = Tokenizer.Parse("1 + 2 * 3");
            Assert.Equal(TokenType.Binary, e.Type);

            var be = e as BinaryToken;
            Assert.Equal("+", be.Operator);
            Assert.Equal(TokenType.Literal, be.Left.Type);
            Assert.Equal(TokenType.Binary, be.Right.Type);

            var le = be.Left as LiteralToken;
            Assert.Equal(1f, le.Value);

            var re = be.Right as BinaryToken;
            Assert.Equal("*", re.Operator);
            Assert.Equal(TokenType.Literal, re.Left.Type);
            Assert.Equal(TokenType.Literal, re.Right.Type);

            var le2 = re.Left as LiteralToken;
            Assert.Equal(2f, le2.Value);

            var le3 = re.Right as LiteralToken;
            Assert.Equal(3f, le3.Value);
        }
    }
}
