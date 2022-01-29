using NUnit.Framework;

namespace ULox.Tests
{
    public class LocalTests : EngineTestBase
    {
        [Test]
        public void Local_WhenFetchGlobal_ShouldThrow()
        {
            testEngine.Run(@"
local fun Foo()
{
    a = 7;
}
");

            Assert.AreEqual("Identifiier 'a' could not be found locally in local function 'Foo'.", testEngine.InterpreterResult);
        }

        [Test]
        public void NoLocal_WhenUpValue_ShouldModify()
        {
            testEngine.Run(@"
fun Foo()
{
    var a = 10;

    fun Bar()
    {
        a = 7;
    }

    Bar();
    print(a);
}

Foo();");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Local_WhenUpValue_ShouldThrow()
        {
            testEngine.Run(@"
fun Foo()
{
    var a = 10;

    local fun Bar()
    {
        a = 7;
    }

    Bar();
    print(a);
}
");

            Assert.AreEqual("Identifiier 'a' could not be found locally in local function 'Bar'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Local_WhenParamNamed_ShouldCompile()
        {
            testEngine.Run(@"
local fun Foo(a)
{
    a = 7;
}
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Local_WhenParamNamedInClass_ShouldCompile()
        {
            testEngine.Run(@"
class T 
{
    local Foo(a)
    {
        a = 7;
    }
}
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Local_WhenFetchGlobalInClass_ShouldThrow()
        {
            testEngine.Run(@"
class T 
{
    local Foo()
    {
        a = 7;
    }
}
");

            Assert.AreEqual("Identifiier 'a' could not be found locally in local function 'Foo'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Local_WhenUpValueInClass_ShouldThrow()
        {
            testEngine.Run(@"
class T
{
    Foo()
    {
        var a = 10;

        local fun Bar()
        {
            a = 7;
        }

        Bar();
        print(a);
    }
}
");

            Assert.AreEqual("Identifiier 'a' could not be found locally in local function 'Bar'.", testEngine.InterpreterResult);
        }
    }
}