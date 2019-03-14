using Monkey.Parsing;

namespace Monkey.Evaluation
{
    public class Quote : IObject
    {
        public const string ObjType = "QUOTE";

        public Quote(INode node) => Node = node;

        public string Inspect() => $"QUOTE({Node.ToString()})";

        public string Type() => ObjType;

        public INode Node { get; }
    }
}
