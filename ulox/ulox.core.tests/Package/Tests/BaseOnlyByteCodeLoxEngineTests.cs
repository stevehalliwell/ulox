using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace ULox.Tests
{
    public class BaseOnlyByteCodeLoxEngineTests
    {
        public class SimpleEngine
        {
            public IProgram Program { get; private set; } = new Program();
            private readonly Vm _vm = new Vm();
            public Vm VM => _vm;

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
        }

        public class SimpleTestEngine : SimpleEngine
        {
            private System.Action<string> _logger;

            public SimpleTestEngine(System.Action<string> logger)
            {
                _logger = logger;

                VM.SetGlobal(new HashedString("print"), Value.New(Print));
            }

            private NativeCallResult Print(Vm vm, int argc)
            {
                var str = vm.GetArg(1).ToString();
                _logger(str);
                AppendResult(str);
                return NativeCallResult.SuccessfulExpression;
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


        private SimpleTestEngine engine;

        [SetUp]
        public void Setup()
        {
            engine = new SimpleTestEngine(Console.WriteLine);
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


        [Test]
        public void Engine_Var_Assign()
        {
            engine.Run(@"
var myVar; 
myVar = 1;");

            Assert.AreEqual("", engine.InterpreterResult);
        }


        [Test]
        public void Engine_Var_String_Assign()
        {
            engine.Run(@"var myVar = ""test"";");

            Assert.AreEqual("", engine.InterpreterResult);
        }


        [Test]
        public void Engine_FNAME_Usage()
        {
            engine.Run(@"
fun Foo()
{
    print(fname);
}

Foo();");

            Assert.AreEqual("Foo", engine.InterpreterResult);
        }
    }
}
