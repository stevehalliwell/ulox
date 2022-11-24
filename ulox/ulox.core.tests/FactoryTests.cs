using NUnit.Framework;

namespace ulox.core.tests
{
    public class FactoryTests : EngineTestBase
    {
        [Test]
        public void Line_WhenNotSet_ShouldThrow()
        {
            testEngine.Run(@"
factory Line(1);
");

            StringAssert.StartsWith("Factory contains no line of key 'Line' at ip:'3' in chunk", testEngine.InterpreterResult);
        }

        [Test]
        public void SetLine_WhenNullCreator_ShouldThrow()
        {
            testEngine.Run(@"
factoryline Foo null;
");

            StringAssert.StartsWith("Factory line of key 'Foo' attempted to be set to null. Not allowed. at ip:'4' in chunk", testEngine.InterpreterResult);
        }

        [Test]
        public void Line_WhenSet_ShouldReturnNonNull()
        {
            testEngine.Run(@"
fun Fac(){}
factoryline Foo Fac;

var line = factory Foo;

Assert.AreEqual(Fac, line);
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Create_WhenSetSimpleDynWithFactorySyntax_ShouldReturnNonNull()
        {
            testEngine.Run(@"
class Foo
{
}

fun FooCreator() {return Foo();}

factoryline Foo FooCreator;

var fooLine = factory Foo;
var foo = fooLine();

Assert.AreNotEqual(null, foo);
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
        [Test]
        public void Create_WhenSetSimpleDynWithFactorySyntaxAndInlineFunction_ShouldReturnNonNull()
        {
            testEngine.Run(@"
class Foo
{
}

factoryline Foo fun () {return Foo();};

var fooLine = factory Foo;
var foo = fooLine();

Assert.AreNotEqual(null, foo);
Assert.AreEqual(typeof(foo), Foo);
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
    }
}