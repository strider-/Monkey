using Monkey.Lexing;

namespace Monkey.Parsing
{
    public class ExpressionStatement : BaseNode, IStatement
    {
        public ExpressionStatement(Token token) : base(token) { }

        public override string ToString() => Expression == null ? string.Empty : Expression.ToString();

        public IExpression Expression { get; set; }
    }
}
