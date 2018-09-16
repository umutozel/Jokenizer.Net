using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Tokens {

    public class ArrayToken : Token {

        public ArrayToken(IEnumerable<Token> items = null): base(TokenType.Array) {
            Items = items ?? Enumerable.Empty<Token>();
        }
        
        public IEnumerable<Token> Items { get; }
    }
}
