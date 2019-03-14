using Monkey.Lexing;

namespace Monkey.Parsing
{
    public class InfixExpression : BaseNode, IExpression
    {
        public InfixExpression(Token token) : base(token) { }

        public void ExpressionNode() { }

        public override string ToString() => $"({Left.ToString()} {Operator} {Right.ToString()})";

        public IExpression Left { get; set; }

        public string Operator { get; set; }

        public IExpression Right { get; set; }
    }
}
