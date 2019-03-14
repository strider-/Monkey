using Monkey.Lexing;
using Monkey.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Monkey.Evaluation
{
    public class Evaluator
    {
        private Dictionary<string, BuiltIn> builtins;

        public Evaluator()
        {
            True = new Boolean(true);
            False = new Boolean(false);
            Null = new Null();

            builtins = new Dictionary<string, BuiltIn>
            {
                {"len", new BuiltIn(Len)},
                {"first", new BuiltIn(First)},
                {"last", new BuiltIn(Last)},
                {"rest", new BuiltIn(Rest)},
                {"push", new BuiltIn(Push)},
                {"puts", new BuiltIn(Puts)},
                {"str", new BuiltIn(Str)},
                {"typeof", new BuiltIn(TypeOf)},
                {"map", new BuiltIn(Map)},
                {"reduce", new BuiltIn(Reduce)}
            };
        }

        public IObject Eval(INode node, Environment env)
        {
            switch (node)
            {
                case ProgramNode prog:
                    return EvalProgram(prog.Statements, env);

                case ExpressionStatement exp:
                    return Eval(exp.Expression, env);

                case IntegerLiteral intLit:
                    return new Integer(intLit.Value);

                case BooleanLiteral boolLit:
                    return NativeBoolToBoolean(boolLit.Value);

                case StringLiteral strLit:
                    return new String(strLit.Value);

                case ArrayLiteral arrLit:
                    var elements = EvalExpressions(arrLit.Elements, env);
                    if (elements.Count() == 1 && IsError(elements.Single()))
                    {
                        return elements.Single();
                    }
                    return new Array(elements);

                case HashLiteral hashLit:
                    return EvalHashLiteral(hashLit, env);

                case IndexExpression idxExp:
                    var idxLeft = Eval(idxExp.Left, env);
                    if (IsError(idxLeft))
                    {
                        return idxLeft;
                    }
                    var index = Eval(idxExp.Index, env);
                    if (IsError(index))
                    {
                        return index;
                    }
                    return EvalIndexExpression(idxLeft, index);

                case PrefixExpression prefix:
                    var pRight = Eval(prefix.Right, env);
                    if (IsError(pRight))
                    {
                        return pRight;
                    }
                    return EvalPrefixExpression(prefix.Operator, pRight, env);

                case InfixExpression infix:
                    var iLeft = Eval(infix.Left, env);
                    if (IsError(iLeft))
                    {
                        return iLeft;
                    }
                    var iRight = Eval(infix.Right, env);
                    if (IsError(iRight))
                    {
                        return iRight;
                    }
                    return EvalInfixExpression(infix.Operator, iLeft, iRight, env);

                case BlockStatement bstmt:
                    return EvalBlockStatement(bstmt, env);

                case IfExpression ifExp:
                    return EvalIfExpression(ifExp, env);

                case ForExpression forExp:
                    ForLoopCounter++;
                    var forResult = EvalForExpression(forExp, env);
                    ForLoopCounter--;
                    return forResult;

                case ReturnStatement rstmt:
                    var rVal = Eval(rstmt.ReturnValue, env);
                    if (IsError(rVal))
                    {
                        return rVal;
                    }
                    return new ReturnValue(rVal);

                case BreakStatement bstmt:
                    return EvalBreakStatement(bstmt);

                case SkipStatement sstmt:
                    return EvalSkipStatement(sstmt);

                case LetStatement lstmt:
                    var lVal = Eval(lstmt.Value, env);
                    if (IsError(lVal))
                    {
                        return lVal;
                    }
                    env.Set(lstmt.Name.Value, lVal);
                    return null;

                case Identifier ident:
                    return EvalIdentifier(ident, env);

                case FunctionLiteral fn:
                    var parms = fn.Parameters;
                    var body = fn.Body;
                    return new Function(parms, body, env);

                case CallExpression ce:
                    if (ce.Function.TokenLiteral() == "quote")
                    {
                        return Quote(ce.Arguments[0], env);
                    }
                    var func = Eval(ce.Function, env);
                    if (IsError(func))
                    {
                        return func;
                    }
                    var args = EvalExpressions(ce.Arguments, env);
                    if (args.Count() == 1 && IsError(args.First()))
                    {
                        return args.First();
                    }
                    return ApplyFunction(func, args);
            }

            return null;
        }

        public void DefineMacros(ProgramNode program, Environment env)
        {
            var definitions = new List<int>();
            for (int i = 0; i < program.Statements.Count; i++)
            {
                var stmt = program.Statements[i];
                if (IsMacroDefinition(stmt))
                {
                    AddMacro(stmt, env);
                    definitions.Add(i);
                }
            }

            foreach (var index in definitions)
            {
                program.Statements.RemoveAt(index);
            }
        }

        public INode ExpandMacros(INode program, Environment env)
        {
            return Parser.Modify(program, (node) =>
            {
                if (!(node is CallExpression ce))
                {
                    return node;
                }

                if (!IsMacroCall(ce, env, out var macro))
                {
                    return node;
                }

                var args = ce.Arguments.Select(arg => new Quote(arg));
                var evalEnv = ExtendMacroEnv(macro, args);
                var evaluated = Eval(macro.Body, evalEnv);

                if (!(evaluated is Quote quote))
                {
                    throw new Exception("only returning quote from macros");
                }

                return quote.Node;
            });
        }

        private IObject Len(params IObject[] args)
        {
            if (args.Length != 1)
            {
                return NewError($"wrong number of arguments, expected 1 got {args.Length}");
            }

            var arg = args.Single();
            switch (arg)
            {
                case String str:
                    return new Integer(str.Value.Length);
                case Array array:
                    return new Integer(array.Elements.Length);
                case Hash hash:
                    return new Integer(hash.Pairs.Count);
                default:
                    return NewError($"argument of type {arg.Type()} not supported by len");
            }
        }

        private IObject First(params IObject[] args)
        {
            if (args.Length != 1)
            {
                return NewError($"wrong number of arguments, expected 1 got {args.Length}");
            }
            if (args[0].Type() != Array.ObjType)
            {
                return NewError($"argument of type {args[0].Type()} not supported by first");
            }
            var arg = args[0] as Array;
            if (arg != null && arg.Elements.Length > 0)
            {
                return arg.Elements[0];
            }

            return Null;
        }

        private IObject Last(params IObject[] args)
        {
            if (args.Length != 1)
            {
                return NewError($"wrong number of arguments, expected 1 got {args.Length}");
            }
            if (args[0].Type() != Array.ObjType)
            {
                return NewError($"argument of type {args[0].Type()} not supported by last");
            }
            var arg = args[0] as Array;
            if (arg != null && arg.Elements.Length > 0)
            {
                return arg.Elements.Last();
            }

            return Null;
        }

        private IObject Rest(params IObject[] args)
        {
            if (args.Length != 1)
            {
                return NewError($"wrong number of arguments, expected 1 got {args.Length}");
            }
            if (args[0].Type() != Array.ObjType)
            {
                return NewError($"argument of type {args[0].Type()} not supported by rest");
            }
            var arg = args[0] as Array;
            if (arg != null && arg.Elements.Length > 0)
            {
                return new Array(arg.Elements.Skip(1));
            }

            return Null;
        }

        private IObject Push(params IObject[] args)
        {
            if (args.Length != 2)
            {
                return NewError($"wrong number of arguments, expected 2 got {args.Length}");
            }
            if (args[0].Type() != Array.ObjType)
            {
                return NewError($"argument of type {args[0].Type()} not supported by push");
            }
            var arg = args[0] as Array;
            return new Array(arg.Elements.Append(args[1]));
        }

        private IObject Puts(params IObject[] args)
        {
            foreach (var arg in args)
            {
                Console.WriteLine(arg.Inspect());
            }

            return Null;
        }

        private IObject Str(params IObject[] args)
        {
            if (args.Length != 1)
            {
                return NewError($"wrong number of arguments, expected 1 got {args.Length}");
            }
            switch (args[0])
            {
                case Integer i:
                    return new String(i.Value.ToString());
                case Boolean b:
                    return new String(b.Value ? bool.TrueString : bool.FalseString);
                case String s:
                    return s;
                default:
                    return NewError($"type {args[0].Type()} cannot be converted to a string");
            }
        }

        private IObject TypeOf(params IObject[] args)
        {
            if (args.Length != 1)
            {
                return NewError($"wrong number of arguments, expected 1 got {args.Length}");
            }

            return new String(args[0].Type());
        }

        private IObject Map(params IObject[] args)
        {
            if (args.Length != 2)
            {
                return NewError($"wrong number of arguments, expected 2 got {args.Length}");
            }
            if (!(args[0] is Array array))
            {
                return NewError($"argument of type {args[0].Type()} not supported by map");
            }
            if (!(args[1] is Function func))
            {
                return NewError($"mapping applier must be a function.");
            }

            var mapped = new List<IObject>();

            for (int i = 0; i < array.Elements.Length; i++)
            {
                var result = ApplyFunction(func, new[] { array.Elements[i] });
                mapped.Add(result);
            }

            return new Array(mapped);
        }

        private IObject Reduce(params IObject[] args)
        {
            if (args.Length != 3)
            {
                return NewError($"wrong number of arguments, expected 3 got {args.Length}");
            }
            if (!(args[0] is Array array))
            {
                return NewError($"argument of type {args[0].Type()} not supported by reduce");
            }
            if (!(args[2] is Function func) || func.Parameters.Count() != 2)
            {
                return NewError($"reducer must be a function that takes 2 parameters.");
            }

            var result = array.Elements.Aggregate(args[1], (a, c) => ApplyFunction(func, new[] { a, c }));

            return result;
        }

        private bool IsMacroCall(CallExpression exp, Environment env, out Macro macro)
        {
            macro = null;
            if(!(exp.Function is Identifier ident))
            {
                return false;
            }

            if(!env.Get(ident.Value, out var obj))
            {
                return false;
            }

            if(!(obj is Macro m))
            {
                return false;
            }

            macro = m;
            return true;
        }

        private Environment ExtendMacroEnv(Macro macro, IEnumerable<Quote> args)
        {
            var extended = new Environment(macro.Environment);
            for (int i = 0; i < macro.Parameters.Count; i++)
            {
                extended.Set(macro.Parameters[i].Value, args.ElementAt(i));
            }
            return extended;
        }

        private void AddMacro(IStatement statement, Environment env)
        {
            var let = statement as LetStatement;
            var literal = let.Value as MacroLiteral;

            var macro = new Macro(literal.Parameters, literal.Body, env);
            env.Set(let.Name.Value, macro);
        }

        private bool IsMacroDefinition(IStatement statement)
        {
            if(statement is LetStatement let)
            {
                if(let.Value is MacroLiteral macro)
                {
                    return true;
                }
            }

            return false;
        }

        private IObject EvalProgram(IEnumerable<IStatement> statements, Environment env)
        {
            IObject result = null;

            foreach (var stmt in statements)
            {
                result = Eval(stmt, env);

                switch (result)
                {
                    case ReturnValue rv:
                        return rv.Value;
                    case Error err:
                        return err;
                }
            }

            return result;
        }

        private IObject EvalHashLiteral(HashLiteral node, Environment env)
        {
            var pairs = new Dictionary<long, KeyValuePair<IObject, IObject>>();

            foreach (var kvp in node.Pairs)
            {
                var key = Eval(kvp.Key, env);
                if (IsError(key))
                {
                    return key;
                }

                if(!(key is IHashable))
                {
                    return NewError($"unusable as hash key: {key.Type()}");
                }

                var hashKey = key.GetHashCode();
                var value = Eval(kvp.Value, env);
                if (IsError(value))
                {
                    return value;
                }

                pairs[hashKey] = new KeyValuePair<IObject, IObject>(key, value);
            }

            return new Hash(pairs);
        }

        private IObject EvalIndexExpression(IObject left, IObject index)
        {
            if (left.Type() == Array.ObjType && index.Type() == Integer.ObjType)
            {
                return EvalArrayIndexExpression(left, index);
            }
            else if (left.Type() == Hash.ObjType)
            {
                return EvalHashIndexExpression(left, index);
            }

            return NewError($"index operator not supported: {left.Type()}");
        }

        private IObject EvalArrayIndexExpression(IObject array, IObject index)
        {
            var arrayObj = array as Array;
            var idx = (index as Integer).Value;
            var max = arrayObj.Elements.Length - 1;

            if (idx < 0 || idx > max)
            {
                return Null;
            }

            return arrayObj.Elements[idx];
        }

        private IObject EvalHashIndexExpression(IObject hash, IObject index)
        {
            var hashObj = hash as Hash;
            if(!(index is IHashable))
            {
                return NewError($"unusable as hash key: {index.Type()}");
            }

            if (!hashObj.Pairs.TryGetValue(index.GetHashCode(), out var kvp))
            {
                return Null;
            }

            return kvp.Value;
        }

        private IEnumerable<IObject> EvalExpressions(IEnumerable<IExpression> exps, Environment env)
        {
            var objs = new List<IObject>();
            foreach (var exp in exps)
            {
                var evaluated = Eval(exp, env);
                if (IsError(evaluated))
                {
                    return new List<IObject> { evaluated }.AsReadOnly();
                }
                objs.Add(evaluated);
            }

            return objs.AsReadOnly();
        }

        private IObject EvalIdentifier(Identifier node, Environment env)
        {
            if (env.Get(node.Value, out var obj))
            {
                return obj;
            }

            if (builtins.TryGetValue(node.Value, out var builtin))
            {
                return builtin;
            }

            return NewError($"identifier not found: {node.Value}");
        }

        private IObject ApplyFunction(IObject fn, IEnumerable<IObject> args)
        {
            switch (fn)
            {
                case Function func:
                    if (func.Parameters.Count() != args.Count())
                    {
                        return NewError($"parameter count mismatch, expected {func.Parameters.Count()}, got {args.Count()}");
                    }

                    var extendedEnv = ExtendFunctionEnv(func, args);
                    var evaluated = Eval(func.Body, extendedEnv);
                    
                    return UnwrappedReturnValue(evaluated);
                case BuiltIn builtin:
                    return builtin.Fn(args.ToArray());
                default:
                    return NewError($"not a function: {fn.Type()}");
            }
        }

        private Environment ExtendFunctionEnv(Function fn, IEnumerable<IObject> args)
        {
            var env = new Environment(fn.Environment);

            for (int i = 0; i < fn.Parameters.Count(); i++)
            {
                env.Set(fn.Parameters.ElementAt(i).Value, args.ElementAt(i));
            }

            return env;
        }

        private IObject UnwrappedReturnValue(IObject obj)
        {
            if (obj is ReturnValue rv)
            {
                return rv.Value;
            }

            return obj;
        }

        private IObject EvalBlockStatement(BlockStatement statement, Environment env)
        {
            IObject result = null;

            foreach (var stmt in statement.Statements)
            {
                result = Eval(stmt, env);

                var shouldHalt = IsError(result) ||
                                 IsReturn(result) ||
                                 IsBreak(result) ||
                                 IsType(result, Skip.ObjType);

                if (result != null && shouldHalt)
                {
                    return result;
                }
            }

            return result;
        }

        private IObject EvalPrefixExpression(string op, IObject right, Environment env)
        {
            switch (op)
            {
                case "!":
                    return EvalBangOperatorExpression(right, env);
                case "-":
                    return EvalMinusPrefixOperatorExpression(right, env);
                default:
                    return NewError($"unknown operator: {op}{right.Type()}");
            }
        }

        private IObject EvalInfixExpression(string op, IObject left, IObject right, Environment env)
        {
            if (left.Type() == Integer.ObjType && right.Type() == Integer.ObjType)
            {
                return EvalIntegerInfixExpression(op, left, right, env);
            }
            else if (left.Type() == String.ObjType && right.Type() == String.ObjType)
            {
                return EvalStringInfixExpression(op, left, right);
            }
            else if (left.Type() != right.Type())
            {
                return NewError($"type mismatch: {left.Type()} {op} {right.Type()}");
            }
            else if (op == "==")
            {
                return NativeBoolToBoolean(left == right);
            }
            else if (op == "!=")
            {
                return NativeBoolToBoolean(left != right);
            }

            return NewError($"unknown operator: {left.Type()} {op} {right.Type()}");
        }

        private IObject EvalIntegerInfixExpression(string op, IObject left, IObject right, Environment env)
        {
            var leftVal = (left as Integer).Value;
            var rightVal = (right as Integer).Value;

            switch (op)
            {
                case "+":
                    return new Integer(leftVal + rightVal);
                case "-":
                    return new Integer(leftVal - rightVal);
                case "*":
                    return new Integer(leftVal * rightVal);
                case "/":
                    return new Integer(leftVal / rightVal);
                case "%":
                    return new Integer(leftVal % rightVal);
                case "<":
                    return NativeBoolToBoolean(leftVal < rightVal);
                case ">":
                    return NativeBoolToBoolean(leftVal > rightVal);
                case "==":
                    return NativeBoolToBoolean(leftVal == rightVal);
                case "!=":
                    return NativeBoolToBoolean(leftVal != rightVal);
                default:
                    return NewError($"unknown operator: {left.Type()} {op} {right.Type()}");
            }
        }

        private IObject EvalStringInfixExpression(string op, IObject left, IObject right)
        {
            var l = (left as String).Value;
            var r = (right as String).Value;

            switch (op)
            {
                case "+":
                    return new String($"{l}{r}");
                case "==":
                    return new Boolean(l == r);
                default:
                    return NewError($"unknown operator: {left.Type()} {op} {right.Type()}");
            }
        }

        private IObject EvalBangOperatorExpression(IObject right, Environment env)
        {
            switch (right)
            {
                case Boolean b:
                    return b.Value ? False : True;
                case Null n:
                    return True;
                default:
                    return False;
            }
        }

        private IObject EvalMinusPrefixOperatorExpression(IObject right, Environment env)
        {
            if (right.Type() != Integer.ObjType)
            {
                return NewError($"unknown operator: -{right.Type()}");
            }

            var value = (right as Integer).Value;
            return new Integer(-value);
        }

        private IObject EvalIfExpression(IfExpression exp, Environment env)
        {
            var condition = Eval(exp.Condition, env);
            if (IsError(condition))
            {
                return condition;
            }

            if (IsTruthy(condition))
            {
                return Eval(exp.Consequence, env);
            }
            else if (exp.Alternative != null)
            {
                return Eval(exp.Alternative, env);
            }
            else
            {
                return Null;
            }
        }

        private IObject EvalForExpression(ForExpression exp, Environment env)
        {
            if (exp.Body.IsEmpty())
            {
                return null;
            }

            var subject = Eval(exp.Collection, env);
            if (IsError(subject))
            {
                return subject;
            }

            if ((subject is Array || subject is String) && exp.ValueIdentifier != null)
            {
                return NewError("value identifier is allowed only when enumerating over hashes");
            }

            var loopEnv = new Environment(env);
            switch (subject)
            {
                case Array array:
                    return EvalArrayForExpression(exp, array, loopEnv);
                case String str:
                    return EvalStringForExpression(exp, str, loopEnv);
                case Hash hash:
                    return EvalHashForExpression(exp, hash, loopEnv);
                default:
                    return NewError($"cannot enumerate over {subject.Type()}");
            }
        }

        private IObject EvalArrayForExpression(ForExpression exp, Array array, Environment env)
        {
            for (int i = 0; i < array.Elements.Length; i++)
            {
                env.Set(exp.KeyIdentifier.Value, array.Elements[i]);
                var result = Eval(exp.Body, env);
                if (result != null)
                {
                    if (IsError(result) || IsReturn(result))
                    {
                        return result;
                    }
                    else if (IsBreak(result))
                    {
                        break;
                    }
                }
            }
            return Null;
        }

        private IObject EvalStringForExpression(ForExpression exp, String str, Environment env)
        {
            for (int i = 0; i < str.Value.Length; i++)
            {
                env.Set(exp.KeyIdentifier.Value, new String(str.Value[i]));
                var result = Eval(exp.Body, env);
                if (result != null)
                {
                    if (IsError(result) || IsReturn(result))
                    {
                        return result;
                    }
                    else if (IsBreak(result))
                    {
                        break;
                    }
                }
            }
            return Null;
        }

        private IObject EvalHashForExpression(ForExpression exp, Hash hash, Environment env)
        {
            foreach (var kvp in hash.Pairs)
            {
                env.Set(exp.KeyIdentifier.Value, kvp.Value.Key);
                if (exp.ValueIdentifier != null)
                {
                    env.Set(exp.ValueIdentifier.Value, kvp.Value.Value);
                }
                var result = Eval(exp.Body, env);
                if (result != null)
                {
                    if (IsError(result) || IsReturn(result))
                    {
                        return result;
                    }
                    else if (IsBreak(result))
                    {
                        break;
                    }
                }
            }
            return Null;
        }

        private IObject EvalBreakStatement(BreakStatement statement)
        {
            if (!InLoopContext)
            {
                return NewError("break statement invalid outside of a for loop");
            }

            return new Break();
        }

        private IObject EvalSkipStatement(SkipStatement statement)
        {
            if (!InLoopContext)
            {
                return NewError("skip statement invalid outside of a for loop");
            }

            return new Skip();
        }

        private IObject Quote(INode node, Environment env)
        {
            node = EvalUnquoteCalls(node, env);
            return new Quote(node);
        }

        private INode EvalUnquoteCalls(INode quoted, Environment env)
        {
            return Parser.Modify(quoted, (node) => {
                if (!IsUnquoteCall(node))
                {
                    return node;
                }

                if(node is CallExpression call)
                {
                    if(call.Arguments.Count != 1)
                    {
                        return node;
                    }

                    var unquoted = Eval(call.Arguments[0], env);
                    return ConvertObjectToNode(unquoted);
                }

                return node;
            });
        }

        private bool IsUnquoteCall(INode node)
        {
            if(node is CallExpression ce)
            {
                return ce.Function.TokenLiteral() == "unquote";
            }

            return false;
        }

        public INode ConvertObjectToNode(IObject obj)
        {
            Token token;
            switch (obj)
            {
                case Integer i:
                    token = new Token(Token.Int, i.Value.ToString());
                    return new IntegerLiteral(token) { Value = i.Value };
                case Boolean b:       
                    if (b.Value)
                    {
                        token = new Token(Token.True, "true");
                    }
                    else
                    {
                        token = new Token(Token.False, "false");
                    }
                    return new BooleanLiteral(token) { Value = b.Value };
                case Quote q:
                    return q.Node;
                default:
                    return null;
            }
        }

        private bool IsTruthy(IObject obj)
        {
            switch (obj)
            {
                case Null n:
                    return false;
                case Boolean b:
                    return b.Value;
                case Integer i:
                    return i.Value != 0;
                default:
                    return true;
            }
        }

        private bool IsType(IObject obj, string type) => obj != null && obj.Type() == type;

        private bool IsError(IObject obj) => IsType(obj, Error.ObjType);

        private bool IsReturn(IObject obj) => IsType(obj, ReturnValue.ObjType);

        private bool IsBreak(IObject obj) => IsType(obj, Break.ObjType);

        private Boolean NativeBoolToBoolean(bool b) => b ? True : False;

        private Error NewError(string msg) => new Error(msg);

        private Boolean True { get; }

        private Boolean False { get; }

        private Null Null { get; }

        private int ForLoopCounter { get; set; } = 0;

        private bool InLoopContext => ForLoopCounter > 0;
    }
}
