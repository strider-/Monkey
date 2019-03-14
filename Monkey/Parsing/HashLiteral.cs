using Monkey.Lexing;
using System.Collections.Generic;
using System.Linq;

namespace Monkey.Parsing
{
    public class HashLiteral : BaseNode, IExpression
    {
        public HashLiteral(Token token) : base(token) { }

        public override string ToString() => $"{{{string.Join(", ", Pairs.Select(p => $"{p.Key.ToString()}: {p.Value.ToString()}"))}}}";

        public Dictionary<IExpression, IExpression> Pairs { get; set; } = new Dictionary<IExpression, IExpression>();
    }
}
