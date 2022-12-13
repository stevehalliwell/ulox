using NUnit.Framework;

namespace ulox.core.tests
{
    [TestFixture]

    public class ByteCodeIteratorTests : EngineTestBase
    {
        private ULox.CompiledScript CompileByteCode(string str)
        {
            return testEngine.MyEngine.Context.CompileScript(new ULox.Script("test", str));
        }

        [Test]
        public void InstructionAndConstantCount_WhenVarInit_ShouldBeExpected()
        {
            var expectedInstructionCount = 7;
            var expectedConstantCount = 1;
            var compiled = CompileByteCode(@"
var i = 0;");

            Assert.AreEqual(expectedInstructionCount, compiled.TopLevelChunk.Instructions.Count);
            Assert.AreEqual(expectedConstantCount, compiled.TopLevelChunk.Constants.Count);
        }
    }
}
