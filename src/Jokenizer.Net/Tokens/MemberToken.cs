namespace Jokenizer.Net.Tokens;

public class MemberToken(Token owner, string name) : Token(TokenType.Member), IVariableToken {
    public Token Owner { get; } = owner;
    public string Name { get; } = name;
}
