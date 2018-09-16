namespace Jokenizer.Net.Tokens {

    public class UnaryToken : Token {

        public UnaryToken(string op, Token target): base(TokenType.Unary) {
            Operator = op;
            Target = target;
        }
        
        public string Operator { get; }
        public Token Target { get; }
    }
}
