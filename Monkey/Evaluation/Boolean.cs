namespace Monkey.Evaluation
{
    public class Boolean : IObject, IHashable
    {
        public const string ObjType = "BOOLEAN";

        public Boolean(bool value) => Value = value;

        public override int GetHashCode() => Value.GetHashCode();

        public string Inspect() => Value.ToString();

        public string Type() => ObjType;

        public bool Value { get; }
    }
}
