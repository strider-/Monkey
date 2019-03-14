namespace Monkey.Evaluation
{
    public class String : IObject, IHashable
    {
        public const string ObjType = "STRING";

        public String(string value) => Value = value;

        public String(char value) => Value = value.ToString();

        public override int GetHashCode() => Value.GetHashCode();

        public string Inspect() => $"\"{Value}\"";

        public string Type() => ObjType;

        public string Value { get; }
    }
}
