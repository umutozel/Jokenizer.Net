namespace Jokenizer.Net;

public class BinaryOperatorInfo {

    internal BinaryOperatorInfo(double precedence, BinaryExpressionConverter expressionConverter) {
        Precedence = precedence;
        ExpressionConverter = expressionConverter;
    }

    // Fractional precedence lets the bitwise tier split (& tighter than ^
    // tighter than |) between the surrounding integer slots without
    // renumbering — any custom operator registered at the default 7 stays
    // tighter than every built-in, same as before.
    public double Precedence { get; }
    public BinaryExpressionConverter ExpressionConverter { get; }
}
