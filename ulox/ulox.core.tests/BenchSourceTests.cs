using NUnit.Framework;
using ULox.Core.Bench;

namespace ULox.Core.Tests
{
    [TestFixture]
    [Ignore("Benchmarks are ignored by default. Run them manually if needed.")]
    public class BenchSourceTests : EngineTestBase
    {
        // [Test]
        // [TestCaseSource(nameof(BenchScripts))]
        // public void Run_BenchScripts_Clean(Script script)
        // {
        //     testEngine.Run(script);
        //     Assert.AreEqual("", testEngine.InterpreterResult);
        // }

        public static Script[] BenchScripts = new Script[]
        {
            BenchmarkScripts.Loop,
            BenchmarkScripts.If,
            CompileVsExecute.Script,
            Vec2Variants.Type,
            Vec2Variants.Tuple,
            ScriptVsNativeFunctional.FunctionalNative,
            ScriptVsNativeFunctional.FunctionalUlox,
        };
    }
}
