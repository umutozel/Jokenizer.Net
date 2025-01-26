namespace Jokenizer.Net.Tokens;

public abstract class Token(TokenType type) {
    public TokenType Type { get; } = type;
}
