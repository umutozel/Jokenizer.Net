using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Tokens {

    public class GroupToken : Token {
        
        public GroupToken(IEnumerable<Token> tokens = null): base(TokenType.Group) {
            Tokens = tokens ?? Enumerable.Empty<Token>();
        }
                
        public IEnumerable<Token> Tokens { get; }
    }
}
