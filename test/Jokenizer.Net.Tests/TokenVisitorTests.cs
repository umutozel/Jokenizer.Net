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
    }
}
