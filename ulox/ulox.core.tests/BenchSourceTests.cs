using NUnit.Framework;
using ULox.Core.Bench;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class BenchSourceTests : EngineTestBase
    {
        [Test]
        [TestCaseSource(nameof(BenchScripts))]
        public void Run_BenchScripts_Clean(Script script)
        {
            testEngine.Run(script);
            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        public static Script[] BenchScripts = new Script[]
        {
            BenchmarkScripts.Loop,
            BenchmarkScripts.If,
            CompileVsExecute.Script,
            Vec2Variants.Type,
            Vec2Variants.Tuple,
            ObjectVsSoa.ObjectBasedScript,
            ObjectVsSoa.SoaBasedScript,
            ScriptVsNativeFunctional.FunctionalNative,
            ScriptVsNativeFunctional.FunctionalUlox,
        };
    }
}
