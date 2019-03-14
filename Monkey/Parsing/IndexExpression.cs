using Monkey.Lexing;

namespace Monkey.Parsing
{
    public class IndexExpression : BaseNode, IExpression
    {
        public IndexExpression(Token token) : base(token) { }

        public override string ToString() => $"({Left.ToString()}[{Index.ToString()}])";

        public IExpression Left { get; set; }

        public IExpression Index { get; set; }
    }
}
