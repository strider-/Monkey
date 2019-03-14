using Monkey.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace Monkey.Evaluation
{
    public class Macro : IObject
    {
        public const string ObjType = "MACRO";

        public Macro(List<Identifier> parameters, BlockStatement body, Environment env)
        {
            Parameters = parameters;
            Body = body;
            Environment = env;
        }

        public string Inspect() => $"macro ({string.Join(", ", Parameters.Select(p => p.ToString()))}) {{\n{Body.ToString()}\n}}";

        public string Type() => ObjType;

        public List<Identifier> Parameters { get; }

        public BlockStatement Body { get; }

        public Environment Environment { get; }
    }
}
