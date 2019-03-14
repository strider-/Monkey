using System.Collections.Generic;
using System.Linq;

namespace Monkey.Parsing
{
    public class ProgramNode : INode
    {
        public string TokenLiteral()
        {
            if (Statements.Any())
            {
                return Statements.First().TokenLiteral();
            }

            return string.Empty;
        }

        public override string ToString() => $"{string.Join("", Statements.Select(stmt => stmt.ToString()))}";

        public List<IStatement> Statements { get; set; } = new List<IStatement>();
    }
}