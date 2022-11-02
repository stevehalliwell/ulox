using NUnit.Framework;

namespace ulox.core.tests
{
    public class ExpectTests : EngineTestBase
    {
        [Test]
        public void Expect_Truthy_Continues()
        {
            testEngine.Run(@"
expect true;"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Expect_TruthyWithMessage_Continues()
        {
            testEngine.Run(@"
expect true : ""a message"";"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
        [Test]
        public void Expect_Falsy_Aborts()
        {
            testEngine.Run(@"
expect false;"
            );

            StringAssert.Contains("Expect failed, got falsey at ip:'4'", testEngine.InterpreterResult);
        }

        [Test]
        public void Expect_FalsyWithMessage_AbortsWithMessage()
        {
            testEngine.Run(@"
expect false : ""a message"";"
            );

            StringAssert.Contains("Expect failed, got a message at ip:'5'", testEngine.InterpreterResult);
        }
    }
}
