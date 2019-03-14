namespace Monkey.Lexing
{
    public class Token
    {
        public const string Macro = "MACRO";

        public const string Illegal = "ILLEGAL";
        public const string BadString = "BADSTRING";
        public const string EOF = "EOF";
        public const string Ident = "IDENT";
        public const string Int = "INT";
        public const string String = "STRING";
        public const string Assign = "=";
        public const string Plus = "+";
        public const string Minus = "-";
        public const string Bang = "!";
        public const string Asterisk = "*";
        public const string Slash = "/";
        public const string Modulus = "%";
        public const string LessThan = "<";
        public const string GreaterThan = ">";
        public const string Comma = ",";
        public const string Semicolon = ";";
        public const string LeftParen = "(";
        public const string RightParen = ")";
        public const string LeftBrace = "{";
        public const string RightBrace = "}";
        public const string LeftBracket = "[";
        public const string RightBracket = "]";
        public const string Colon = ":";

        public const string Equal = "==";
        public const string NotEqual = "!=";

        public const string AddAssign = "+=";
        public const string DiffAssign = "-=";
        public const string ProductAssign = "*=";
        public const string QuotientAssign = "/=";

        public const string Function = "FUNCTION";
        public const string Let = "LET";
        public const string True = "TRUE";
        public const string False = "FALSE";
        public const string If = "IF";
        public const string For = "FOR";
        public const string In = "IN";
        public const string Else = "ELSE";
        public const string Return = "RETURN";
        public const string Break = "BREAK";
        public const string Skip = "SKIP";

        public Token(string type, char chr) : this(type, chr.ToString()) { }

        public Token(string type, string literal)
        {
            Type = type;
            Literal = literal;
        }

        public string Type { get; }

        public string Literal { get; }

        public override string ToString() => $"Type:{Type} Literal:{Literal}";
    }
}
