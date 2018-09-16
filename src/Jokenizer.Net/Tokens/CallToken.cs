using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Tokens {

    public class CallToken : Token {

        public CallToken(Token callee, IEnumerable<Token> args = null) : base(TokenType.Call) {
            Callee = callee;
            Args = args ?? Enumerable.Empty<Token>();
        }
        
        public Token Callee { get; }
        public IEnumerable<Token> Args { get; }
    }
}
