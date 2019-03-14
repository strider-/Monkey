namespace Monkey.Evaluation
{
    public class Break : IObject
    {
        public const string ObjType = "BREAK";

        public string Inspect() => "break";

        public string Type() => ObjType;
    }
}
