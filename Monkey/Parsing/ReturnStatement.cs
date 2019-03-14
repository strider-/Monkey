using Monkey.Lexing;
using System.Text;

namespace Monkey.Parsing
{
    public class ReturnStatement : BaseNode, IStatement
    {
        public ReturnStatement(Token token) : base(token) { }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append($"{TokenLiteral()} ");

            if (ReturnValue != null)
            {
                sb.Append(ReturnValue.ToString());
            }

            sb.Append(";");

            return sb.ToString();
        }

        public IExpression ReturnValue { get; set; }
    }
}
