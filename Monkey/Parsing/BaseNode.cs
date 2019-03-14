using Monkey.Lexing;

namespace Monkey.Parsing
{
    public abstract class BaseNode : INode
    {
        protected readonly Token Token;

        public BaseNode(Token token) => Token = token;

        public override abstract string ToString();

        public string TokenLiteral() => Token.Literal;
    }
}