namespace Jokenizer.Net.Tokens;

public class AssignToken(string name, Token right) : Token(TokenType.Assign), IVariableToken {
    public string Name { get; } = name;
    public Token Right { get; } = right;
}
