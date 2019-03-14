using Monkey.Lexing;
using Xunit;

namespace Monkey.Tests
{
    [Trait("Lexing", "")]
    public class LexerTests
    {
        [Fact]
        public void NextToken_Returns_Expected_Token()
        {
            var lexer = new Lexer(Input());

            foreach(var expected in ExpectedTokens())
            {
                var token = lexer.NextToken();

                Assert.Equal(expected.Type, token.Type);
                Assert.Equal(expected.Literal, token.Literal);
            }
        }

        private string Input()
        {
            return @"let five = 5;
                    let ten = 10;
                    let add = fn(x, y) {
	                    x + y;
                    };
                    let result = add(five, ten);
                    !-/*5;
                    5 < 10 > 5;
                    if (5 < 10) {
	                    return true;
                    } else {
	                    return false;
                    }
                    10 == 10;
                    10 != 9;
                    ""foobar""
                    ""foo bar""
                    [1, 2];
                    { ""foo"": ""bar""}
                    macro(x, y) { x + y; };
                    let bad = ""bad";
        }

        private Token[] ExpectedTokens()
        {
            return new[]
            {
                new Token(Token.Let, "let"),
                new Token(Token.Ident, "five"),
                new Token(Token.Assign, "="),
                new Token(Token.Int, "5"),
                new Token(Token.Semicolon, ";"),
                new Token(Token.Let, "let"),
                new Token(Token.Ident, "ten"),
                new Token(Token.Assign, "="),
                new Token(Token.Int, "10"),
                new Token(Token.Semicolon, ";"),
                new Token(Token.Let, "let"),
                new Token(Token.Ident, "add"),
                new Token(Token.Assign, "="),
                new Token(Token.Function, "fn"),
                new Token(Token.LeftParen, "("),
                new Token(Token.Ident, "x"),
                new Token(Token.Comma, ","),
                new Token(Token.Ident, "y"),
                new Token(Token.RightParen, ")"),
                new Token(Token.LeftBrace, "{"),
                new Token(Token.Ident, "x"),
                new Token(Token.Plus, "+"),
                new Token(Token.Ident, "y"),
                new Token(Token.Semicolon, ";"),
                new Token(Token.RightBrace, "}"),
                new Token(Token.Semicolon, ";"),
                new Token(Token.Let, "let"),
                new Token(Token.Ident, "result"),
                new Token(Token.Assign, "="),
                new Token(Token.Ident, "add"),
                new Token(Token.LeftParen, "("),
                new Token(Token.Ident, "five"),
                new Token(Token.Comma, ","),
                new Token(Token.Ident, "ten"),
                new Token(Token.RightParen, ")"),
                new Token(Token.Semicolon, ";"),
                new Token(Token.Bang, "!"),
                new Token(Token.Minus, "-"),
                new Token(Token.Slash, "/"),
                new Token(Token.Asterisk, "*"),
                new Token(Token.Int, "5"),
                new Token(Token.Semicolon, ";"),
                new Token(Token.Int, "5"),
                new Token(Token.LessThan, "<"),
                new Token(Token.Int, "10"),
                new Token(Token.GreaterThan, ">"),
                new Token(Token.Int, "5"),
                new Token(Token.Semicolon, ";"),
                new Token(Token.If, "if"),
                new Token(Token.LeftParen, "("),
                new Token(Token.Int, "5"),
                new Token(Token.LessThan, "<"),
                new Token(Token.Int, "10"),
                new Token(Token.RightParen, ")"),
                new Token(Token.LeftBrace, "{"),
                new Token(Token.Return, "return"),
                new Token(Token.True, "true"),
                new Token(Token.Semicolon, ";"),
                new Token(Token.RightBrace, "}"),
                new Token(Token.Else, "else"),
                new Token(Token.LeftBrace, "{"),
                new Token(Token.Return, "return"),
                new Token(Token.False, "false"),
                new Token(Token.Semicolon, ";"),
                new Token(Token.RightBrace, "}"),
                new Token(Token.Int, "10"),
                new Token(Token.Equal, "=="),
                new Token(Token.Int, "10"),
                new Token(Token.Semicolon, ";"),
                new Token(Token.Int, "10"),
                new Token(Token.NotEqual, "!="),
                new Token(Token.Int, "9"),
                new Token(Token.Semicolon, ";"),
                new Token(Token.String, "foobar"),
                new Token(Token.String, "foo bar"),
                new Token(Token.LeftBracket, "["),
                new Token(Token.Int, "1"),
                new Token(Token.Comma, ","),
                new Token(Token.Int, "2"),
                new Token(Token.RightBracket, "]"),
                new Token(Token.Semicolon, ";"),
                new Token(Token.LeftBrace, "{"),
                new Token(Token.String, "foo"),
                new Token(Token.Colon, ":"),
                new Token(Token.String, "bar"),
                new Token(Token.RightBrace, "}"),
                new Token(Token.Macro, "macro"),
                new Token(Token.LeftParen, "("),
                new Token(Token.Ident, "x"),
                new Token(Token.Comma, ","),
                new Token(Token.Ident, "y"),
                new Token(Token.RightParen, ")"),
                new Token(Token.LeftBrace, "{"),
                new Token(Token.Ident, "x"),
                new Token(Token.Plus, "+"),
                new Token(Token.Ident, "y"),
                new Token(Token.Semicolon, ";"),
                new Token(Token.RightBrace, "}"),
                new Token(Token.Semicolon, ";"),
                new Token(Token.Let, "let"),
                new Token(Token.Ident, "bad"),
                new Token(Token.Assign, "="),
                new Token(Token.BadString, "\0"),
                new Token(Token.EOF, "\0"),
            };
        }
    }
}