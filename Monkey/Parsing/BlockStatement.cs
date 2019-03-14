using Monkey.Lexing;
using System.Collections.Generic;
using System.Linq;

namespace Monkey.Parsing
{
    public class BlockStatement : BaseNode, IStatement
    {
        public BlockStatement(Token token) : base(token) { }

        public override string ToString() => $"{string.Join(" ", Statements.Select(stmt => stmt.ToString()))}";

        public bool IsEmpty() => !Statements.Any();

        public List<IStatement> Statements { get; set; } = new List<IStatement>();
    }
}
