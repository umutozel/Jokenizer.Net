namespace Jokenizer.Net.Tokens;

public class LiteralToken(object? value) : Token(TokenType.Literal) {
    public object? Value { get; } = value;
}
