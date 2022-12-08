using NUnit.Framework;
using ULox;

namespace ulox.core.tests
{
    public class EngineTestBase
    {
        protected ByteCodeInterpreterTestEngine testEngine;

        [SetUp]
        public void Setup()
        {
            testEngine = new ByteCodeInterpreterTestEngine(System.Console.WriteLine);
            (testEngine.MyEngine.Context.Program as Program).Optimiser.Enabled = false;
        }
    }
}