using Monkey.Lexing;

namespace Monkey.Parsing
{
    public class PrefixExpression : BaseNode, IExpression
    {
        public PrefixExpression(Token token) : base(token) { }

        public override string ToString() => $"({Operator}{Right.ToString()})";

        public string Operator { get; set; }

        public IExpression Right { get; set; }
    }
}