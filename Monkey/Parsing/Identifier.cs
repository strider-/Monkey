using Monkey.Lexing;

namespace Monkey.Parsing
{
    public class Identifier : BaseNode, IExpression
    {
        public Identifier(Token token) : base(token) { }

        public override string ToString() => Value;

        public string Value { get; set; }
    }
}
