using Monkey.Lexing;
using System.Collections.Generic;
using System.Linq;

namespace Monkey.Parsing
{
    public class ArrayLiteral : BaseNode, IExpression
    {
        public ArrayLiteral(Token token) : base(token) { }

        public override string ToString() => $"[{string.Join(", ", Elements.Select(e => e.ToString()))}]";

        public List<IExpression> Elements { get; set; }
    }
}
