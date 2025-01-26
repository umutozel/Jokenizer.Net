using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Tokens;

public class LambdaToken(Token body, IEnumerable<string>? parameters = null) : Token(TokenType.Lambda) {
    public Token Body { get; } = body;
    public string[] Parameters { get; } = parameters == null ? [] : parameters.ToArray();
}
