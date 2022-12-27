using NUnit.Framework;

namespace ulox.core.tests
{
    public class FactoryTests : EngineTestBase
    {
        [Test]
        public void Line_WhenSet_ShouldReturnNonNull()
        {
            testEngine.Run(@"
fun Fac(){}
register Foo Fac;

var line = inject Foo;

Assert.AreEqual(Fac, line);
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Factory_WhenViaRegisterAndInject_ShouldReturnObject()
        {
            testEngine.Run(@"
class Foo {}
register FooFac fun(){return Foo();};

var obj = inject FooFac();

Assert.AreEqual(typeof(obj), Foo);
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }


        [Test]
        public void Factory_WhenMultiSimpleReg_ShouldReturnExpected()
        {
            testEngine.Run(@"
register Fac1 fun(){return 1;};
register Fac2 fun(){return 2;};

var val = inject Fac2();

print(val);
");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Create_WhenSetSimpleDynWithFactorySyntax_ShouldReturnNonNull()
        {
            testEngine.Run(@"
class Foo
{
}

fun FooCreator() {return Foo();}

register Foo FooCreator;

var fooLine = inject Foo;
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

register Foo fun () {return Foo();};

var fooLine = inject Foo;
var foo = fooLine();

Assert.AreNotEqual(null, foo);
Assert.AreEqual(typeof(foo), Foo);
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
    }
}