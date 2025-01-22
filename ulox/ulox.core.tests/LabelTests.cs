using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class LabelTests : EngineTestBase
    {
        [Test]
        public void Goto_WhenValid_ShouldSkipPrint()
        {
            testEngine.Run(@"
goto end;
print(""fail"");
label end;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
    }
}
