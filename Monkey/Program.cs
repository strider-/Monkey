using Monkey.Evaluation;
using Monkey.Lexing;
using Monkey.Parsing;
using System.IO;
using System.Linq;

namespace Monkey
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Any())
            {
                var input = File.ReadAllText(args.First());
                var lexer = new Lexer(input);
                var parser = new Parser(lexer);
                var program = parser.ParseProgram();
                var evaluator = new Evaluator();
                var env = new Environment();

                evaluator.Eval(program, env);
            }
            else
            {
                new Repl().Loop();
            }
        }
    }
}