using NUnit.Framework;
using System.Collections.Generic;

namespace ULox.Tests
{
    public class SimpleTestRunnerByteCodeLoxEngineTests
    {
        public class SimpleTestrunnerScanner : Scanner
        {
            protected override (char ch, TokenType token)[] CharToTokenTuple()
            {
                return new[]
                {
                    ('(', TokenType.OPEN_PAREN),
                    (')', TokenType.CLOSE_PAREN),
                    ('{', TokenType.OPEN_BRACE),
                    ('}', TokenType.CLOSE_BRACE),
                    (',', TokenType.COMMA),
                    (';', TokenType.END_STATEMENT),
                };
            }

            protected override (char ch, TokenType tokenFlat, TokenType tokenCompound)[] CharToCompoundTokenTuple()
            {
                return new[]
                {
                    ('+', TokenType.PLUS, TokenType.PLUS_EQUAL),
                    ('-', TokenType.MINUS, TokenType.MINUS_EQUAL),
                    ('*', TokenType.STAR, TokenType.STAR_EQUAL),
                    ('%', TokenType.PERCENT, TokenType.PERCENT_EQUAL),
                    ('!', TokenType.BANG, TokenType.BANG_EQUAL),
                    ('=', TokenType.ASSIGN, TokenType.EQUALITY),
                    ('<', TokenType.LESS, TokenType.LESS_EQUAL),
                    ('>', TokenType.GREATER, TokenType.GREATER_EQUAL),
                };
            }

            protected override (string, TokenType)[] IdentifierTokenTypeTuple()
            {
                return new[]
                {
                    ( "var",    TokenType.VAR),
                    ( "string", TokenType.STRING),
                    ( "int",    TokenType.INT),
                    ( "float",  TokenType.FLOAT),
                    ( "and",    TokenType.AND),
                    ( "or",     TokenType.OR),
                    ( "if",     TokenType.IF),
                    ( "else",   TokenType.ELSE),
                    ( "while",  TokenType.WHILE),
                    ( "for",    TokenType.FOR),
                    ( "loop",   TokenType.LOOP),
                    ( "return", TokenType.RETURN),
                    ( "break",  TokenType.BREAK),
                    ( "continue", TokenType.CONTINUE),
                    ( "true",   TokenType.TRUE),
                    ( "false",  TokenType.FALSE),
                    ( "null",   TokenType.NULL),
                    ( "fun",    TokenType.FUNCTION),

                    ( "test",  TokenType.TEST),
                    ( "testcase",  TokenType.TESTCASE),
                };
            }
        }
        public class SimpleTestrunnerCompiler : Compiler
        {
            protected override void GenerateDeclarationLookup()
            {
                var testcaseCompilette = new TestcaseCompillette();
                var testdec = new TestDeclarationCompilette();
                testcaseCompilette.SetTestDeclarationCompilette(testdec);
                var decl = new List<ICompilette>()
            {
                testdec,
                new CompiletteAction(TokenType.FUNCTION, FunctionDeclaration),
                new CompiletteAction(TokenType.VAR, VarDeclaration),
                testcaseCompilette,
            };

                foreach (var item in decl)
                    declarationCompilettes[item.Match] = item;
            }
        }

        public class SimpleTestrunnerProgram : ProgramBase<SimpleTestrunnerScanner, SimpleTestrunnerCompiler, DisassemblerBase>
        {

        }

        public class SimpleTestrunnerVM : VMBase
        {
            protected override bool DoCustomComparisonOp(OpCode opCode, Value lhs, Value rhs) => false;

            protected override bool DoCustomMathOp(OpCode opCode, Value lhs, Value rhs) => false;

            protected override bool ExtendedCall(Value callee, int argCount) => false;

            protected override bool ExtendedOp(OpCode opCode, Chunk chunk) => false;
        }

        public class SimpleTestrunnerEngine
        {
            public IProgram Program { get; private set; } = new SimpleTestrunnerProgram();
            private readonly VMBase _vm = new SimpleTestrunnerVM();
            public VMBase VM => _vm;

            public string Disassembly => Program.Disassembly;

            public virtual void Run(string testString)
            {
                var chunk = Program.Compile(testString);
                _vm.Interpret(chunk.TopLevelChunk);
            }

            public virtual void Execute(Program program)
            {
                Program = program;
                _vm.Run(Program);
            }

            public virtual void AddLibrary(ILoxByteCodeLibrary lib)
            {
                var toAdd = lib.GetBindings();

                foreach (var item in toAdd)
                {
                    _vm.SetGlobal(item.Key, item.Value);
                }
            }
        }

        public class SimpleTestrunnerTestEngine : SimpleTestrunnerEngine
        {
            private System.Action<string> _logger;

            public SimpleTestrunnerTestEngine(System.Action<string> logger)
            {
                _logger = logger;

                Value Print(VMBase vm, int args)
                {
                    var str = vm.GetArg(1).ToString();
                    _logger(str);
                    AppendResult(str);
                    return Value.Null();
                }

                VM.SetGlobal("print", Value.New(Print));
            }
            protected void AppendResult(string str) => InterpreterResult += str;
            public string InterpreterResult { get; private set; } = string.Empty;

            public override void Run(string testString)
            {
                try
                {
                    base.Run(testString);
                }
                catch (LoxException e)
                {
                    AppendResult(e.Message);
                }
                finally
                {
                    _logger(InterpreterResult);
                    _logger(Disassembly);
                    _logger(VM.GenerateGlobalsDump());
                }
            }
        }


        private SimpleTestrunnerTestEngine engine;

        [SetUp]
        public void Setup()
        {
            engine = new SimpleTestrunnerTestEngine(UnityEngine.Debug.Log);
        }


        [Test]
        public void Engine_Cycle_Global_Var()
        {
            engine.Run(@"var myVar = 10; 
var myNull; 
print (myVar); 
print (myNull);

var myOtherVar = myVar * 2;

print (myOtherVar);");

            Assert.AreEqual("10null20", engine.InterpreterResult);
        }
    }
}