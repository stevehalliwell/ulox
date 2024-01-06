using NUnit.Framework;
using ULox.Core.Bench;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class ProfileSourceTests : EngineTestBase
    {
        [Test]
        [TestCaseSource(nameof(DivideCases))]
        public void Run_BouncingBallProfileScript(Script script)
        {
            testEngine.Run(script);
            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        public static Script[] DivideCases = new Script[]
        {
            BenchmarkScripts.Loop,
            BenchmarkScripts.If,
            CompileVsExecute.Script,
            Vec2Variants.Type,
            Vec2Variants.Tuple,
        };
    }
}
