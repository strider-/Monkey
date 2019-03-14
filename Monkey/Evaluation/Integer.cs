namespace Monkey.Evaluation
{
    public class Integer : IObject, IHashable
    {
        public const string ObjType = "INTEGER";

        public Integer(long value) => Value = value;

        public override int GetHashCode() => Value.GetHashCode();

        public string Inspect() => Value.ToString();

        public string Type() => ObjType;

        public long Value { get; }
    }
}
