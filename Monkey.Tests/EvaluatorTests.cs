using Monkey.Evaluation;
using Monkey.Lexing;
using Monkey.Parsing;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Monkey.Tests
{
    [Trait("Evaluation", "")]
    public class EvaluatorTests
    {
        [Theory]
        [InlineData("5", 5)]
        [InlineData("10", 10)]
        [InlineData("-5", -5)]
        [InlineData("-10", -10)]
        [InlineData("5 + 5 + 5 + 5 - 10", 10)]
        [InlineData("2 * 2 * 2 * 2 * 2", 32)]
        [InlineData("-50 + 100 + -50", 0)]
        [InlineData("5 * 2 + 10", 20)]
        [InlineData("5 + 2 * 10", 25)]
        [InlineData("20 + 2 * -10", 0)]
        [InlineData("50 / 2 * 2 + 10", 60)]
        [InlineData("2 * (5 + 10)", 30)]
        [InlineData("3 * 3 * 3 + 10", 37)]
        [InlineData("3 * (3 * 3) + 10", 37)]
        [InlineData("(5 + 10 * 2 + 15 / 3) * 2 + -10", 50)]
        public void Integers_Are_Evaluated_Correctly(string input, long expected)
        {
            var eval = TestEval(input);
            AssertInteger(expected, eval);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("1 < 2", true)]
        [InlineData("1 > 2", false)]
        [InlineData("1 < 1", false)]
        [InlineData("1 > 1", false)]
        [InlineData("1 == 1", true)]
        [InlineData("1 != 1", false)]
        [InlineData("1 == 2", false)]
        [InlineData("1 != 2", true)]
        [InlineData("true == true", true)]
        [InlineData("false == false", true)]
        [InlineData("true == false", false)]
        [InlineData("true != false", true)]
        [InlineData("false != true", true)]
        [InlineData("(1 < 2) == true", true)]
        [InlineData("(1 < 2) == false", false)]
        [InlineData("(1 > 2) == true", false)]
        [InlineData("(1 > 2) == false", true)]
        public void Booleans_Are_Evaluated_Correctly(string input, bool expected)
        {
            var eval = TestEval(input);
            AssertBoolean(expected, eval);
        }

        [Theory]
        [InlineData("!true", false)]
        [InlineData("!false", true)]
        [InlineData("!5", false)]
        [InlineData("!!true", true)]
        [InlineData("!!false", false)]
        [InlineData("!!5", true)]
        public void BangOperator_Is_Evaluated_Correctly(string input, bool expected)
        {
            var eval = TestEval(input);
            AssertBoolean(expected, eval);
        }

        [Theory]
        [InlineData("if (true) { 10 }", 10L)]
        [InlineData("if (false) { 10 }", null)]
        [InlineData("if (1) { 10 }", 10L)]
        [InlineData("if (1 < 2) { 10 }", 10L)]
        [InlineData("if (1 > 2) { 10 }", null)]
        [InlineData("if (1 > 2) { 10 } else { 20 }", 20L)]
        [InlineData("if (1 < 2) { 10 } else { 20 }", 10L)]
        public void IfExpressions_Are_Evaluated_Correctly(string input, object expected)
        {
            var eval = TestEval(input);
            switch (expected)
            {
                case long l:
                    AssertInteger(l, eval);
                    break;
                case null:
                    Assert.IsType<Null>(eval);
                    break;
                default:
                    Assert.True(false, "unknown expected value");
                    break;
            }
        }

        [Theory]
        [InlineData("return 10;", 10)]
        [InlineData("return 10; 9;", 10)]
        [InlineData("return 2 * 5; 9;", 10)]
        [InlineData("9; return 2 * 5; 9", 10)]
        [InlineData("if (10 > 1) { if (10 > 1) { return 10; } return 1; }", 10)]
        public void ReturnStatements_Are_Evaluated_Correctly(string input, long expected)
        {
            var eval = TestEval(input);
            AssertInteger(expected, eval);
        }

        [Theory]
        [InlineData("5 + true;", "type mismatch: INTEGER + BOOLEAN")]
        [InlineData("5 + true; 5;", "type mismatch: INTEGER + BOOLEAN")]
        [InlineData("-true;", "unknown operator: -BOOLEAN")]
        [InlineData("true + false;", "unknown operator: BOOLEAN + BOOLEAN")]
        [InlineData("5; true + false; 5;", "unknown operator: BOOLEAN + BOOLEAN")]
        [InlineData("if (10 > 1) { true + false; }", "unknown operator: BOOLEAN + BOOLEAN")]
        [InlineData("if (10 > 1) { if (10 > 1) { return true + false; } return 1; }", "unknown operator: BOOLEAN + BOOLEAN")]
        [InlineData("foobar", "identifier not found: foobar")]
        [InlineData("\"Hello\" - \"World\"", "unknown operator: STRING - STRING")]
        [InlineData("{\"name\": \"Monkey\"}[fn(x) { x }];", "unusable as hash key: FUNCTION")]
        public void ErrorHandling_Generates_Expected_Messages(string input, string expectedError)
        {
            var eval = TestEval(input);
            var err = Assert.IsType<Error>(eval);

            Assert.Equal(expectedError, err.Message);
        }

        [Theory]
        [InlineData("let a = 5; a;", 5)]
        [InlineData("let a = 5 * 5; a;", 25)]
        [InlineData("let a = 5; let b = a; b;", 5)]
        [InlineData("let a = 5; let b = a; let c = a + b + 5; c;", 15)]
        public void LetStatements_Are_Evaluated_Correctly(string input, long expected)
        {
            var eval = TestEval(input);
            AssertInteger(expected, eval);
        }

        [Fact]
        public void Functions_Are_Evaluated_Correctly()
        {
            var input = "fn(x) { x + 2; };";
            var eval = TestEval(input);

            var func = Assert.IsType<Function>(eval);
            Assert.Equal(1, func.Parameters.Count());
            Assert.Equal("x", func.Parameters.Single().ToString());
            Assert.Equal("(x + 2)", func.Body.ToString());
        }

        [Theory]
        [InlineData("let identity = fn(x) { x; }; identity(5);", 5)]
        [InlineData("let identity = fn(x) { return x; }; identity(5);", 5)]
        [InlineData("let double = fn(x) { x * 2; }; double(5);", 10)]
        [InlineData("let add = fn(x, y) { x + y; }; add(5, 5);", 10)]
        [InlineData("let add = fn(x, y) { x + y; }; add(5 + 5, add(5, 5));", 20)]
        [InlineData("fn(x) { x; }(5)", 5)]
        public void Function_Application_Is_Evaluated_Correctly(string input, long expected)
        {
            var eval = TestEval(input);
            AssertInteger(expected, eval);
        }

        [Fact]
        public void Closures_Work_As_Expected()
        {
            var input = @"let newAdder = fn(x) {
                            fn(y) { x + y };
                          };

                          let addTwo = newAdder(2);
                          addTwo(7);";
            var eval = TestEval(input);

            AssertInteger(9L, eval);
        }

        [Fact]
        public void StringLiterals_Are_Evaluated_Correctly()
        {
            var input = "\"Hello World!\"";
            var eval = TestEval(input);

            var str = Assert.IsType<String>(eval);
            Assert.Equal("Hello World!", str.Value);
        }

        [Fact]
        public void String_Concatenation_Works_As_Expected()
        {
            var input = "\"Hello\" + \" \" + \"World!\"";
            var eval = TestEval(input);

            var str = Assert.IsType<String>(eval);
            Assert.Equal("Hello World!", str.Value);
        }

        [Theory]
        [InlineData("\"Test\" == \"Test\"", true)]
        [InlineData("\"Test\" == \"test\"", false)]
        [InlineData("str(3) == \"3\"", true)]
        [InlineData("\"Mike\" == \"ekiM\"", false)]
        public void String_Equality_Works_As_Expected(string input, bool expected)
        {
            var eval = TestEval(input);

            AssertBoolean(expected, eval);
        }

        [Theory]
        [InlineData("len(\"\")", 0L)]
        [InlineData("len(\"four\")", 4L)]
        [InlineData("len(\"hello world\")", 11L)]
        [InlineData("len(1)", "argument of type INTEGER not supported by len")]
        [InlineData("len(\"one\", \"two\")", "wrong number of arguments, expected 1 got 2")]
        [InlineData("len([])", 0L)]
        [InlineData("len([1, 2, 3])", 3L)]
        [InlineData("len([0])", 1L)]
        public void BuiltInFunction_Len_Works_As_Expected(string input, object expected)
        {
            var eval = TestEval(input);

            AssertBuiltInFunctionResult(expected, eval);
        }

        [Theory]
        [InlineData("first([1, 2, 3])", 1L)]
        [InlineData("first([3, 2, 1])", 3L)]
        [InlineData("first([0])", 0L)]
        [InlineData("first([])", null)]
        [InlineData("first([1, 2, 3], [])", "wrong number of arguments, expected 1 got 2")]
        [InlineData("first(1)", "argument of type INTEGER not supported by first")]
        public void BuiltInFunction_First_Works_As_Expected(string input, object expected)
        {
            var eval = TestEval(input);

            AssertBuiltInFunctionResult(expected, eval);
        }

        [Theory]
        [InlineData("last([1, 2, 3])", 3L)]
        [InlineData("last([3, 2, 1])", 1L)]
        [InlineData("last([0])", 0L)]
        [InlineData("last([])", null)]
        [InlineData("last([1, 2, 3], [])", "wrong number of arguments, expected 1 got 2")]
        [InlineData("last(\"foo\")", "argument of type STRING not supported by last")]
        public void BuiltInFunction_Last_Works_As_Expected(string input, object expected)
        {
            var eval = TestEval(input);

            AssertBuiltInFunctionResult(expected, eval);
        }

        [Theory]
        [InlineData("rest([1, 2, 3])[0]", 2L)]
        [InlineData("rest([1, 2, 3])[1]", 3L)]
        [InlineData("rest([1, 2, 3])[2]", null)]
        [InlineData("rest([3, 2, 1])[0]", 2L)]
        [InlineData("rest([3, 2, 1])[1]", 1L)]
        [InlineData("rest([3, 2, 1])[2]", null)]
        [InlineData("rest([0])[0]", null)]
        [InlineData("rest([])", null)]
        [InlineData("rest([1, 2, 3], [])", "wrong number of arguments, expected 1 got 2")]
        [InlineData("rest(\"foo\")", "argument of type STRING not supported by rest")]
        public void BuiltInFunction_Rest_Works_As_Expected(string input, object expected)
        {
            var eval = TestEval(input);

            AssertBuiltInFunctionResult(expected, eval);
        }

        [Theory]
        [InlineData("let arr = push([1, 2, 3], 1); arr[3];", 1L)]
        [InlineData("let arr = push([3, 2, 1], 2); arr[3];", 2L)]
        [InlineData("let arr = push([0], 1); arr[1];", 1L)]
        [InlineData("push([], 1)[0];", 1L)]
        [InlineData("push([1, 2, 3])", "wrong number of arguments, expected 2 got 1")]
        [InlineData("push(\"foo\", 1)", "argument of type STRING not supported by push")]
        public void BuiltInFunction_Push_Works_As_Expected(string input, object expected)
        {
            var eval = TestEval(input);

            AssertBuiltInFunctionResult(expected, eval);
        }

        [Theory]
        [InlineData("str(3)", "3")]
        [InlineData("str(54 * 93)", "5022")]
        [InlineData("str(true)", "True")]
        [InlineData("str(34, 2)", "wrong number of arguments, expected 1 got 2")]
        [InlineData("str(\"Hi\")", "Hi")]
        [InlineData("str(!!!true)", "False")]
        [InlineData("let age = 37; \"I am \" + str(age) + \" years old.\"", "I am 37 years old.")]
        [InlineData("str([1,2,3])", "type ARRAY cannot be converted to a string")]
        public void BuiltInFunction_Str_Works_As_Expected(string input, string expected)
        {
            var eval = TestEval(input);

            switch (eval)
            {
                case String str:
                    Assert.Equal(expected, str.Value);
                    break;
                case Error err:
                    Assert.Equal(expected, err.Message);
                    break;
                default:
                    Assert.True(false, "unknown expected value");
                    break;
            }
        }

        [Theory]
        [InlineData("typeof(3 + 7)", "INTEGER")]
        [InlineData("typeof(\"Hi!\")", "STRING")]
        [InlineData("typeof([1,2,3])", "ARRAY")]
        [InlineData("let hash = {}; typeof(hash)", "HASH")]
        [InlineData("typeof(fn(x) { x + x; })", "FUNCTION")]
        [InlineData("typeof(90)", "INTEGER")]
        [InlineData("typeof(puts)", "BUILTIN")]
        [InlineData("typeof(3, 4)", "wrong number of arguments, expected 1 got 2")]
        public void BuiltInFunction_TypeOf_Works_As_Expected(string input, string expected)
        {
            var eval = TestEval(input);

            switch (eval)
            {
                case String str:
                    Assert.Equal(expected, str.Value);
                    break;
                case Error err:
                    Assert.Equal(expected, err.Message);
                    break;
                default:
                    Assert.True(false, "unknown expected value");
                    break;
            }
        }

        [Theory]
        [InlineData("map([1,2,3,4], fn(x) { x * 2; })", new int[] { 2, 4, 6, 8 })]
        [InlineData("map([2,4,6,8], fn(x) { x + x; })", new int[] { 4, 8, 12, 16 })]
        [InlineData("map([10,20,30,40], fn(x) { x / 5; })", new int[] { 2, 4, 6, 8 })]
        public void BuiltInFunction_Map_Works_As_Expected(string input, int[] expected)
        {
            var eval = TestEval(input);

            var array = Assert.IsType<Array>(eval);
            Assert.Equal(expected.Length, array.Elements.Length);
            for (int i = 0; i < array.Elements.Length; i++)
            {
                var integer = Assert.IsType<Integer>(array.Elements[i]);
                Assert.Equal(expected[i], integer.Value);
            }
        }

        [Fact]
        public void ArrayLiterals_Are_Evaluated_Correctly()
        {
            var input = "[1, 2 * 2, 3 + 3]";
            var eval = TestEval(input);

            var array = Assert.IsType<Array>(eval);
            Assert.Equal(3, array.Elements.Length);

            AssertInteger(1L, array.Elements[0]);
            AssertInteger(4L, array.Elements[1]);
            AssertInteger(6L, array.Elements[2]);
        }

        [Theory]
        [InlineData("[1, 2, 3][0]", 1L)]
        [InlineData("[1, 2, 3][1]", 2L)]
        [InlineData("[1, 2, 3][2]", 3L)]
        [InlineData("let i = 0; [1][i];", 1L)]
        [InlineData("[1, 2, 3][1 + 1];", 3L)]
        [InlineData("let myArray = [1, 2, 3]; myArray[2];", 3L)]
        [InlineData("let myArray = [1, 2, 3]; myArray[0] + myArray[1] + myArray[2];", 6L)]
        [InlineData("let myArray = [1, 2, 3]; let i = myArray[0]; myArray[i];", 2L)]
        [InlineData("[1, 2, 3][3]", null)]
        [InlineData("[1, 2, 3][-1]", null)]
        public void ArrayIndexExpressions_Are_Evaluated_Correctly(string input, object expected)
        {
            var eval = TestEval(input);

            switch (expected)
            {
                case long l:
                    AssertInteger(l, eval);
                    break;
                case null:
                    Assert.IsType<Null>(eval);
                    break;
                default:
                    Assert.True(false, "unknown expected value");
                    break;
            }
        }

        [Fact]
        public void HashLiterals_Are_Evaluated_Correctly()
        {
            var input = @"let two = ""two"";
                          {
                            ""one"": 10 - 9,
                            two: 1 + 1,
                            ""thr"" + ""ee"": 6 / 2,
                            4: 4,
                            true: 5,
                            false: 6
                          }";
            var expected = new Dictionary<long, long>
            {
                {new String("one").GetHashCode(), 1},
                {new String("two").GetHashCode(), 2},
                {new String("three").GetHashCode(), 3},
                {new Integer(4).GetHashCode(), 4},
                {new Boolean(true).GetHashCode(), 5},
                {new Boolean(false).GetHashCode(), 6}
            };

            var eval = TestEval(input);

            var hash = Assert.IsType<Hash>(eval);
            Assert.Equal(expected.Count, hash.Pairs.Count);
            foreach (var kvp in expected)
            {
                var pair = hash.Pairs[kvp.Key];
                AssertInteger(kvp.Value, pair.Value);
            }
        }

        [Theory]
        [InlineData("{\"foo\": 5}[\"foo\"]", 5L)]
        [InlineData("{\"foo\": 5}[\"bar\"]", null)]
        [InlineData("let key = \"foo\"; {\"foo\": 5}[key]", 5L)]
        [InlineData("{}[\"foo\"]", null)]
        [InlineData("{5: 5}[5]", 5L)]
        [InlineData("{true: 5}[true]", 5L)]
        [InlineData("{false: 5}[false]", 5L)]
        public void HashIndexExpressions_Are_Evaluated_Correctly(string input, object expected)
        {
            var eval = TestEval(input);
            switch (expected)
            {
                case long l:
                    AssertInteger(l, eval);
                    break;
                case null:
                    Assert.IsType<Null>(eval);
                    break;
                default:
                    Assert.True(false, "unknown expected value");
                    break;
            }
        }

        [Fact]
        public void BreakStatement_Is_Invalid_Outside_A_For_Loop()
        {
            var input = "if(true) { break; }";
            var eval = TestEval(input);
            var expectedMsg = "break statement invalid outside of a for loop";

            var err = Assert.IsType<Error>(eval);
            Assert.Equal(expectedMsg, err.Message);
        }

        [Fact]
        public void BreakStatement_Terminates_A_For_Loop()
        {
            var input = "for(i in [2, 1, 4, 3, 6, 5]) { if(i % 2 == 1) { return false; } if(i % 2 == 0) { break; } };";
            var eval = TestEval(input);

            Assert.IsType<Null>(eval);
        }

        [Fact]
        public void SkipStatement_Is_Invalid_Outside_A_For_Loop()
        {
            var input = "if(true) { skip; }";
            var eval = TestEval(input);
            var expectedMsg = "skip statement invalid outside of a for loop";

            var err = Assert.IsType<Error>(eval);
            Assert.Equal(expectedMsg, err.Message);
        }

        [Fact]
        public void SkipStatement_Forces_Next_Loop_Iteration()
        {
            var input = "for(i in [1, 2, 3]) { if(i != 3) { skip; } return true; };";
            var eval = TestEval(input);

            var boolean = Assert.IsType<Boolean>(eval);
            Assert.True(boolean.Value);
        }

        [Theory]
        [InlineData("quote(5)", "5")]
        [InlineData("quote(5 + 8)", "(5 + 8)")]
        [InlineData("quote(foobar)", "foobar")]
        [InlineData("quote(foobar + barfoo)", "(foobar + barfoo)")]
        [InlineData("let foobar = 8; quote(foobar)", "foobar")]
        public void Quote_Stops_Evaluation(string input, string expected)
        {
            var eval = TestEval(input);
            var quote = Assert.IsType<Quote>(eval);
            Assert.Equal(expected, quote.Node.ToString());
        }

        [Theory]
        [InlineData("quote(unquote(4))", "4")]
        [InlineData("quote(unquote(4 + 4))", "8")]
        [InlineData("quote(8 + unquote(4 + 4))", "(8 + 8)")]
        [InlineData("quote(unquote(4 + 4) + 8)", "(8 + 8)")]
        [InlineData("let foobar = 8; quote(unquote(foobar))", "8")]
        [InlineData("quote(unquote(true))", "true")]
        [InlineData("quote(unquote(true == false))", "false")]
        [InlineData("quote(unquote(quote(4 + 4)))", "(4 + 4)")]
        [InlineData("let qix = quote(4 + 4); quote(unquote(4 + 4) + unquote(qix))", "(8 + (4 + 4))")]
        public void Unquote_Evaluates_An_Expression(string input, string expected)
        {
            var eval = TestEval(input);
            var quote = Assert.IsType<Quote>(eval);
            Assert.Equal(expected, quote.Node.ToString());
        }

        [Fact]
        public void DefineMacros_Works_As_Expected()
        {
            var input = @"let number = 1;
                          let function = fn(x, y) { x + y };
                          let mymacro = macro(x, y) { x + y};";
            var env = new Environment();
            var evaluator = new Evaluator();
            var program = ParseProgram(input);

            evaluator.DefineMacros(program, env);

            Assert.Equal(2, program.Statements.Count);
            Assert.False(env.Get("number", out _));
            Assert.False(env.Get("function", out _));
            Assert.True(env.Get("mymacro", out var obj));
            var macro = Assert.IsType<Macro>(obj);
            Assert.Equal(2, macro.Parameters.Count);
            Assert.Equal("x", macro.Parameters[0].ToString());
            Assert.Equal("y", macro.Parameters[1].ToString());
            Assert.Equal("(x + y)", macro.Body.ToString());
        }

        [Theory]
        [InlineData("let ix = macro() { quote(1 + 2); }; ix();", "(1 + 2)")]
        [InlineData("let reverse = macro(a, b) { quote(unquote(b) - unquote(a)); }; reverse(2 + 2, 10 - 5);", "(10 - 5) - (2 + 2)")]
        [InlineData(@"let unless = macro(condition, consequence, alternative) {
                quote(if (!(unquote(condition))) {
                    unquote(consequence);
                } else {
                    unquote(alternative);
                });
            }; unless(10 > 5, puts(""not greater""), puts(""greater""));", "if (!(10 > 5)) { puts(\"not greater\") } else { puts(\"greater\") }")]
        public void ExpandMacros_Works_As_Expected(string input, string expectedStr)
        {
            var expected = ParseProgram(expectedStr);
            var program = ParseProgram(input);
            var env = new Environment();
            var evaluator = new Evaluator();

            evaluator.DefineMacros(program, env);
            var expanded = evaluator.ExpandMacros(program, env);

            Assert.Equal(expected.ToString(), expanded.ToString());
        }

        private ProgramNode ParseProgram(string input)
        {
            var lexer = new Lexer(input);
            var parser = new Parser(lexer);
            return parser.ParseProgram();
        }

        private IObject TestEval(string input)
        {
            var program = ParseProgram(input);
            var env = new Environment();

            return new Evaluator().Eval(program, env);
        }

        private void AssertBuiltInFunctionResult(object expected, IObject obj)
        {
            switch (expected)
            {
                case long l:
                    AssertInteger(l, obj);
                    break;
                case string str:
                    var err = Assert.IsType<Error>(obj);
                    Assert.Equal(str, err.Message);
                    break;
                case null:
                    Assert.IsType<Null>(obj);
                    break;
                default:
                    Assert.True(false, "unknown expected value");
                    break;
            }
        }

        private void AssertInteger(long expected, IObject obj)
        {
            var integer = Assert.IsType<Integer>(obj);
            Assert.Equal(expected, integer.Value);
        }

        private void AssertBoolean(bool expected, IObject obj)
        {
            var boolean = Assert.IsType<Boolean>(obj);
            Assert.Equal(expected, boolean.Value);
        }
    }
}