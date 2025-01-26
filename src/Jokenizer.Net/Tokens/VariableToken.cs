namespace Jokenizer.Net.Tokens;

public class VariableToken(string name) : Token(TokenType.Variable), IVariableToken {
    public string Name { get; } = name;
}
