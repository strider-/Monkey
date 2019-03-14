using Monkey.Lexing;

namespace Monkey.Parsing
{
    public class IntegerLiteral : BaseNode, IExpression
    {
        public IntegerLiteral(Token token) : base(token) { }

        public override string ToString() => TokenLiteral();

        public long Value { get; set; }
    }
}
