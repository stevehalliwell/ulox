using NUnit.Framework;

namespace ulox.core.tests
{
    [TestFixture]
    public class OptimiserTests
    {
        protected ByteCodeInterpreterTestEngine testEngine;

        [SetUp]
        public void Setup()
        {
            testEngine = new ByteCodeInterpreterTestEngine(System.Console.WriteLine);
        }

        [Test]
        public void Optimiser_NothingToOptimise_DoesNothing()
        {
            testEngine.Run("print (1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            Assert.AreEqual(13, testEngine.MyEngine.Context.Program.CompiledScripts[0].TopLevelChunk.Instructions.Count);
        }

        [Test]
        public void Optimiser_UnusedLabel_RemovesDeadCode()
        {
            testEngine.Run(@"
label unused;
print (1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            Assert.AreEqual(13, testEngine.MyEngine.Context.Program.CompiledScripts[0].TopLevelChunk.Instructions.Count);
        }

        [Test]
        public void Optimiser_JumpOps_RemovesUnreachable()
        {
            testEngine.Run(@"
goto skip;
print(1);
label skip;
print(2);");

            Assert.AreEqual("2", testEngine.InterpreterResult);
            Assert.AreEqual(14, testEngine.MyEngine.Context.Program.CompiledScripts[0].TopLevelChunk.Instructions.Count);
        }

        [Test]
        public void Optimiser_JumpNowhere_RemovesUselessJump()
        {
            testEngine.Run(@"
goto skip;
label skip;
print(1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            Assert.AreEqual(13, testEngine.MyEngine.Context.Program.CompiledScripts[0].TopLevelChunk.Instructions.Count);
        }
    }
}
