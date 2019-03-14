namespace Monkey.Evaluation
{
    public class Null : IObject
    {
        public const string ObjType = "NULL";

        public string Inspect() => "null";

        public string Type() => ObjType;
    }
}
