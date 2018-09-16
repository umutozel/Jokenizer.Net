using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Tokens {

    public class ObjectToken : Token {

        public ObjectToken(IEnumerable<IVariableToken> members): base(TokenType.Object) {
            Members = members == null ? new IVariableToken[0] : members.ToArray();
        }
        
        public IVariableToken[] Members { get; }
    }
}
