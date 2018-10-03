using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Jokenizer.Net.Tests {
    using Fixture;
    using Tokens;

    public class TokenizerTests {

        [Fact]
        public void ShouldThrowForNullOrEmpty() {
            Assert.Throws<ArgumentNullException>(() => Tokenizer.Parse(null));
            Assert.Throws<ArgumentException>(() => Tokenizer.Parse(" "));
        }

        [Fact]
        public void ShouldReturnNumberLiteralToken() {
            var e = Tokenizer.Parse<LiteralToken>("42");
            Assert.Equal(42, e.Value);
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

            Assert.Throws<InvalidSyntaxException>(() => Tokenizer.Parse("\"blow"));

            var e2 = Tokenizer.Parse<LiteralToken>("\"\\z\\a\\b\\f\\n\\r\\t\\v\\0\\\"\\\\\"");
            Assert.Equal("\\z\a\b\f\n\r\t\v\0\"\\", e2.Value);
        }

        [Fact]
        public void ShouldReturnLiteralTokenForKnownVariables() {
            var e1 = Tokenizer.Parse<LiteralToken>("true");
            Assert.True((bool)e1.Value);

            var e2 = Tokenizer.Parse<LiteralToken>("false");
            Assert.False((bool)e2.Value);

            var e3 = Tokenizer.Parse<LiteralToken>("null");
            Assert.Null(e3.Value);
        }

        [Fact]
        public void ShouldReturnVariableToken() {
            var e1 = Tokenizer.Parse<VariableToken>("Name");
            Assert.Equal("Name", e1.Name);

            var e2 = Tokenizer.Parse<VariableToken>("@0");
            Assert.Equal("@0", e2.Name);

            Assert.Throws<InvalidSyntaxException>(() => Tokenizer.Parse<VariableToken>("42d"));
            Assert.Throws<InvalidSyntaxException>(() => Tokenizer.Parse<VariableToken>("@a"));
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
            var e = Tokenizer.Parse<ObjectToken>("new { a = 4, b.c }");

            Assert.Equal(2, e.Members.Length);
            Assert.Equal("a", e.Members[0].Name);
            Assert.Equal("c", e.Members[1].Name);

            Assert.Throws<InvalidSyntaxException>(() => Tokenizer.Parse<ObjectToken>("new { a = 4"));
            Assert.Throws<InvalidSyntaxException>(() => Tokenizer.Parse<ObjectToken>("new { 4 = 4 }"));
            Assert.Throws<InvalidSyntaxException>(() => Tokenizer.Parse<ObjectToken>("new { a.b = 4 }"));
        }

        [Fact]
        public void ShouldReturnArrayToken() {
            var e = Tokenizer.Parse<ArrayToken>("new[] { 4, b.c }");

            LiteralToken lt;
            Assert.Equal(2, e.Items.Length);
            Assert.NotNull(lt = e.Items[0] as LiteralToken);
            Assert.Equal(4, lt.Value);
            MemberToken mt;
            Assert.NotNull(mt = e.Items[1] as MemberToken);
            Assert.Equal("c", mt.Name);
            VariableToken vt;
            Assert.NotNull(vt = mt.Owner as VariableToken);
            Assert.Equal("b", vt.Name);

            Assert.Throws<InvalidSyntaxException>(() => Tokenizer.Parse<ObjectToken>("new ["));
            Assert.Throws<InvalidSyntaxException>(() => Tokenizer.Parse<ObjectToken>("new []"));
            Assert.Throws<InvalidSyntaxException>(() => Tokenizer.Parse<ObjectToken>("new [] { 4, "));
            Assert.Throws<InvalidSyntaxException>(() => Tokenizer.Parse<ObjectToken>("new [] { 4, 2"));
            Assert.Throws<InvalidSyntaxException>(() => Tokenizer.Parse<ObjectToken>("new [] { a = 2 }"));
        }

        [Fact]
        public void ShouldReturnMemberToken() {
            var e = Tokenizer.Parse("Company.Name");
            Assert.Equal(TokenType.Member, e.Type);

            var me = e as MemberToken;
            Assert.Equal(TokenType.Variable, me.Owner.Type);

            var ve = me.Owner as VariableToken;
            Assert.Equal("Company", ve.Name);
            Assert.Equal("Name", me.Name);

            Assert.Throws<InvalidSyntaxException>(() => Tokenizer.Parse("Company.4D"));
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
        public void ShouldReturnLambdaToken() {
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

            Assert.Throws<InvalidSyntaxException>(() => Tokenizer.Parse("(a, 4) => a < 4"));
            Assert.Throws<InvalidSyntaxException>(() => Tokenizer.Parse("4 => a < 4"));
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
            Assert.Equal(42, le.Value);

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
            Assert.Equal(42, wt.Value);

            var wf = te.WhenFalse as LiteralToken;
            Assert.Equal(21, wf.Value);
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

            var ie1 = Tokenizer.Parse("$\"don't {w}, 42\"");
            Assert.Equal(TokenType.Binary, ie1.Type);

            var bie = ie1 as BinaryToken;
            Assert.Equal("+", bie.Operator);
            Assert.Equal(TokenType.Binary, bie.Left.Type);
            Assert.Equal(TokenType.Literal, bie.Right.Type);

            var ie2 = Tokenizer.Parse("$\"don't {w}\"");
            Assert.Equal(TokenType.Binary, ie2.Type);

            Assert.Throws<InvalidSyntaxException>(() => Tokenizer.Parse("$\"don't {w, 42\""));
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
            Assert.Equal(1, le.Value);

            var re = be.Right as BinaryToken;
            Assert.Equal("*", re.Operator);
            Assert.Equal(TokenType.Literal, re.Left.Type);
            Assert.Equal(TokenType.Literal, re.Right.Type);

            var le2 = re.Left as LiteralToken;
            Assert.Equal(2, le2.Value);

            var le3 = re.Right as LiteralToken;
            Assert.Equal(3, le3.Value);
        }
        
        [Fact]
        public void ShoulHandleNullArgument() {
            var call = new CallToken(null);
            var group = new GroupToken(null);
            var lambda = new LambdaToken(null);
            var obj = new ObjectToken(null);
            var array = new ArrayToken(null);

            Assert.NotNull(call.Args);
            Assert.NotNull(group.Tokens);
            Assert.NotNull(lambda.Parameters);
            Assert.NotNull(obj.Members);
            Assert.NotNull(array.Items);
        }
    }
}
