using NUnit.Framework;

namespace ulox.core.tests
{
    public class ExpectTests : EngineTestBase
    {
        [Test]
        public void Expect_TruthyVar_Continues()
        {
            testEngine.Run(@"
var a = true;
expect a;"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
        [Test]
        public void Expect_TruthyLiteral_Continues()
        {
            testEngine.Run(@"
expect true;"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Expect_TruthyMultiPartExpression_Continues()
        {
            testEngine.Run(@"
expect true and 1 == 2/2;"
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
        public void Expect_TruthyMultiPartExpressionWithMessage_Continues()
        {
            testEngine.Run(@"
expect true and 1 == 2/2 : ""a message"";"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Expect_Falsy_Aborts()
        {
            testEngine.Run(@"
expect false;"
            );

            StringAssert.Contains("Expect failed, got falsey at ip:'3'", testEngine.InterpreterResult);
        }

        [Test]
        public void Expect_FalsyWithMessage_AbortsWithMessage()
        {
            testEngine.Run(@"
expect false : ""a message"";"
            );

            StringAssert.Contains("Expect failed, got a message at ip:'3'", testEngine.InterpreterResult);
        }

        [Test]
        public void Expect_FalsyMultiPartExpression_Aborts()
        {
            testEngine.Run(@"
expect true and 1 == 2;"
            );

            StringAssert.Contains("Expect failed, got falsey at ip:", testEngine.InterpreterResult);
        }

        [Test]
        public void Expect_FalsyMultiPartExpressionWithMessage_AbortsWithMessage()
        {
            testEngine.Run(@"
expect true and 1 == 2: ""a message"";"
            );

            StringAssert.Contains("Expect failed, got a message at ip:", testEngine.InterpreterResult);
        }

        [Test]
        public void Expect_TruthyWithMessageThenFalsey_Aborts()
        {
            testEngine.Run(@"
expect true : ""a message"",
        false;"
            );

            StringAssert.Contains("Expect failed, got falsey at ip:", testEngine.InterpreterResult);
        }

        [Test]
        public void Expect_TruthyWithMessageThenFalseyMultiPartExpressionWithMessage_Aborts()
        {
            testEngine.Run(@"
expect true : ""a message"",
        true and 1 == 2: ""a message"";"
            );

            StringAssert.Contains("Expect failed, got a message at ip:", testEngine.InterpreterResult);
        }
    }
}
//multi expect