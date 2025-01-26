using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Tokens;

public class GroupToken(IEnumerable<Token>? tokens = null) : Token(TokenType.Group) {
    public Token[] Tokens { get; } = tokens == null ? [] : tokens.ToArray();
}
