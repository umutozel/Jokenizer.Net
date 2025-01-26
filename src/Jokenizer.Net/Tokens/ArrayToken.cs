using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Tokens;

public class ArrayToken(IEnumerable<Token>? items) : Token(TokenType.Array) {
    public Token[] Items { get; } = items == null ? [] : items.ToArray();
}
