namespace Monkey.Evaluation
{
    public class Skip : IObject
    {
        public const string ObjType = "SKIP";

        public string Inspect() => "skip";

        public string Type() => ObjType;
    }
}
