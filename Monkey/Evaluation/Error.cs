namespace Monkey.Evaluation
{
    public class Error : IObject
    {
        public Error(string msg) => Message = msg;

        public const string ObjType = "ERROR";

        public string Inspect() => $"ERROR: {Message}";

        public string Type() => ObjType;

        public string Message { get; }
    }
}
