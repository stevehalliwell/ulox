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

        [Test]
        public void DeepClone_CompiledScript_Match()
        {
            testEngine.Run(ObjectVsSoa.SoaBasedScript);

            var compiledScript = testEngine.MyEngine.Context.CompiledScripts[0];
            var clone = compiledScript.DeepClone();

            Assert.AreEqual(compiledScript.ScriptHash, clone.ScriptHash);
            Assert.AreEqual(compiledScript.AllChunks.Count, clone.AllChunks.Count);
            for (int i = 0; i < compiledScript.AllChunks.Count; i++)
            {
                var lhs = compiledScript.AllChunks[i];
                var rhs = clone.AllChunks[i];
                Assert.AreEqual(lhs.Constants.Count, rhs.Constants.Count);
                Assert.AreEqual(lhs.RunLengthLineNumbers.Count, rhs.RunLengthLineNumbers.Count);
                Assert.AreEqual(lhs.Labels.Count, rhs.Labels.Count);
                Assert.AreEqual(lhs.ChunkName, rhs.ChunkName);
                Assert.AreEqual(lhs.SourceName, rhs.SourceName);
                Assert.AreEqual(lhs.ContainingChunkChainName, rhs.ContainingChunkChainName);
            }
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
