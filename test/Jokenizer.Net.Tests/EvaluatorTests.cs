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
        public void ShouldEvaluateInterpolatedString() {
            var v1 = Evaluator.ToFunc<string>("\"4\\\"2\"");
            Assert.Equal("4\"2", v1());

            var v2 = Evaluator.ToFunc<string>("\"\\a\\b\\f\\n\\r\\t\\v\\0\\\"\\\\\"");
            Assert.Equal("\a\b\f\n\r\t\v\0\"\\", v2());
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
        }

        [Fact]
        public void ShouldEvaluateObject() {
            var v = Evaluator.ToFunc<dynamic>("new { a = 4, b = @0 }", 2);
            var o = v();

            Assert.Equal(4, o.a);
            Assert.Equal(2, o.b);
        }

        [Fact]
        public void ShouldEvaluateMember() {
            var v1 = Evaluator.ToFunc<string>("Company.Name", new Dictionary<string, object> { { "Company", new { Name = "Rick" } } });

            Assert.Equal("Rick", v1());

            var v2 = Evaluator.ToFunc<string>("@0.Name", new { Name = "Rick" });

            Assert.Equal("Rick", v2());
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
        }
    }
}
