using Monkey.Lexing;

namespace Monkey.Parsing
{
    public class BooleanLiteral : BaseNode, IExpression
    {
        public BooleanLiteral(Token token) : base(token) { }

        public override string ToString() => TokenLiteral();

        public bool Value { get; set; }
    }
}
