using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Tokens {

    public class ArrayToken : Token {

        public ArrayToken(IEnumerable<Token> items = null): base(TokenType.Array) {
            Items = items == null ? new Token[0] : items.ToArray();
        }
        
        public Token[] Items { get; }
    }
}
