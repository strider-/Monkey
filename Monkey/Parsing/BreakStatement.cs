using Monkey.Lexing;

namespace Monkey.Parsing
{
    public class BreakStatement : BaseNode, IStatement
    {
        public BreakStatement(Token token) : base(token) { }

        public override string ToString() => $"{TokenLiteral()};";
    }
}
