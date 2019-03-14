using Monkey.Lexing;
using System.Text;

namespace Monkey.Parsing
{
    public class ForExpression : BaseNode, IExpression
    {
        public ForExpression(Token token) : base(token) { }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (ValueIdentifier == null)
            {
                sb.Append($"for {KeyIdentifier.ToString()}");
            }
            else
            {
                sb.Append($"for {KeyIdentifier.ToString()}, {ValueIdentifier.ToString()}");
            }

            sb.Append($" in {Collection.ToString()} {{\n{Body.ToString()}\n}}");

            return sb.ToString();
        }

        public Identifier KeyIdentifier { get; set; }

        public Identifier ValueIdentifier { get; set; }

        public IExpression Collection { get; set; }

        public BlockStatement Body { get; set; }
    }
}
