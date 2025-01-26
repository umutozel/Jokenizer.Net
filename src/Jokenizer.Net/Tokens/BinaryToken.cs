namespace Jokenizer.Net.Tokens;

public class BinaryToken(string op, Token left, Token right) : Token(TokenType.Binary) {
    public string Operator { get; } = op;
    public Token Left { get; } = left;
    public Token Right { get; } = right;
}
