using Monkey.Evaluation;
using Monkey.Lexing;
using Monkey.Parsing;
using System;
using System.Linq;
using System.Text;

namespace Monkey
{
    public class Repl
    {
        const string SinglePrompt = ">> ";
        const string MultiPrompt = "-> ";
        const string MultiToggle = ".";


        private void ShowHelp()
        {
            Console.WriteLine("Welcome to the Monkey programming language!");
            Console.WriteLine("Type in commands, or type \"quit\" (without quotes) to exit.");
            Console.WriteLine($"Enter {MultiToggle} to turn multi-line mode on.  Enter {MultiToggle} again to turn it off and evaluate.");
        }


        public void Loop()
        {
            ShowHelp();
            var env = new Evaluation.Environment();
            var macroEnv = new Evaluation.Environment();

            while (true)
            {
                Console.Write(SinglePrompt);

                var input = Console.ReadLine();
                if (input.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
                {
                    break;
                }
                else if (input.StartsWith("run", StringComparison.CurrentCultureIgnoreCase))
                {
                    input = LoadFile(input);
                }
                else if (input == MultiToggle)
                {
                    input = GetMultiLineInput();
                }

                var lexer = new Lexer(input);
                var parser = new Parser(lexer);
                var prog = parser.ParseProgram();

                if (parser.Errors.Any())
                {
                    foreach (var err in parser.Errors)
                    {
                        Console.WriteLine($"\t{err}");
                    }
                    continue;
                }

                var evaluator = new Evaluator();
                evaluator.DefineMacros(prog, macroEnv);
                var expanded = evaluator.ExpandMacros(prog, macroEnv);
                var obj = evaluator.Eval(expanded, env);

                if (obj != null)
                {
                    Console.WriteLine(obj.Inspect());
                }
            }
        }

        private string GetMultiLineInput()
        {
            string input;
            var multiline = new StringBuilder();

            do
            {
                Console.Write(MultiPrompt);
                input = Console.ReadLine();
                if (input != MultiToggle)
                {
                    multiline.AppendLine(input);
                }
            } while (input != MultiToggle);

            return multiline.ToString();
        }

        private string LoadFile(string input)
        {
            var split = input.Split(new char[] { ' ' }, 2);
            var filePath = split.Length == 2 ? split[1] : null;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                Console.WriteLine("Missing filename");
                return string.Empty;
            }

            try
            {
                return System.IO.File.ReadAllText(filePath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return string.Empty;
        }
    }
}
