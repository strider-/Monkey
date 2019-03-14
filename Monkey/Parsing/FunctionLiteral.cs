using Monkey.Lexing;
using System.Collections.Generic;
using System.Linq;

namespace Monkey.Parsing
{
    public class FunctionLiteral : BaseNode, IExpression
    {
        public FunctionLiteral(Token token) : base(token) { }

        public override string ToString() => $"{TokenLiteral()} ({string.Join(",", Parameters.Select(p => p.ToString()))}) {Body.ToString()}";

        public List<Identifier> Parameters { get; set; }

        public BlockStatement Body { get; set; }
    }
}
