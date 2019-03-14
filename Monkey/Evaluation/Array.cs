using System.Collections.Generic;
using System.Linq;

namespace Monkey.Evaluation
{
    public class Array : IObject
    {
        public const string ObjType = "ARRAY";

        public Array(IEnumerable<IObject> elements) => Elements = elements.ToArray();

        public string Inspect() => $"[{string.Join(", ", Elements.Select(e => e.Inspect()))}]";

        public string Type() => ObjType;

        public IObject[] Elements { get; }
    }
}
