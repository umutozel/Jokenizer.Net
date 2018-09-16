using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Tokens {

    public class LambdaToken : Token {

        public LambdaToken(Token body, IEnumerable<string> parameters = null) : base(TokenType.Lambda) {
            Body = body;
            Parameters = parameters ?? Enumerable.Empty<string>();
        }

        public Token Body { get; }
        public IEnumerable<string> Parameters { get; }
    }
}
