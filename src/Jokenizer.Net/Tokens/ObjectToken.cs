using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Tokens;

public class ObjectToken(IEnumerable<AssignToken>? members) : Token(TokenType.Object) {
    public AssignToken[] Members { get; } = members == null ? [] : members.ToArray();
}
