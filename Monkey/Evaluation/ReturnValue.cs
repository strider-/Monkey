namespace Monkey.Evaluation
{
    public class ReturnValue : IObject
    {
        public ReturnValue(IObject value) => Value = value;

        public const string ObjType = "RETURN_VALUE";

        public string Inspect() => Value.Inspect();

        public string Type() => ObjType;

        public IObject Value { get; }
    }
}
