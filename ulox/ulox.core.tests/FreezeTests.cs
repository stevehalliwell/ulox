using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class FreezeTests : EngineTestBase
    {
        [Test]
        public void Instance_WhenUnfrozen_ShouldActAsDynamic()
        {
            testEngine.Run(@"
class Pair {}

var pair = Pair();
Unfreeze(pair);
pair.first = 1;
pair.second = 2;
print( pair.first + pair.second);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Freeze_WhenNumber_ShouldFail()
        {
            testEngine.Run(@"
var foo = 7;
Freeze(foo);");

            StringAssert.StartsWith("Freeze attempted on unsupported type 'Double'", testEngine.InterpreterResult);
        }

        [Test]
        public void Freeze_WhenDynamic_ShouldBeIsFrozen()
        {
            testEngine.Run(@"
var foo = {=};
print(IsFrozen(foo));
Freeze(foo);
print(IsFrozen(foo));
");

            Assert.AreEqual("FalseTrue", testEngine.InterpreterResult);
        }
    }
}
