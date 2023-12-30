using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]

    public class ByteCodeIteratorTests : EngineTestBase
    {
        private CompiledScript CompileByteCode(string str)
        {
            return testEngine.MyEngine.Context.CompileScript(new Script("test", str));
        }

        [Test]
        public void InstructionAndConstantCount_WhenVarInit_ShouldBeExpected()
        {
            var expectedInstructionCount = 4;
            var expectedConstantCount = 1;
            var compiled = CompileByteCode(@"
var i = 0;");

            Assert.AreEqual(expectedInstructionCount, compiled.TopLevelChunk.Instructions.Count);
            Assert.AreEqual(expectedConstantCount, compiled.TopLevelChunk.Constants.Count);
        }
    }
}
