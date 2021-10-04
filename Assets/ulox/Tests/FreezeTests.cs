using NUnit.Framework;

namespace ULox.Tests
{
    public class FreezeTests : EngineTestBase
    {
        [Test]
        public void InstanceFromClass_WhenFrozenAndNonExistingFieldWritten_ShouldPreventChangeAndLog()
        {
            var expected = "Attempted to Create a new field 'a' via SetField on a frozen object. This is not allowed.";
            var script = @"
class Foo 
{
}


var inst = Foo();
freeze inst;
inst.a = 10;";

            testEngine.Run(script);

            Assert.AreEqual(expected, testEngine.InterpreterResult);
        }
        [Test]
        public void Class_WhenFrozenAndNonExistingFieldWritten_ShouldPreventChangeAndLog()
        {
            var expected = "Attempted to Create a new field 'a' via SetField on a frozen object. This is not allowed.";
            var script = @"
class Foo 
{
}

freeze Foo;
Foo.a = 10;";

            testEngine.Run(script);

            Assert.AreEqual(expected, testEngine.InterpreterResult);
        }
    }
}