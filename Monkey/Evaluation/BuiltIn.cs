namespace Monkey.Evaluation
{
    public delegate IObject BuiltInDelegate(params IObject[] args);

    public class BuiltIn : IObject
    {
        public BuiltIn(BuiltInDelegate fn) => Fn = fn;

        public const string ObjType = "BUILTIN";

        public string Inspect() => "built-in function";

        public string Type() => ObjType;

        public BuiltInDelegate Fn { get; }
    }
}
