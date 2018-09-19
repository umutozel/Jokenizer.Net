namespace Jokenizer.Net.Tokens {

    public class MemberToken : Token {

        public MemberToken(Token owner, string member) : base(TokenType.Member) {
            Owner = owner;
            Member = member;
        }

        public Token Owner { get; }
        public string Member { get; }
    }
}
