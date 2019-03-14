using Monkey.Lexing;
using Monkey.Parsing;
using System;
using System.Collections.Generic;
using Xunit;

namespace Monkey.Tests
{
    [Trait("Parsing", "")]
    public class ParserTests
    {
        [Theory]
        [InlineData("let x = 5;", "x", 5L)]
        [InlineData("let y = true;", "y", true)]
        [InlineData("let foobar = y;", "foobar", "y")]
        public void LetStatements_Are_Parsed_Correctly(string input, string expectedIdentifier, object expectedValue)
        {
            var program = Initialize(input);

            Assert.Equal(1, program.Statements.Count);
            var letStmt = Assert.IsType<LetStatement>(program.Statements[0]);
            Assert.Equal(expectedIdentifier, letStmt.Name.Value);
            AssertExpression(expectedValue, letStmt.Value);
        }

        [Fact]
        public void ReturnStatements_Are_Parsed_Correctly()
        {
            var input = "return 5;\nreturn 10;\nreturn 993322;";
            var program = Initialize(input);

            Assert.Equal(3, program.Statements.Count);

            foreach(var stmt in program.Statements)
            {
                var ret = Assert.IsType<ReturnStatement>(stmt);
                Assert.Equal("return", ret.TokenLiteral());
            }
        }

        [Fact]
        public void Identifiers_Are_Parsed_Correctly()
        {
            var input = "foobar;";
            var program = Initialize(input);

            Assert.Equal(1, program.Statements.Count);
            var exp = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var ident = Assert.IsType<Identifier>(exp.Expression);
            Assert.Equal("foobar", ident.Value);
            Assert.Equal("foobar", ident.TokenLiteral());
        }

        [Fact]
        public void IntegerLiterals_Are_Parsed_Correctly()
        {
            var input = "5;";
            var program = Initialize(input);

            Assert.Equal(1, program.Statements.Count);
            var exp = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var ident = Assert.IsType<IntegerLiteral>(exp.Expression);
            Assert.Equal(5, ident.Value);
            Assert.Equal("5", ident.TokenLiteral());
        }

        [Theory]
        [InlineData("!5;", "!", 5L)]
        [InlineData("-15;", "-", 15L)]
        [InlineData("!true;", "!", true)]
        [InlineData("!false;", "!", false)]
        public void PrefixExpressions_Are_Parsed_Correctly(string input, string expectedOp, object expectedValue)
        {
            var program = Initialize(input);

            Assert.Equal(1, program.Statements.Count);
            var exp = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var prefix = Assert.IsType<PrefixExpression>(exp.Expression);
            Assert.Equal(expectedOp, prefix.Operator);
            AssertExpression(expectedValue, prefix.Right);
        }

        [Theory]
        [InlineData("5 + 5", 5L, "+", 5L)]
        [InlineData("5 - 5", 5L, "-", 5L)]
        [InlineData("5 * 5", 5L, "*", 5L)]
        [InlineData("5 / 5", 5L, "/", 5L)]
        [InlineData("5 % 5", 5L, "%", 5L)]
        [InlineData("5 > 5", 5L, ">", 5L)]
        [InlineData("5 < 5", 5L, "<", 5L)]
        [InlineData("5 == 5", 5L, "==", 5L)]
        [InlineData("5 != 5", 5L, "!=", 5L)]
        [InlineData("true == true", true, "==", true)]
        [InlineData("true != false", true, "!=", false)]
        [InlineData("false == false", false, "==", false)]
        public void InfixExpressions_Are_Parsed_Correctly(string input, object expectedLeft, string expectedOp, object expectedRight)
        {
            var program = Initialize(input);

            Assert.Equal(1, program.Statements.Count);
            var exp = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var infix = Assert.IsType<InfixExpression>(exp.Expression);
            AssertInfixExpression(infix, expectedLeft, expectedOp, expectedRight);
        }

        [Theory]
        [InlineData("-a * b", "((-a) * b)")]
        [InlineData("!-a", "(!(-a))")]
        [InlineData("a + b + c", "((a + b) + c)")]
        [InlineData("a + b - c", "((a + b) - c)")]
        [InlineData("a * b * c", "((a * b) * c)")]
        [InlineData("a * b / c", "((a * b) / c)")]
        [InlineData("a + b / c", "(a + (b / c))")]
        [InlineData("a + b * c + d / e - f", "(((a + (b * c)) + (d / e)) - f)")]
        [InlineData("3 + 4; -5 * 5", "(3 + 4)((-5) * 5)")]
        [InlineData("5 > 4 == 3 < 4", "((5 > 4) == (3 < 4))")]
        [InlineData("5 < 4 != 3 > 4", "((5 < 4) != (3 > 4))")]
        [InlineData("3 + 4 * 5 == 3 * 1 + 4 * 5", "((3 + (4 * 5)) == ((3 * 1) + (4 * 5)))")]
        [InlineData("true", "true")]
        [InlineData("false", "false")]
        [InlineData("3 > 5 == false", "((3 > 5) == false)")]
        [InlineData("3 < 5 == true", "((3 < 5) == true)")]
        [InlineData("1 + (2 + 3) + 4", "((1 + (2 + 3)) + 4)")]
        [InlineData("(5 + 5) * 2", "((5 + 5) * 2)")]
        [InlineData("2 / (5 + 5)", "(2 / (5 + 5))")]
        [InlineData("-(5 + 5)", "(-(5 + 5))")]
        [InlineData("!(true == true)", "(!(true == true))")]
        [InlineData("a + add(b * c) + d", "((a + add((b * c))) + d)")]
        [InlineData("add(a, b, 1, 2 * 3, 4 + 5, add(6, 7 * 8))", "add(a, b, 1, (2 * 3), (4 + 5), add(6, (7 * 8)))")]
        [InlineData("add(a + b + c * d / f + g)", "add((((a + b) + ((c * d) / f)) + g))")]
        [InlineData("a * [1, 2, 3, 4][b * c] * d", "((a * ([1, 2, 3, 4][(b * c)])) * d)")]
        [InlineData("add(a * b[2], b[1], 2 * [1, 2][1])", "add((a * (b[2])), (b[1]), (2 * ([1, 2][1])))")]
        public void OperatorPrecedence_Is_Correct(string input, string expected)
        {
            var program = Initialize(input);

            Assert.Equal(expected, program.ToString());
        }

        [Fact]
        public void BooleanExpressions_Are_Parsed_Correctly()
        {
            var input = "true;";
            var program = Initialize(input);

            Assert.Equal(1, program.Statements.Count);
            var exp = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var boolean = Assert.IsType<BooleanLiteral>(exp.Expression);
            Assert.Equal(true, boolean.Value);
            Assert.Equal("true", boolean.TokenLiteral());
        }

        [Fact]
        public void IfExpressions_Are_Parsed_Correctly()
        {
            var input = "if(x < y) { x }";
            var program = Initialize(input);

            Assert.Equal(1, program.Statements.Count);
            var exp = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var ifexp = Assert.IsType<IfExpression>(exp.Expression);
            AssertInfixExpression(ifexp.Condition, "x", "<", "y");
            Assert.Equal(1, ifexp.Consequence.Statements.Count);
            var consequence = Assert.IsType<ExpressionStatement>(ifexp.Consequence.Statements[0]);
            AssertExpression("x", consequence.Expression);
            Assert.Null(ifexp.Alternative);
        }

        [Fact]
        public void IfExpressions_With_Else_Are_Parsed_Correctly()
        {
            var input = "if(x < y) { x } else { y }";
            var program = Initialize(input);

            Assert.Equal(1, program.Statements.Count);
            var exp = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var ifexp = Assert.IsType<IfExpression>(exp.Expression);
            AssertInfixExpression(ifexp.Condition, "x", "<", "y");
            Assert.Equal(1, ifexp.Consequence.Statements.Count);
            var consequence = Assert.IsType<ExpressionStatement>(ifexp.Consequence.Statements[0]);
            AssertExpression("x", consequence.Expression);
            var alternative = Assert.IsType<ExpressionStatement>(ifexp.Alternative.Statements[0]);
            AssertExpression("y", alternative.Expression);
        }

        [Fact]
        public void ForExpressions_Are_Parsed_Correctly()
        {
            var input = "for(item in array) { puts(item) }";
            var program = Initialize(input);

            Assert.Equal(1, program.Statements.Count);
            var exp = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var forExp = Assert.IsType<ForExpression>(exp.Expression); 
            AssertExpression("item", forExp.KeyIdentifier);
            Assert.Null(forExp.ValueIdentifier);
            AssertExpression("array", forExp.Collection);
            Assert.Equal(1, forExp.Body.Statements.Count);
            var body = Assert.IsType<ExpressionStatement>(forExp.Body.Statements[0]);
            var callExp = Assert.IsType<CallExpression>(body.Expression);
            Assert.Equal("puts", callExp.Function.TokenLiteral());
        }

        [Fact]
        public void BreakStatement_Is_Parsed_Correctly()
        {
            var input = "break;";
            var program = Initialize(input);

            Assert.Equal(1, program.Statements.Count);
            var brk = Assert.IsType<BreakStatement>(program.Statements[0]);
            Assert.Equal("break", brk.TokenLiteral());
        }

        [Fact]
        public void SkipStatement_Is_Parsed_Correctly()
        {
            var input = "skip;";
            var program = Initialize(input);

            Assert.Equal(1, program.Statements.Count);
            var brk = Assert.IsType<SkipStatement>(program.Statements[0]);
            Assert.Equal("skip", brk.TokenLiteral());
        }

        [Fact]
        public void ForExpressions_For_Hashes_Are_Parsed_Correctly()
        {
            var input = "for(key, value in hash) { puts(key + \": \" + value) }";
            var program = Initialize(input);

            Assert.Equal(1, program.Statements.Count);
            var exp = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var forExp = Assert.IsType<ForExpression>(exp.Expression);
            AssertExpression("key", forExp.KeyIdentifier);
            AssertExpression("value", forExp.ValueIdentifier);
            AssertExpression("hash", forExp.Collection);
            Assert.Equal(1, forExp.Body.Statements.Count);
            var body = Assert.IsType<ExpressionStatement>(forExp.Body.Statements[0]);
            var callExp = Assert.IsType<CallExpression>(body.Expression);
            Assert.Equal("puts", callExp.Function.TokenLiteral());
        }

        [Fact]
        public void FunctionLiterals_Are_Parsed_Correctly()
        {
            var input = "fn(x, y) { x + y; }";
            var program = Initialize(input);

            Assert.Equal(1, program.Statements.Count);
            var exp = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var func = Assert.IsType<FunctionLiteral>(exp.Expression);
            Assert.Equal(2, func.Parameters.Count);
            AssertExpression("x", func.Parameters[0]);
            AssertExpression("y", func.Parameters[1]);
            Assert.Equal(1, func.Body.Statements.Count);
            var bodyStmt = Assert.IsType<ExpressionStatement>(func.Body.Statements[0]);
            AssertInfixExpression(bodyStmt.Expression, "x", "+", "y");
        }

        [Theory]
        [InlineData("fn() { };", new string[] { })]
        [InlineData("fn(x) { };", new string[] { "x" })]
        [InlineData("fn(x, y, z) { };", new string[] { "x", "y", "z" })]
        public void FunctionParameters_Are_Parsed_Correctly(string input, string[] expectedParams)
        {
            var program = Initialize(input);

            var stmt = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var func = Assert.IsType<FunctionLiteral>(stmt.Expression);

            Assert.Equal(expectedParams.Length, func.Parameters.Count);
            for (var i = 0; i < expectedParams.Length; i++)
            {
                AssertExpression(expectedParams[i], func.Parameters[i]);
            }
        }

        [Fact]
        public void CallExpressions_Are_Parsed_Correctly()
        {
            var input = "add(1, 2 * 3, 4 + 5);";
            var program = Initialize(input);

            Assert.Equal(1, program.Statements.Count);
            var stmt = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var callExp = Assert.IsType<CallExpression>(stmt.Expression);
            AssertExpression("add", callExp.Function);
            Assert.Equal(3, callExp.Arguments.Count);
            AssertExpression(1L, callExp.Arguments[0]);
            AssertInfixExpression(callExp.Arguments[1], 2L, "*", 3L);
            AssertInfixExpression(callExp.Arguments[2], 4L, "+", 5L);
        }

        [Fact]
        public void StringLiterals_Are_Parsed_Correctly()
        {
            var input = "\"hello world!\";";
            var program = Initialize(input);

            Assert.Equal(1, program.Statements.Count);
            var stmt = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var str = Assert.IsType<StringLiteral>(stmt.Expression);
            Assert.Equal("hello world!", str.Value);
        }

        [Fact]
        public void InvalidStringLiterals_Are_Detected()
        {
            var input = "let name = \"Mike";
            var lexer = new Lexer(input);
            var parser = new Parser(lexer);
            var program = parser.ParseProgram();

            Assert.Equal(1, parser.Errors.Count);
            Assert.Equal("Invalid string detected (did you forget an ending \"?)", parser.Errors[0]);
        }

        [Fact]
        public void ArrayLiterals_Are_Parsed_Correctly()
        {
            var input = "[1, 2 * 2, 3 + 3];";
            var program = Initialize(input);

            var stmt = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var array = Assert.IsType<ArrayLiteral>(stmt.Expression);
            Assert.Equal(3, array.Elements.Count);
            AssertExpression(1L, array.Elements[0]);
            AssertInfixExpression(array.Elements[1], 2L, "*", 2L);
            AssertInfixExpression(array.Elements[2], 3L, "+", 3L);
        }

        [Fact]
        public void IndexExpressions_Are_Parsed_Correctly()
        {
            var input = "array[1 + 1];";
            var program = Initialize(input);

            var stmt = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var index = Assert.IsType<IndexExpression>(stmt.Expression);
            AssertExpression("array", index.Left);
            AssertInfixExpression(index.Index, 1L, "+", 1L);
        }

        [Fact]
        public void HashLiterals_With_String_Keys_Are_Parsed_Correctly()
        {
            var input = "{\"one\": 1, \"two\": 2, \"three\": 3};";
            var program = Initialize(input);
            var expected = new Dictionary<string, long>
            {
                {"one", 1L},
                {"two", 2L},
                {"three", 3L}
            };

            var stmt = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var hash = Assert.IsType<HashLiteral>(stmt.Expression);
            Assert.Equal(3, hash.Pairs.Count);

            foreach(var kvp in hash.Pairs)
            {
                var key = Assert.IsType<StringLiteral>(kvp.Key);
                AssertExpression(expected[key.ToString()], kvp.Value);
            }
        }

        [Fact]
        public void HashLiterals_That_Are_Empty_Are_Parsed_Correctly()
        {
            var input = "{}";
            var program = Initialize(input);

            var stmt = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var hash = Assert.IsType<HashLiteral>(stmt.Expression);
            Assert.Equal(0, hash.Pairs.Count);
        }

        [Fact]
        public void HashLiterals_With_Expressions_Are_Parsed_Correctly()
        {
            var input = "{\"one\": 0 + 1, \"two\": 10 - 8, \"three\": 15 / 5};";
            var program = Initialize(input);
            var expected = new Dictionary<string, Action<IExpression>>
            {
                {"one", (exp) => AssertInfixExpression(exp, 0L, "+", 1L)},
                {"two", (exp) => AssertInfixExpression(exp, 10L, "-", 8L)},
                {"three", (exp) => AssertInfixExpression(exp, 15L, "/", 5L)}
            };

            var stmt = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var hash = Assert.IsType<HashLiteral>(stmt.Expression);
            Assert.Equal(3, hash.Pairs.Count);

            foreach (var kvp in hash.Pairs)
            {
                var key = Assert.IsType<StringLiteral>(kvp.Key);
                var assertFunc = expected[key.ToString()];
                assertFunc(kvp.Value);
            }
        }

        [Fact]
        public void Modify_Replaces_Nodes()
        {
            var token1 = new Token(Token.Int, "1");
            var token2 = new Token(Token.Int, "2");
            Func<IExpression> one = () => new IntegerLiteral(null) { Value = 1 };
            Func<IExpression> two = () => new IntegerLiteral(null) { Value = 2 };
            Func<INode, INode> turnOneIntoTwo = (node) =>
            {
                if (node is IntegerLiteral integer)
                {
                    if (integer.Value != 1)
                    {
                        return node;
                    }
                    integer.Value = 2;
                    return integer;
                }

                return node;
            };
            var tests = new Dictionary<INode, INode>
            {
                { one(),
                  two() },

                { new ProgramNode(){Statements = new List<IStatement>{ new ExpressionStatement(null) { Expression = one() } } } ,
                  new ProgramNode(){Statements = new List<IStatement>{ new ExpressionStatement(null) { Expression = two() } } } },

                { new InfixExpression(null){ Left = one(), Operator = "+", Right = two()},
                  new InfixExpression(null){ Left = two(), Operator = "+", Right = two()} },

                { new InfixExpression(null){ Left = two(), Operator = "+", Right = one()},
                  new InfixExpression(null){ Left = two(), Operator = "+", Right = two()} },

                { new PrefixExpression(null){ Operator = "-", Right = one() },
                  new PrefixExpression(null){ Operator = "-", Right = two() } },

                { new IndexExpression(null) { Left = one(), Index = one() },
                  new IndexExpression(null) { Left = two(), Index = two() } },

                { new IfExpression(null) { Condition = one(),
                                           Consequence = new BlockStatement(null) {
                                               Statements = new List<IStatement>() {
                                                   new ExpressionStatement(null) { Expression = one() } } },
                                           Alternative = new BlockStatement(null) {
                                               Statements = new List<IStatement>() {
                                                   new ExpressionStatement(null) { Expression = one() } } } },
                  new IfExpression(null) { Condition = two(),
                                           Consequence = new BlockStatement(null) {
                                               Statements = new List<IStatement>() {
                                                   new ExpressionStatement(null) { Expression = two() } } },
                                           Alternative = new BlockStatement(null) {
                                               Statements = new List<IStatement>() {
                                                   new ExpressionStatement(null) { Expression = two() } } } } },

                { new ForExpression(null) { Body = new BlockStatement(null) {
                                                Statements = new List<IStatement>() {
                                                    new ExpressionStatement(null) {  Expression = one() } } },
                                            Collection = new ArrayLiteral(null) {
                                                Elements = new List<IExpression>() { one() } },
                                            KeyIdentifier = new Identifier(null) { Value = "item" } },
                 new ForExpression(null) { Body = new BlockStatement(null) {
                                               Statements = new List<IStatement>() {
                                                    new ExpressionStatement(null) {  Expression = two() } } },
                                           Collection = new ArrayLiteral(null) {
                                                Elements = new List<IExpression>() { two() } },
                                           KeyIdentifier = new Identifier(null) { Value = "item" } } },

                { new ReturnStatement(null) { ReturnValue = one() },
                  new ReturnStatement(null) { ReturnValue = two() } },

                { new LetStatement(null) { Value = one() },
                  new LetStatement(null) { Value = two() } },

                { new FunctionLiteral(null) { Parameters = new List<Identifier>(),
                                              Body = new BlockStatement(null) {
                                                  Statements = new List<IStatement>() {
                                                      new ExpressionStatement(null) { Expression = one() } } } },
                  new FunctionLiteral(null) { Parameters = new List<Identifier>(),
                                              Body = new BlockStatement(null) {
                                                  Statements = new List<IStatement>() {
                                                      new ExpressionStatement(null) { Expression = two() } } } } },

                { new ArrayLiteral(null) { Elements = new List<IExpression>() { one(), one() } },
                  new ArrayLiteral(null) { Elements = new List<IExpression>() { two(), two() } } },

            };

            foreach (var test in tests)
            {
                var modified = Parser.Modify(test.Key, turnOneIntoTwo);
                var jsonModified = Newtonsoft.Json.JsonConvert.SerializeObject(modified);
                var jsonExpected = Newtonsoft.Json.JsonConvert.SerializeObject(test.Value);

                Assert.Equal(jsonExpected, jsonModified);
            }

            var hashLiteral = new HashLiteral(null)
            {
                Pairs = new Dictionary<IExpression, IExpression>()
                {
                    { one(), one() },
                    { one(), one() }
                }
            };

            Parser.Modify(hashLiteral, turnOneIntoTwo);

            foreach(var kvp in hashLiteral.Pairs)
            {
                var key = Assert.IsType<IntegerLiteral>(kvp.Key);
                var val = Assert.IsType<IntegerLiteral>(kvp.Value);

                Assert.Equal(2, key.Value);
                Assert.Equal(2, val.Value);
            }
        }

        [Fact]
        public void MacroLiterals_Are_Parsed_Correctly()
        {
            var input = "macro(x, y) { x + y; }";
            var program = Initialize(input);

            Assert.Equal(1, program.Statements.Count);
            var stmt = Assert.IsType<ExpressionStatement>(program.Statements[0]);
            var macro = Assert.IsType<MacroLiteral>(stmt.Expression);
            Assert.Equal(2, macro.Parameters.Count);
            AssertExpression("x", macro.Parameters[0]);
            AssertExpression("y", macro.Parameters[1]);
            Assert.Equal(1, macro.Body.Statements.Count);
            var body = Assert.IsType<ExpressionStatement>(macro.Body.Statements[0]);
            AssertInfixExpression(body.Expression, "x", "+", "y");
        }

        private ProgramNode Initialize(string input)
        {
            var lexer = new Lexer(input);
            var parser = new Parser(lexer);
            var program = parser.ParseProgram();

            Assert.Equal(0, parser.Errors.Count);

            return program;
        }

        private void AssertInfixExpression(IExpression exp, object expectedleft, string expectedOp, object expectedRight)
        {
            var infix = Assert.IsType<InfixExpression>(exp);
            AssertExpression(expectedleft, infix.Left);
            Assert.Equal(expectedOp, infix.Operator);
            AssertExpression(expectedRight, infix.Right);
        }

        private void AssertExpression (object expectedValue, IExpression exp)
        {
            switch (exp)
            {
                case IntegerLiteral i:
                    Assert.Equal(expectedValue, i.Value);
                    break;
                case BooleanLiteral b:
                    Assert.Equal(expectedValue, b.Value);
                    break;
                case Identifier ident:
                    Assert.Equal(expectedValue, ident.Value);
                    break;
                default:
                    Assert.True(false, "Unexpected expression!");
                    break;
            }
        }
    }
}