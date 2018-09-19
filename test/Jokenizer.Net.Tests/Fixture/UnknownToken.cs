using Jokenizer.Net.Tokens;

namespace Jokenizer.Net.Tests.Fixture {

    public class UnkownToken : Token {

        public UnkownToken() : base((TokenType)42) {
        }
    }
}
