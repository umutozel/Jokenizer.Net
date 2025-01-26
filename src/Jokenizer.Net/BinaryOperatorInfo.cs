namespace Jokenizer.Net;

public class BinaryOperatorInfo {

    internal BinaryOperatorInfo(byte precedence, BinaryExpressionConverter expressionConverter) {
        Precedence = precedence;
        ExpressionConverter = expressionConverter;
    }

    public byte Precedence { get; }
    public BinaryExpressionConverter ExpressionConverter { get; }
}
