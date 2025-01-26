namespace Jokenizer.Net.Tokens;

public class IndexerToken(Token owner, Token key) : Token(TokenType.Indexer) {
    public Token Owner { get; } = owner;
    public Token Key { get; } = key;
}
