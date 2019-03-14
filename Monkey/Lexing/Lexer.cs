using System;
using System.Collections.Generic;

namespace Monkey.Lexing
{
    public class Lexer
    {
        private Dictionary<string, string> Keywords = new Dictionary<string, string>
        {
            {"macro", Token.Macro},
            {"fn", Token.Function},
            {"let", Token.Let},
            {"true", Token.True},
            {"false", Token.False},
            {"if", Token.If},
            {"for", Token.For},
            {"in", Token.In},
            {"else", Token.Else},
            {"return", Token.Return},
            {"break", Token.Break},
            {"skip", Token.Skip}
        };

        private readonly string _input;
        private int _pos;
        private int _readPos;
        private char _ch;

        public Lexer(string input)
        {
            _input = input;
            ReadChar();
        }

        public Token NextToken()
        {
            Token token = null;

            SkipWhitespace();

            switch (_ch)
            {
                case '=':
                    if (PeekChar() == '=')
                    {
                        token = ConsumeAs(Token.Equal);
                    }
                    else
                    {
                        token = new Token(Token.Assign, _ch);
                    }
                    break;
                case ';':
                    token = new Token(Token.Semicolon, _ch);
                    break;
                case '(':
                    token = new Token(Token.LeftParen, _ch);
                    break;
                case ')':
                    token = new Token(Token.RightParen, _ch);
                    break;
                case ',':
                    token = new Token(Token.Comma, _ch);
                    break;
                case '+':
                    if (PeekChar() == '=')
                    {
                        token = ConsumeAs(Token.AddAssign);
                    }
                    else
                    {
                        token = new Token(Token.Plus, _ch);
                    }
                    break;
                case '-':
                    if (PeekChar() == '=')
                    {
                        token = ConsumeAs(Token.DiffAssign);
                    }
                    else
                    {
                        token = new Token(Token.Minus, _ch);
                    }
                    break;
                case '!':
                    if (PeekChar() == '=')
                    {
                        token = ConsumeAs(Token.NotEqual);
                    }
                    else
                    {
                        token = new Token(Token.Bang, _ch);
                    }
                    break;
                case '/':
                    if (PeekChar() == '=')
                    {
                        token = ConsumeAs(Token.QuotientAssign);
                    }
                    else
                    {
                        token = new Token(Token.Slash, _ch);
                    }
                    break;
                case '%':
                    token = new Token(Token.Modulus, _ch);
                    break;
                case '*':
                    if (PeekChar() == '=')
                    {
                        token = ConsumeAs(Token.ProductAssign);
                    }
                    else
                    {
                        token = new Token(Token.Asterisk, _ch);
                    }
                    break;
                case '<':
                    token = new Token(Token.LessThan, _ch);
                    break;
                case '>':
                    token = new Token(Token.GreaterThan, _ch);
                    break;
                case '{':
                    token = new Token(Token.LeftBrace, _ch);
                    break;
                case '}':
                    token = new Token(Token.RightBrace, _ch);
                    break;
                case '\0':
                    token = new Token(Token.EOF, _ch);
                    break;
                case '"':
                    var str = ReadString();
                    if (str == null)
                    {
                        token = new Token(Token.BadString, _ch);
                    }
                    else
                    {
                        token = new Token(Token.String, str);
                    }
                    break;
                case '[':
                    token = new Token(Token.LeftBracket, _ch);
                    break;
                case ']':
                    token = new Token(Token.RightBracket, _ch);
                    break;
                case ':':
                    token = new Token(Token.Colon, _ch);
                    break;
                default:
                    if (IsLetter(_ch))
                    {
                        var literal = ReadIdentifier();
                        var type = LookupIdent(literal);

                        token = new Token(type, literal);
                        return token;
                    }
                    else if (IsDigit(_ch))
                    {
                        var literal = ReadNumber();

                        token = new Token(Token.Int, literal);
                        return token;
                    }
                    else
                    {
                        token = new Token(Token.Illegal, _ch);
                    }
                    break;
            }

            ReadChar();
            return token;
        }

        private Token ConsumeAs(string token)
        {
            var chr = _ch;
            ReadChar();
            return new Token(token, $"{chr}{_ch}");
        }

        private string ReadIdentifier()
        {
            var position = _pos;
            while (IsLetter(_ch))
            {
                ReadChar();
            }
            return _input.Substring(position, _pos - position);
        }

        private string ReadNumber()
        {
            var position = _pos;
            while (IsDigit(_ch))
            {
                ReadChar();
            }
            return _input.Substring(position, _pos - position);
        }

        private string ReadString()
        {
            var position = _pos + 1;
            do
            {
                ReadChar();
                if (_ch == '\0')
                {
                    return null;
                }
            } while (_ch != '"');

            return _input.Substring(position, _pos - position);
        }

        private string LookupIdent(string ident)
        {
            if (Keywords.ContainsKey(ident))
            {
                return Keywords[ident];
            }

            return Token.Ident;
        }

        private bool IsLetter(char chr) => char.IsLetter(chr) || chr == '_';

        private bool IsDigit(char chr) => char.IsDigit(chr);

        private void ReadChar()
        {
            if (_readPos >= _input.Length)
            {
                _ch = '\0';
            }
            else
            {
                _ch = _input[_readPos];
            }
            _pos = _readPos;
            _readPos += 1;
        }

        private char PeekChar()
        {
            if (_pos >= _input.Length)
            {
                return '\0';
            }
            else
            {
                return _input[_readPos];
            }
        }

        private void SkipWhitespace()
        {
            while (char.IsWhiteSpace(_ch))
            {
                ReadChar();
            }
        }
    }
}
