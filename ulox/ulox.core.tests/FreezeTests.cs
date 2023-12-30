using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class FreezeTests : EngineTestBase
    {
        [Test]
        public void InstanceFromClass_WhenFrozenAndNonExistingFieldWritten_ShouldPreventChangeAndLog()
        {
            testEngine.Run(@"
class Foo
{
}

var inst = Foo();
inst.a = 10;");

            StringAssert.StartsWith("Attempted to create a new ", testEngine.InterpreterResult);
        }

        [Test]
        public void InstanceFromClass_WhenHasInitAndInitChainAndNonExistingFieldWritten_ShouldPreventChangeAndLog()
        {
            testEngine.Run(@"
class Foo
{
    var b = 2;
    init(){this.c = 3;}
}

var inst = Foo();
inst.a = 10;");

            StringAssert.StartsWith("Attempted to create a new ", testEngine.InterpreterResult);
        }

        [Test]
        public void Class_WhenFrozenAndNonExistingFieldWritten_ShouldPreventChangeAndLog()
        {
            testEngine.Run(@"
class Foo
{
}

freeze Foo;
Foo.a = 10;");

            StringAssert.StartsWith("Attempted to create a new ", testEngine.InterpreterResult);
        }

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
freeze foo;");

            StringAssert.StartsWith("Freeze attempted on unsupported type 'Double'", testEngine.InterpreterResult);
        }
    }
}
