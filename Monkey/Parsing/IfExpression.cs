using Monkey.Lexing;
using System.Text;

namespace Monkey.Parsing
{
    public class IfExpression : BaseNode, IExpression
    {
        public IfExpression(Token token) : base(token) { }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append($"if {Condition.ToString()} {Consequence.ToString()}");

            if (Alternative != null)
            {
                sb.Append($" else {Alternative.ToString()}");
            }

            return sb.ToString();
        }

        public IExpression Condition { get; set; }

        public BlockStatement Consequence { get; set; }

        public BlockStatement Alternative { get; set; }
    }
}
