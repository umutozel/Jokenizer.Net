using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Jokenizer.Net.Tests {
    using Fixture;
    using Tokens;

    public class EvaluatorTests {

        static EvaluatorTests() {
            ExtensionMethods.ProbeAllAssemblies();
            ExtensionMethods.ProbeAssemblies(typeof(Extensions).Assembly);
        }

        [Fact]
        public void ShouldEvaluateNumber() {
            var v = Evaluator.ToFunc<int>("42");
            Assert.Equal(42, v());
        }

        [Fact]
        public void ShouldEvaluateFloatNumber() {
            var v = Evaluator.ToFunc<float>("42.4242");
            Assert.Equal(42.4242, v(), 1);
        }

        [Fact]
        public void ShouldEvaluateString() {
            var v1 = Evaluator.ToFunc<string>("\"4\\\"2\"");
            Assert.Equal("4\"2", v1());

            var v2 = Evaluator.ToFunc<string>("\"\\a\\b\\f\\n\\r\\t\\v\\0\\\"\\\\\"");
            Assert.Equal("\a\b\f\n\r\t\v\0\"\\", v2());
        }

        [Fact]
        public void ShouldEvaluateInterpolatedString() {
            var v = Evaluator.ToFunc<string>("$\"don't {@0}, 42\"", "panic");
            Assert.Equal("don't panic, 42", v());
        }

        [Fact]
        public void ShouldEvaluteToKnownVariable() {
            var v1 = Evaluator.ToFunc<bool>("true");
            Assert.True(v1());

            var v2 = Evaluator.ToFunc<bool>("false");
            Assert.False(v2());

            var v3 = Evaluator.ToFunc<object>("null");
            Assert.Null(v3());
        }

        [Fact]
        public void ShouldEvaluateVariable() {
            var v1 = Evaluator.ToFunc<string>("Name", new Dictionary<string, object> { { "Name", "Rick" } });
            Assert.Equal("Rick", v1());

            var v2 = Evaluator.ToFunc<string>("@0", "Rick");
            Assert.Equal("Rick", v1());
        }

        [Fact]
        public void ShouldEvaluateUnary() {
            var v = Evaluator.ToFunc<bool>("!IsActive", new Dictionary<string, object> { { "IsActive", true } });
            Assert.False(v());

            Assert.Throws<Exception>(() => Evaluator.ToLambda<bool>(new UnaryToken('/', new LiteralToken(1))));
        }

        [Fact]
        public void ShouldEvaluateObject() {
            var v = Evaluator.ToFunc<dynamic>("new { a = 4, b = @0 }", 2);
            var o = v();
            Assert.Equal(4, o.a);
            Assert.Equal(2, o.b);
            var s = o.ToString();
            Assert.Equal("{a=4, b=2}", s);
        }

        [Fact]
        public void ShouldEvaluateMember() {
            var v1 = Evaluator.ToFunc<string>("Company.Name", new Dictionary<string, object> { { "Company", new { Name = "Rick" } } });
            Assert.Equal("Rick", v1());

            var v2 = Evaluator.ToFunc<string>("@0.Name", new { Name = "Rick" });
            Assert.Equal("Rick", v2());

            var v3 = Evaluator.ToFunc<Company, string>("Name");
            Assert.Equal("Netflix", v3(new Company { Name = "Netflix" }));

            Assert.Throws<Exception>(() => Evaluator.ToFunc<Company, int, string>("Address"));
        }

        [Fact]
        public void ShouldEvaluateIndexer() {
            var v = Evaluator.ToFunc<string>("@0[0]", null, new string[] { "Rick" }, null);
            Assert.Equal("Rick", v());
        }

        [Fact]
        public void ShouldEvaluateLambda() {
            var v = Evaluator.ToFunc<int, int, bool>("(a, b) => a < b");
            Assert.True(v(1, 2));

            Assert.Throws<Exception>(() => Evaluator.ToFunc<int, int, bool>("4 < (a, b) => a < b"));
        }

        [Fact]
        public void ShouldEvaluateCall() {
            var v1 = Evaluator.ToFunc<IEnumerable<int>, int>("items => items.Sum(i => i*2)");
            Assert.Equal(30, v1(new[] { 1, 2, 3, 4, 5 }));

            var v2 = Evaluator.ToFunc<IEnumerable<int>, int>("Sum(i => i*2)");
            Assert.Equal(30, v2(new[] { 1, 2, 3, 4, 5 }));

            var v3 = Evaluator.ToFunc<string>("\"RICK\".ToLower()");
            Assert.Equal("rick", v3());

            var v4 = Evaluator.ToFunc<Company, int>("c => c.Len()");
            Assert.Equal(7, v4(new Company { Name = "Netflix" }));

            Assert.Throws<Exception>(() => Evaluator.ToFunc<bool>("@0[1]()"));
            Assert.Throws<Exception>(() => Evaluator.ToFunc<IEnumerable<int>, int>("SumBody(i => i*2)"));
        }

        [Fact]
        public void ShouldEvaluateTernary() {
            var v = Evaluator.ToFunc<int>("@0 ? 42 : 21", true);
            Assert.Equal(42, v());
        }

        [Fact]
        public void ShouldEvaluateBinary() {
            var v1 = Evaluator.ToFunc<bool>("@0 > @1", 4, 2);
            Assert.True(v1());

            var v2 = Evaluator.ToFunc<Company, bool>("c => c.PostalCode < 4");
            Assert.True(v2(new Company { PostalCode = 3 }));

            var id = Guid.NewGuid();
            var v3 = Evaluator.ToFunc<Company, bool>($"c => c.Id == \"{id}\"");
            Assert.True(v3(new Company { Id = id }));

            var createDate = DateTime.Now.Date;
            var v4 = Evaluator.ToFunc<Company, bool>($"c => c.CreateDate == \"{createDate}\"");
            Assert.True(v4(new Company { CreateDate = createDate }));

            var updateDate = DateTime.Now.Date;
            var v5 = Evaluator.ToFunc<Company, bool>($"c => c.UpdateDate == \"{updateDate}\"");
            Assert.True(v5(new Company { UpdateDate = updateDate }));

            var v6 = Evaluator.ToFunc<Company, bool>($"c => c.UpdateDate == null");
            Assert.True(v6(new Company()));

            Assert.Throws<Exception>(() => Evaluator.ToLambda<bool>(new BinaryToken("!", new LiteralToken(1), new LiteralToken(2))));
        }

        [Fact]
        public void ShouldEvaluateBinaryWithCorrectPrecedence() {
            var v = Evaluator.ToFunc<int>("1 + 2 * 3");
            Assert.Equal(7, v());
        }

        [Fact]
        public void ShouldThrowForUnknownToken() {
            Assert.Throws<Exception>(() => Evaluator.ToLambda<bool>(new UnkownToken()));
        }

        [Fact]
        public void ShouldThrowForInvalidToken() {
            Assert.Throws<Exception>(() => Evaluator.ToLambda<string>(new AssignToken("Name", new LiteralToken("Netflix"))));
            Assert.Throws<Exception>(() => Evaluator.ToLambda<string>(new GroupToken(new[] { new LiteralToken("Netflix") })));
            Assert.Throws<Exception>(() => Evaluator.ToLambda<string>("a < b => b*2"));
        }

        [Fact]
        public void MethodSignatureTests() {
            var l1 = Evaluator.ToLambda<int, int, int>("(i1, i2) => i1 + i2");
            var l2 = Evaluator.ToLambda<int, int, int>("(i1, i2) => i1 + i2", new Dictionary<string, object>());
            var l3 = Evaluator.ToLambda<int, int>("(i1) => i1 + 2");
            var l4 = Evaluator.ToLambda<int, int>("(i1) => i1 + 2", new Dictionary<string, object>());
            var l5 = Evaluator.ToLambda<int>("() => 3");
            var l6 = Evaluator.ToLambda<int>("() => 3", new Dictionary<string, object>());
            var l7 = Evaluator.ToLambda("() => 3", Enumerable.Empty<Type>());
            var l8 = Evaluator.ToLambda("() => 3", Enumerable.Empty<Type>(), new Dictionary<string, object>());

            var f1 = Evaluator.ToFunc<int, int, int>("(i1, i2) => i1 + i2");
            Assert.Equal(3, f1(1, 2));

            var f2 = Evaluator.ToFunc<int, int, int>("(i1, i2) => i1 + i2", new Dictionary<string, object>());
            Assert.Equal(3, f2(1, 2));

            var f3 = Evaluator.ToFunc<int, int>("(i1) => i1 + 2");
            Assert.Equal(3, f3(1));

            var f4 = Evaluator.ToFunc<int, int>("(i1) => i1 + 2", new Dictionary<string, object>());
            Assert.Equal(3, f4(1));

            var f5 = Evaluator.ToFunc<int>("() => 3");
            Assert.Equal(3, f5());

            var f6 = Evaluator.ToFunc<int>("() => 3", new Dictionary<string, object>());
            Assert.Equal(3, f6());

            var f7 = Evaluator.ToFunc("() => 3", Enumerable.Empty<Type>());
            var f8 = Evaluator.ToFunc("() => 3", Enumerable.Empty<Type>(), new Dictionary<string, object>());
        }
    }
}
