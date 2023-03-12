using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class ConditionalTests : EngineTestBase
    {
        [Test]
        public void If_WhenFalseSingleStatementBody_ShouldSkipAfter()
        {
            testEngine.Run(@"if(1 > 2) print (""ERROR""); print (""End"");");

            Assert.AreEqual("End", testEngine.InterpreterResult);
        }

        [Test]
        public void If_WhenFalseAndElse_ShouldHitElse()
        {
            testEngine.Run(@"
if(1 > 2)
    print (""ERROR"");
else
    print (""The "");
print (""End"");");

            Assert.AreEqual("The End", testEngine.InterpreterResult);
        }

        [Test]
        public void If_WhenCompoundLogicExpressions_ShouldHitFalse()
        {
            testEngine.Run(@"
if(1 > 2 or 2 > 3)
    print( ""ERROR"");
else if (1 == 1 and 2 == 2)
    print (""The "");
print (""End"");");

            Assert.AreEqual("The End", testEngine.InterpreterResult);
        }
    }
}
