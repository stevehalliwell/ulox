using NUnit.Framework;

namespace ULox.Tests
{
    public class FunctionalTests : EngineTestBase
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

        [Test]
        public void Local_WhenMethodWithThis_ShouldCompile()
        {
            testEngine.Run(@"
class T 
{
    var a = 1;

    local Foo()
    {
        return this.a;
    }
}

var t = T();
print(t.Foo());
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Pure_WhenFetchGlobal_ShouldThrow()
        {
            testEngine.Run(@"
pure fun Foo()
{
    a = 7;
}
");

            Assert.AreEqual("Identifiier 'a' could not be found locally in local function 'Foo'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Pure_WhenGetParamNamed_ShouldCompile()
        {
            testEngine.Run(@"
pure fun Foo(a)
{
    var b = a;
}
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Pure_WhenSetParamNamed_ShouldThrow()
        {
            testEngine.Run(@"
pure fun Foo(a)
{
    a = 7;
}
");

            Assert.AreEqual("Attempted to write to function param 'a', this is not allowed in a 'pure' function.", testEngine.InterpreterResult);
        }

        [Test]
        public void Pure_WhenNonPureFunctionArg_ShouldThrow()
        {
            testEngine.Run(@"
fun Meth()
{
    print(""Side Effect"");
}

pure fun Foo(a)
{
    a();
}

Foo(Meth);
");

            Assert.AreEqual("Pure call 'Foo' with non-pure confirming argument '<closure Meth upvals:0>'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Pure_WhenPureFunctionArg_ShouldCompile()
        {
            testEngine.Run(@"
pure fun Meth(lhs,rhs)
{
    return lhs+rhs;
}

pure fun Foo(a,b,c)
{
    return a(b,c);
}

var res = Foo(Meth,1,2);
print(res);
");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Pure_WhenParamNamedInClass_ShouldCompile()
        {
            testEngine.Run(@"
class T 
{
    pure Foo(a)
    {
        a = 7;
    }
}
");

            Assert.AreEqual("Attempted to write to function param 'a', this is not allowed in a 'pure' function.", testEngine.InterpreterResult);
        }

        [Test]
        public void Pure_WhenFetchGlobalInClass_ShouldThrow()
        {
            testEngine.Run(@"
class T 
{
    pure Foo()
    {
        a = 7;
    }
}
");

            Assert.AreEqual("Identifiier 'a' could not be found locally in local function 'Foo'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Pure_WhenUpValueInClass_ShouldThrow()
        {
            testEngine.Run(@"
class T
{
    Foo()
    {
        var a = 10;

        pure fun Bar()
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

        [Test]
        public void Pure_WhenMethodWithThis_ShouldThrow()
        {
            testEngine.Run(@"
class T 
{
    pure Foo()
    {
        return this.a;
    }
}
");

            Assert.AreEqual("Identifiier 'this' could not be found locally in local function 'Foo'.", testEngine.InterpreterResult);
        }
    }
}