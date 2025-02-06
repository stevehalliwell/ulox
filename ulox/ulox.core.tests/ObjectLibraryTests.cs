using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class ObjectLibraryTests : EngineTestBase
    {
        [Test]
        public void Instance_WhenUnfrozen_ShouldActAsDynamic()
        {
            testEngine.Run(@"
class Pair {}

var pair = Pair();
Object.Unfreeze(pair);
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
Object.Freeze(foo);");

            StringAssert.Contains("Freeze attempted on unsupported type 'Double'", testEngine.InterpreterResult);
        }

        [Test]
        public void Freeze_WhenDynamic_ShouldBeIsFrozen()
        {
            testEngine.Run(@"
var foo = {=};
print(Object.IsFrozen(foo));
Object.Freeze(foo);
print(Object.IsFrozen(foo));
");

            Assert.AreEqual("FalseTrue", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Duplicate_ClassInstance_Matches()
        {
            testEngine.Run(@"
class Foo
{
    var Bar = ""Hello World!"";

    Speak(){print(this.Bar);}
}

var a = Foo();
var b = Object.Duplicate(a);
print(b);
b.Speak();
b.Bar = ""Bye"";
b.Speak();
a.Speak();");

            Assert.AreEqual("<inst Foo>Hello World!ByeHello World!", testEngine.InterpreterResult);
        }
    }
}
