using Monkey.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace Monkey.Evaluation
{
    public class Function : IObject
    {
        public const string ObjType = "FUNCTION";

        public Function(IEnumerable<Identifier> parameters, BlockStatement body, Environment env)
        {
            Parameters = parameters;
            Body = body;
            Environment = env;
        }

        public string Inspect() => $"fn ({string.Join(", ", Parameters.Select(p => p.ToString()))}) {{\n{Body.ToString()}\n}}";

        public string Type() => ObjType;

        public IEnumerable<Identifier> Parameters { get; }

        public BlockStatement Body { get; }

        public Environment Environment { get; }
    }
}