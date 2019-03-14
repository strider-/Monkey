using Monkey.Lexing;

namespace Monkey.Parsing
{
    public class SkipStatement : BaseNode, IStatement
    {
        public SkipStatement(Token token) : base(token) { }

        public override string ToString() => $"{TokenLiteral()};";
    }
}
