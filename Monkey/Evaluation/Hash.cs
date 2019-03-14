using System.Collections.Generic;
using System.Linq;

namespace Monkey.Evaluation
{
    public class Hash : IObject
    {
        public const string ObjType = "HASH";

        public Hash(Dictionary<long, KeyValuePair<IObject, IObject>> pairs) => Pairs = pairs;

        public string Inspect() => $"{{{string.Join(", ", Pairs.Select(kvp => $"{kvp.Value.Key.Inspect()}: {kvp.Value.Value.Inspect()}"))}}}";

        public string Type() => ObjType;

        public Dictionary<long, KeyValuePair<IObject, IObject>> Pairs { get; }
    }
}
