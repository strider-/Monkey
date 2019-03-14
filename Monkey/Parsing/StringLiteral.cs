using Monkey.Lexing;

namespace Monkey.Parsing
{
    public class StringLiteral : BaseNode, IExpression
    {
        public StringLiteral(Token token) : base(token) { }

        public override string ToString() => TokenLiteral();

        public string Value => TokenLiteral();
    }
}
