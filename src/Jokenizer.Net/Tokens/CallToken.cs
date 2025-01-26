using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Tokens;

public class CallToken(Token callee, IEnumerable<Token>? args = null) : Token(TokenType.Call) {
    public Token Callee { get; } = callee;
    public Token[] Args { get; } = args == null ? [] : args.ToArray();
}
