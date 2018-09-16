using System.Collections.Generic;

namespace Jokenizer.Net.Tokens {

    public class ObjectToken : Token {

        public ObjectToken(IEnumerable<IVariableToken> members): base(TokenType.Object) {
            Members = members;
        }
        
        public IEnumerable<IVariableToken> Members { get; }
    }
}
