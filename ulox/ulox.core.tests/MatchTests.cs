using NUnit.Framework;
using ULox;

namespace ulox.core.tests
{
    public class MatchTests : EngineTestBase
    {
        [Test]
        public void Match_WhenBoolTrueAndFull_ShouldPass()
        {
            testEngine.Run(@"
{
var a = true;

match a
{
    true: print(1);
    false: print(2);
}
}");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Match_WhenBoolFalseAndFull_ShouldPass()
        {
            testEngine.Run(@"
{
var a = false;

match a
{
    true: print(1);
    false: print(2);
}
}");
            
            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenBoolFalseAndMissing_ShouldThrow()
        {
            void Act () => testEngine.Run(@"
{
var a = false;

match a
{
    true: print(1);
}
}");

            var ex = Assert.Throws<PanicException>(Act);
            StringAssert.Contains("Match on 'a' did have a matching case", ex.Message);
        }
    }
}
