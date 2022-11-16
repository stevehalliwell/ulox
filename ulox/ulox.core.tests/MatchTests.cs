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
var a = true;

match a
{
    true: print(1);
    false: print(2);
}");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Match_WhenBoolFalseAndFull_ShouldPass()
        {
            testEngine.Run(@"

var a = false;

match a
{
    true: print(1);
    false: print(2);
}
");
            
            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenBoolFalseAndMissing_ShouldThrow()
        {
            void Act () => testEngine.Run(@"

var a = false;

match a
{
    true: print(1);
}
");

            var ex = Assert.Throws<PanicException>(Act);
            StringAssert.Contains("Match on 'a' did have a matching case", ex.Message);
        }
        
        [Test]
        public void Match_WhenInt0AndFull_ShouldPass()
        {
            testEngine.Run(@"
var a = 0;

match a
{
    0: print(1);
    1: print(2);
}");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenInt1AndFull_ShouldPass()
        {
            testEngine.Run(@"
var a = 1;

match a
{
    0: print(1);
    1: print(2);
}");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenInt2AndUnmatched_ShouldFail()
        {
            void Act() => testEngine.Run(@"
var a = 2;

match a
{
    0: print(1);
    1: print(2);
}");

            var ex = Assert.Throws<PanicException>(Act);
            StringAssert.Contains("Match on 'a' did have a matching case", ex.Message);
        }

        [Test]
        public void Match_WhenInt2AndExpressionMatches_ShouldPass()
        {
            testEngine.Run(@"
var a = 2;

match a
{
    0+2: print(1);
    1: print(2);
}");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenString_ShouldPass()
        {
            testEngine.Run(@"
var a = ""Hello"";

match a
{
    ""Hello"": print(1);
    1: print(2);
}");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }
    }
}
