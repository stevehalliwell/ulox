using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class SystemTypeTests : EngineTestBase
    {
        [Test]
        public void Delcared_WhenAccessed_ShouldHaveSystemObject()
        {
            testEngine.Run(@"
system Foo {}
print (Foo);");

            Assert.AreEqual("<System Foo>", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Delcared_WhenVar_ShouldNotCompile()
        {
            testEngine.Run(@"
system Foo
{
    var a=0;
}

print (Foo);");

            StringAssert.StartsWith("Expect method name", testEngine.InterpreterResult);
        }

        [Test]
        public void Delcared_WhenMethodCalled_ShouldRun()
        {
            testEngine.Run(@"
system Foo
{
    Bar()
    {
        retval = 7;
    }
}

print (Foo.Bar());");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Delcared_WhenAttemptToInit_ShouldFail()
        {
            testEngine.Run(@"
system Foo
{
    Bar()
    {
        retval = 7;
    }
}

var f = Foo();");

            StringAssert.StartsWith("Attempted to create an instance of the system 'Foo'", testEngine.InterpreterResult);
        }
    }
}
