﻿using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ULox.Tests
{
    public class SimpleTestRunnerByteCodeLoxEngineTests
    {
        public class SimpleTestrunnerScanner : ScannerBase
        {
            public SimpleTestrunnerScanner()
            {
                this.SetupSimpleScanner();
                this.AddIdentifierGenerator(
                    ( "test",  TokenType.TEST),
                    ( "testcase",  TokenType.TESTCASE));

                this.AddSingleCharTokenGenerators(('.', TokenType.DOT));
            }
        }

        public class SimpleTestrunnerCompiler : CompilerBase
        {
            public SimpleTestrunnerCompiler()
            {
                this.SetupSimpleCompiler();
                var testcaseCompilette = new TestcaseCompillette();
                var testdec = new TestDeclarationCompilette();
                testcaseCompilette.SetTestDeclarationCompilette(testdec);
                this.AddDeclarationCompilette(
                    testdec,
                    testcaseCompilette);
            }
        }

        public class SimpleTestrunnerProgram : ProgramBase<SimpleTestrunnerScanner, SimpleTestrunnerCompiler, Disassembler>
        {
        }

        public class SimpleTestrunnerVM : VMBase
        {
            public TestRunner TestRunner { get; protected set; } = new TestRunner(() => new SimpleTestrunnerVM());

            protected override bool ExtendedOp(OpCode opCode, Chunk chunk)
            {
                switch (opCode)
                {
                case OpCode.INVOKE:
                    DoInvokeOp(chunk);
                    break;
                case OpCode.TEST:
                    TestRunner.DoTestOpCode(this, chunk);
                    break;
                default:
                    return false;
                }
                return true;
            }
            public override void CopyFrom(IVm otherVM)
            {
                base.CopyFrom(otherVM);
                if (otherVM is SimpleTestrunnerVM matchingVM)
                    TestRunner = matchingVM.TestRunner;
            }

            private void DoInvokeOp(Chunk chunk)
            {
                var constantIndex = ReadByte(chunk);
                var methodName = chunk.ReadConstant(constantIndex).val.asString;
                var argCount = ReadByte(chunk);

                var receiver = Peek(argCount);
                switch (receiver.type)
                {
                case ValueType.Instance:
                    {
                        var inst = receiver.val.asInstance;

                        //it could be a field
                        if (inst.Fields.TryGetValue(methodName, out var fieldFunc))
                        {
                            _valueStack[_valueStack.Count - 1 - argCount] = fieldFunc;
                            PushCallFrameFromValue(fieldFunc, argCount);
                        }
                        else
                        {
                            var fromClass = inst.FromClass;
                            if (!fromClass.TryGetMethod(methodName, out var method))
                            {
                                throw new VMException($"No method of name '{methodName}' found on '{fromClass}'.");
                            }

                            PushCallFrameFromValue(method, argCount);
                        }
                    }
                    break;
                default:
                    throw new VMException($"Cannot invoke on '{receiver}'.");
                }
            }
        }

        public class SimpleTestrunnerEngine
        {
            public IProgram Program { get; private set; } = new SimpleTestrunnerProgram();
            private readonly SimpleTestrunnerVM _vm = new SimpleTestrunnerVM();
            public SimpleTestrunnerVM VM => _vm;

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

            public virtual void AddLibrary(IULoxLibrary lib)
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

                VM.SetGlobal(new HashedString("print"), Value.New(Print));
            }

            private NativeCallResult Print(VMBase vm, int argc)
            {
                var str = vm.GetArg(1).ToString();
                _logger(str);
                AppendResult(str);
                vm.PushReturn(Value.Null());
                return NativeCallResult.SuccessfulExpression;
            }

            protected void AppendResult(string str) => InterpreterResult += str;

            public string InterpreterResult { get; private set; } = string.Empty;
            public bool AllPassed => VM.TestRunner.AllPassed;
            public int TestsFound => VM.TestRunner.TestsFound;

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
            engine = new SimpleTestrunnerTestEngine(System.Console.WriteLine);
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
        public void Engine_TestCase_Print()
        {
            engine.AddLibrary(new AssertLibrary(() => new SimpleTestrunnerVM()));

            engine.Run(@"
test T
{
    testcase A
    {
        print(2==2);
    }
}");

            Assert.AreEqual("True", engine.InterpreterResult);
        }

        [Test]
        public void Engine_TestCase_Constants()
        {
            engine.AddLibrary(new AssertLibrary(() => new SimpleTestrunnerVM()));

            engine.Run(@"
test T
{
    testcase A
    {
        Assert.AreEqual(2,2);
    }
}");

            Assert.AreEqual("", engine.InterpreterResult);
        }

        [Test]
        [TestCase("Constants")]
        [TestCase("ControlFlow")]
        [TestCase("Functions")]
        [TestCase("Logic")]
        [TestCase("Math")]
        public void RunTestScript_NoFail(string fileNamePartial)
        {
            var noFailFiles = UloxScriptTestBase.GetFilesInSubFolder(NoFailUloxTests.NoFailFolderName);
            var file = noFailFiles.FirstOrDefault(x => x.Contains(fileNamePartial));

            var script = File.ReadAllText(file);
            engine.AddLibrary(new AssertLibrary(() => new SimpleTestrunnerVM()));
            engine.AddLibrary(new DebugLibrary());
            engine.Run(script);

            Assert.IsTrue(engine.AllPassed);
            Assert.AreNotEqual(0, engine.TestsFound, "Expect to find at least 1 test in the NoFail tests folder");
        }
    }
}