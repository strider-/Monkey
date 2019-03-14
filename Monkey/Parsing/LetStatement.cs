using Monkey.Lexing;
using System.Text;

namespace Monkey.Parsing
{
    public class LetStatement : BaseNode, IStatement
    {
        public LetStatement(Token token) : base(token) { }

        public Identifier Name { get; set; }

        public IExpression Value { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append($"{TokenLiteral()} {Name.ToString()} = ");

            if (Value != null)
            {
                sb.Append(Value.ToString());
            }

            sb.Append(";");

            return sb.ToString();
        }
    }
}
