namespace Jokenizer.Net.Tokens;

public class UnaryToken(char op, Token target) : Token(TokenType.Unary) {
    public char Operator { get; } = op;
    public Token Target { get; } = target;
}
