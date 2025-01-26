namespace Jokenizer.Net.Tokens;

public class TernaryToken(Token predicate, Token whenTrue, Token whenFalse) : Token(TokenType.Ternary) {
    public Token Predicate { get; } = predicate;
    public Token WhenTrue { get; } = whenTrue;
    public Token WhenFalse { get; } = whenFalse;
}
