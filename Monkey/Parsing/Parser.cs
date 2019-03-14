using Monkey.Lexing;
using System;
using System.Collections.Generic;

namespace Monkey.Parsing
{
    public class Parser
    {
        const int Lowest = 1;
        const int Equal = 2;
        const int LessOrGreater = 3;
        const int Sum = 4;
        const int Product = 5;
        const int Prefix = 6;
        const int Call = 7;
        const int Index = 8;

        private readonly Lexer _lexer;
        private Token _current;
        private Token _peek;
        private Dictionary<string, Func<IExpression>> _prefixParse = new Dictionary<string, Func<IExpression>>();
        private Dictionary<string, Func<IExpression, IExpression>> _infixParse = new Dictionary<string, Func<IExpression, IExpression>>();

        Dictionary<string, int> _precedences = new Dictionary<string, int>
        {
            {Token.Equal, Equal},
            {Token.NotEqual, Equal},
            {Token.LessThan, LessOrGreater},
            {Token.GreaterThan, LessOrGreater},
            {Token.Plus, Sum},
            {Token.Minus, Sum},
            {Token.Slash, Product},
            {Token.Asterisk, Product},
            {Token.Modulus, Product},
            {Token.LeftParen, Call},
            {Token.LeftBracket, Index}
        };

        public Parser(Lexer lexer)
        {
            _lexer = lexer;

            RegisterPrefix(Token.Ident, ParseIdentifier);
            RegisterPrefix(Token.Int, ParseIntegerLiteral);
            RegisterPrefix(Token.Bang, ParsePrefixExpression);
            RegisterPrefix(Token.Minus, ParsePrefixExpression);
            RegisterPrefix(Token.True, ParseBooleanLiteral);
            RegisterPrefix(Token.False, ParseBooleanLiteral);
            RegisterPrefix(Token.LeftParen, ParseGroupedExpression);
            RegisterPrefix(Token.If, ParseIfExpression);
            RegisterPrefix(Token.For, ParseForExpression);
            RegisterPrefix(Token.Function, ParseFunctionLiteral);
            RegisterPrefix(Token.String, ParseStringLiteral);
            RegisterPrefix(Token.LeftBracket, ParseArrayLiteral);
            RegisterPrefix(Token.LeftBrace, ParseHashLiteral);
            RegisterPrefix(Token.Macro, ParseMacroLiteral);

            RegisterInfix(Token.LeftBracket, ParseIndexExpression);
            RegisterInfix(Token.Plus, ParseInfixExpression);
            RegisterInfix(Token.Minus, ParseInfixExpression);
            RegisterInfix(Token.Slash, ParseInfixExpression);
            RegisterInfix(Token.Modulus, ParseInfixExpression);
            RegisterInfix(Token.Asterisk, ParseInfixExpression);
            RegisterInfix(Token.Equal, ParseInfixExpression);
            RegisterInfix(Token.NotEqual, ParseInfixExpression);
            RegisterInfix(Token.LessThan, ParseInfixExpression);
            RegisterInfix(Token.GreaterThan, ParseInfixExpression);
            RegisterInfix(Token.LeftParen, ParseCallExpression);
            
            NextToken();
            NextToken();
        }

        public static INode Modify(INode node, Func<INode, INode> modifier)
        {
            switch (node)
            {
                case ProgramNode pn:
                    for(int i=0; i<pn.Statements.Count; i++)
                    {
                        pn.Statements[i] = Modify(pn.Statements[i], modifier) as IStatement;
                    }
                    break;
                case ExpressionStatement es:
                    es.Expression = Modify(es.Expression, modifier) as IExpression;
                    break;
                case InfixExpression infix:
                    infix.Left = Modify(infix.Left, modifier) as IExpression;
                    infix.Right = Modify(infix.Right, modifier) as IExpression;
                    break;
                case PrefixExpression prefix:
                    prefix.Right = Modify(prefix.Right, modifier) as IExpression;
                    break;
                case IndexExpression index:
                    index.Left = Modify(index.Left, modifier) as IExpression;
                    index.Index = Modify(index.Index, modifier) as IExpression;
                    break;
                case IfExpression iif:
                    iif.Condition = Modify(iif.Condition, modifier) as IExpression;
                    iif.Consequence = Modify(iif.Consequence, modifier) as BlockStatement;
                    if(iif.Alternative != null)
                    {
                        iif.Alternative = Modify(iif.Alternative, modifier) as BlockStatement;
                    }
                    break;
                case ForExpression ffor:
                    ffor.Body = Modify(ffor.Body, modifier) as BlockStatement;
                    ffor.Collection = Modify(ffor.Collection, modifier) as IExpression;
                    ffor.KeyIdentifier = Modify(ffor.KeyIdentifier, modifier) as Identifier;
                    if(ffor.ValueIdentifier != null)
                    {
                        ffor.ValueIdentifier = Modify(ffor.ValueIdentifier, modifier) as Identifier;
                    }
                    break;
                case BlockStatement block:
                    for(int i = 0; i<block.Statements.Count; i++)
                    {
                        block.Statements[i] = Modify(block.Statements[i], modifier) as IStatement;
                    }
                    break;
                case ReturnStatement ret:
                    ret.ReturnValue = Modify(ret.ReturnValue, modifier) as IExpression;
                    break;
                case LetStatement let:
                    let.Value = Modify(let.Value, modifier) as IExpression;
                    break;
                case FunctionLiteral func:
                    for (int i = 0; i < func.Parameters.Count; i++)
                    {
                        func.Parameters[i] = Modify(func.Parameters[i], modifier) as Identifier;
                    }
                    func.Body = Modify(func.Body, modifier) as BlockStatement;
                    break;
                case ArrayLiteral array:
                    for (int i = 0; i < array.Elements.Count; i++)
                    {
                        array.Elements[i] = Modify(array.Elements[i], modifier) as IExpression;
                    }
                    break;
                case HashLiteral hash:
                    var dict = new Dictionary<IExpression, IExpression>();
                    foreach(var kvp in hash.Pairs)
                    {
                        var newKey = Modify(kvp.Key, modifier) as IExpression;
                        var newVal = Modify(kvp.Value, modifier) as IExpression;
                        dict[newKey] = newVal;
                    }
                    hash.Pairs = dict;
                    break;
            }

            return modifier(node);
        }

        public ProgramNode ParseProgram()
        {
            var program = new ProgramNode();

            while (!CurrentTokenIs(Token.EOF))
            {
                var statement = ParseStatement();
                if (statement != null)
                {
                    program.Statements.Add(statement);
                }
                NextToken();
            }

            return program;
        }

        private void NextToken()
        {
            _current = _peek;
            _peek = _lexer.NextToken();
        }

        private IStatement ParseStatement()
        {
            switch (_current.Type)
            {
                case Token.Let:
                    return ParseLetStatement();
                case Token.Return:
                    return ParseReturnStatement();
                case Token.Break:
                    return ParseBreakStatement();
                case Token.Skip:
                    return ParseSkipStatement();
                default:
                    if (_current.Type == Token.Ident && PeekTokenIsAssignmentOperator())
                    {
                        return ParseAssignmentOperatorStatement();
                    }
                    return ParseExpressionStatement();
            }
        }

        private LetStatement ParseAssignmentOperatorStatement()
        {
            var statement = new LetStatement(new Token(Token.Let, "let"));
            var ident = new Identifier(_current) { Value = _current.Literal };

            statement.Name = ident;
            statement.Value = AssignmentOperatorInfixExpression(ident);

            if (PeekTokenIs(Token.Semicolon))
            {
                NextToken();
            }

            return statement;
        }

        private void RegisterPrefix(string tokenType, Func<IExpression> prefixFunc)
        {
            _prefixParse[tokenType] = prefixFunc;
        }

        private void RegisterInfix(string tokenType, Func<IExpression, IExpression> infixFunc)
        {
            _infixParse[tokenType] = infixFunc;
        }

        private IExpression ParseIndexExpression(IExpression left)
        {
            var exp = new IndexExpression(_current) { Left = left };

            NextToken();
            exp.Index = ParseExpression(Lowest);

            if (!ExpectPeek(Token.RightBracket))
            {
                return null;
            }

            return exp;
        }

        private LetStatement ParseLetStatement()
        {
            var statement = new LetStatement(_current);

            if (!ExpectPeek(Token.Ident))
            {
                return null;
            }

            statement.Name = new Identifier(_current) { Value = _current.Literal };

            if (PeekTokenIsAssignmentOperator())
            {
                statement.Value = AssignmentOperatorInfixExpression(statement.Name);
            }
            else
            {
                if (!ExpectPeek(Token.Assign))
                {
                    return null;
                }

                NextToken();

                statement.Value = ParseExpression(Lowest);
            }

            if (PeekTokenIs(Token.Semicolon))
            {
                NextToken();
            }

            return statement;
        }

        private InfixExpression AssignmentOperatorInfixExpression(Identifier left)
        {
            NextToken();

            var op = _current.Literal[0].ToString();
            var infix = new InfixExpression(_current)
            {
                Left = left,
                Operator = op
            };

            NextToken();
            infix.Right = ParseExpression(Lowest);

            return infix;
        }

        private ReturnStatement ParseReturnStatement()
        {
            var statement = new ReturnStatement(_current);

            NextToken();

            statement.ReturnValue = ParseExpression(Lowest);

            if (PeekTokenIs(Token.Semicolon))
            {
                NextToken();
            }

            return statement;
        }

        private BreakStatement ParseBreakStatement()
        {
            var statement = new BreakStatement(_current);

            if (PeekTokenIs(Token.Semicolon))
            {
                NextToken();
            }

            return statement;
        }

        private SkipStatement ParseSkipStatement()
        {
            var statement = new SkipStatement(_current);

            if (PeekTokenIs(Token.Semicolon))
            {
                NextToken();
            }

            return statement;
        }

        private ExpressionStatement ParseExpressionStatement()
        {
            var statement = new ExpressionStatement(_current);
            statement.Expression = ParseExpression(Lowest);

            if (PeekTokenIs(Token.Semicolon))
            {
                NextToken();
            }

            return statement;
        }

        private IExpression ParseExpression(int precedence)
        {
            if (_current.Type == Token.BadString)
            {
                Errors.Add("Invalid string detected (did you forget an ending \"?)");
                return null;
            }

            if (!_prefixParse.ContainsKey(_current.Type))
            {
                NoPrefixParseError(_current.Type);
                return null;
            }

            var left = _prefixParse[_current.Type]();

            while (!PeekTokenIs(Token.Semicolon) && precedence < PeekPrecedence())
            {
                if (!_infixParse.ContainsKey(_peek.Type))
                {
                    return left;
                }
                var fn = _infixParse[_peek.Type];

                NextToken();
                left = fn(left);
            }

            return left;
        }

        private void NoPrefixParseError(string expected)
        {
            Errors.Add($"no prefix parse function for {expected} found");
        }

        private IExpression ParseIdentifier()
        {
            return new Identifier(_current) { Value = _current.Literal };
        }

        private IExpression ParseIntegerLiteral()
        {
            var intLit = new IntegerLiteral(_current);

            if (!long.TryParse(_current.Literal, out var value))
            {
                Errors.Add($"could not parse {_current.Literal} as integer");
                return null;
            }

            intLit.Value = value;
            return intLit;
        }

        private IExpression ParseArrayLiteral()
        {
            var array = new ArrayLiteral(_current);

            array.Elements = ParseExpressionList(Token.RightBracket);

            return array;
        }

        private IExpression ParseHashLiteral()
        {
            var hash = new HashLiteral(_current);

            while (!PeekTokenIs(Token.RightBrace))
            {
                NextToken();
                var key = ParseExpression(Lowest);

                if (!ExpectPeek(Token.Colon))
                {
                    return null;
                }

                NextToken();
                var value = ParseExpression(Lowest);

                hash.Pairs[key] = value;

                if (!PeekTokenIs(Token.RightBrace) && !ExpectPeek(Token.Comma))
                {
                    return null;
                }
            }

            if (!ExpectPeek(Token.RightBrace))
            {
                return null;
            }

            return hash;
        }

        private IExpression ParseStringLiteral() => new StringLiteral(_current);

        private IExpression ParseBooleanLiteral() => new BooleanLiteral(_current) { Value = CurrentTokenIs(Token.True) };

        private List<IExpression> ParseExpressionList(string endToken)
        {
            var list = new List<IExpression>();

            if (PeekTokenIs(endToken))
            {
                NextToken();
                return list;
            }

            NextToken();
            list.Add(ParseExpression(Lowest));

            while (PeekTokenIs(Token.Comma))
            {
                NextToken();
                NextToken();
                list.Add(ParseExpression(Lowest));
            }

            if (!ExpectPeek(endToken))
            {
                return null;
            }

            return list;
        }

        private IExpression ParseGroupedExpression()
        {
            NextToken();

            var expression = ParseExpression(Lowest);

            if (!ExpectPeek(Token.RightParen))
            {
                return null;
            }

            return expression;
        }

        private IExpression ParseIfExpression()
        {
            var expression = new IfExpression(_current);

            if (!ExpectPeek(Token.LeftParen))
            {
                return null;
            }

            NextToken();
            expression.Condition = ParseExpression(Lowest);

            if (!ExpectPeek(Token.RightParen))
            {
                return null;
            }

            if (!ExpectPeek(Token.LeftBrace))
            {
                return null;
            }

            expression.Consequence = ParseBlockStatement();

            if (PeekTokenIs(Token.Else))
            {
                NextToken();
                if (!ExpectPeek(Token.LeftBrace))
                {
                    return null;
                }

                expression.Alternative = ParseBlockStatement();
            }

            return expression;
        }

        private IExpression ParseForExpression()
        {
            var expression = new ForExpression(_current);

            if (!ExpectPeek(Token.LeftParen))
            {
                return null;
            }

            NextToken();
            expression.KeyIdentifier = (Identifier)ParseIdentifier();

            if (PeekTokenIs(Token.Comma))
            {
                NextToken();
                NextToken();
                expression.ValueIdentifier = (Identifier)ParseIdentifier();
            }

            if (!ExpectPeek(Token.In))
            {
                return null;
            }

            NextToken();
            expression.Collection = ParseExpression(Lowest);

            if (!ExpectPeek(Token.RightParen))
            {
                return null;
            }

            if (!ExpectPeek(Token.LeftBrace))
            {
                return null;
            }

            expression.Body = ParseBlockStatement();

            return expression;
        }

        private BlockStatement ParseBlockStatement()
        {
            var block = new BlockStatement(_current);

            NextToken();

            while (!CurrentTokenIs(Token.RightBrace))
            {
                var statement = ParseStatement();
                if (statement != null)
                {
                    block.Statements.Add(statement);
                }
                NextToken();
            }

            return block;
        }

        private IExpression ParsePrefixExpression()
        {
            var expression = new PrefixExpression(_current) { Operator = _current.Literal };

            NextToken();

            expression.Right = ParseExpression(Prefix);

            return expression;
        }

        private IExpression ParseInfixExpression(IExpression left)
        {
            var expression = new InfixExpression(_current)
            {
                Operator = _current.Literal,
                Left = left,
            };

            var precedence = CurrentPrecedence();
            NextToken();
            expression.Right = ParseExpression(precedence);

            return expression;
        }

        private IExpression ParseFunctionLiteral()
        {
            var fnLit = new FunctionLiteral(_current);

            if (!ExpectPeek(Token.LeftParen))
            {
                return null;
            }

            fnLit.Parameters = ParseFunctionParameters();

            if (!ExpectPeek(Token.LeftBrace))
            {
                return null;
            }

            fnLit.Body = ParseBlockStatement();

            return fnLit;
        }

        private List<Identifier> ParseFunctionParameters()
        {
            var idents = new List<Identifier>();

            if (PeekTokenIs(Token.RightParen))
            {
                NextToken();
                return idents;
            }

            NextToken();

            var ident = new Identifier(_current) { Value = _current.Literal };
            idents.Add(ident);

            while (PeekTokenIs(Token.Comma))
            {
                NextToken();
                NextToken();
                ident = new Identifier(_current) { Value = _current.Literal };
                idents.Add(ident);
            }

            if (!ExpectPeek(Token.RightParen))
            {
                return null;
            }

            return idents;
        }

        private IExpression ParseCallExpression(IExpression fn)
        {
            var expression = new CallExpression(_current) { Function = fn };
            expression.Arguments = ParseExpressionList(Token.RightParen);
            return expression;
        }

        private IExpression ParseMacroLiteral()
        {
            var macro = new MacroLiteral(_current);

            if (!ExpectPeek(Token.LeftParen))
            {
                return null;
            }

            macro.Parameters = ParseFunctionParameters();

            if (!ExpectPeek(Token.LeftBrace))
            {
                return null;
            }

            macro.Body = ParseBlockStatement();

            return macro;
        }

        private int PeekPrecedence()
        {
            if (_precedences.ContainsKey(_peek.Type))
            {
                return _precedences[_peek.Type];
            }

            return Lowest;
        }

        private int CurrentPrecedence()
        {
            if (_precedences.ContainsKey(_current.Type))
            {
                return _precedences[_current.Type];
            }

            return Lowest;
        }

        private bool CurrentTokenIs(string expected) => _current.Type == expected;
        
        private bool PeekTokenIs(string expected) => _peek.Type == expected;

        private bool PeekTokenIsAssignmentOperator() => PeekTokenIs(Token.AddAssign) ||
                                                        PeekTokenIs(Token.DiffAssign) ||
                                                        PeekTokenIs(Token.ProductAssign) ||
                                                        PeekTokenIs(Token.QuotientAssign);

        private bool ExpectPeek(string expected)
        {
            if (PeekTokenIs(expected))
            {
                NextToken();
                return true;
            }

            PeekError(expected);
            return false;
        }

        private void PeekError(string expected)
        {
            Errors.Add($"expected next token to be '{expected}', got '{_peek.Type}' instead.");
        }

        public List<string> Errors { get; } = new List<string>();
    }
}
