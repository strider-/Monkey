using Monkey.Lexing;
using System.Collections.Generic;
using System.Linq;

namespace Monkey.Parsing
{
    public class CallExpression : BaseNode, IExpression
    {
        public CallExpression(Token token) : base(token) { }

        public override string ToString() => $"{Function.ToString()}({string.Join(", ", Arguments.Select(arg => arg.ToString()))})";

        public IExpression Function { get; set; }

        public List<IExpression> Arguments { get; set; }
    }
}