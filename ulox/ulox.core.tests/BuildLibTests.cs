using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class BuildLibTests : EngineTestBase
    {
        [Test]
        public void Interpret_Nested_Clean()
        {
            testEngine.Run(@"
Build.Interpret(
""print(1);""
);
print(2);
"
            );

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        [Test]
        public void Interpret_Repeated_MultipleLogs()
        {
            testEngine.Run(@"
Build.ReinterpretOnEachCompile(true);
Build.Interpret(""print(1);"");
Build.Interpret(""print(1);"");
Build.Interpret(""print(1);"");
"
            );

            Assert.AreEqual("111", testEngine.InterpreterResult);
        }
    }
}
