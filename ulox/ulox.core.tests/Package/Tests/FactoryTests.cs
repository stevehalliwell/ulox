using NUnit.Framework;

namespace ULox.Tests
{
    public class FactoryTests : EngineTestBase
    {
        [Test]
        public void Factory_WhenGet_ShouldExist()
        {
            testEngine.Run(@"
Assert.IsNotNull(Factory);
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Line_WhenNotSet_ShouldThrow()
        {
            testEngine.Run(@"
Factory.Line(1);
");

            StringAssert.StartsWith("Factory contains no line of key '1' at ip:'1' in native:'Line'.", testEngine.InterpreterResult);
        }

        [Test]
        public void SetLine_WhenNullType_ShouldThrow()
        {
            testEngine.Run(@"
Factory.SetLine(null, null);
");

            StringAssert.StartsWith("'SetLine' must have non null key argument at ip:'1' in native:'SetLine'.", testEngine.InterpreterResult);
        }

        [Test]
        public void SetLine_WhenNullCreator_ShouldThrow()
        {
            testEngine.Run(@"
class Foo
{
}

Factory.SetLine(Foo, null);
");

            StringAssert.StartsWith("'SetLine' must have non null line argument at ip:'1' in native:'SetLine'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Line_WhenSet_ShouldReturnNonNull()
        {
            testEngine.Run(@"
class Foo
{
}

var dummyCreator = {:};

Factory.SetLine(Foo, dummyCreator);

var line = Factory.Line(Foo);

Assert.AreEqual(dummyCreator, line);
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Line_WhenSetSimpleDyn_ShouldReturnNonNull()
        {
            testEngine.Run(@"
class Foo
{
}

Factory.SetLine(Foo, {:});

var line = Factory.Line(Foo);

Assert.AreNotEqual(null, line);
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Create_WhenSetSimpleDyn_ShouldReturnNonNull()
        {
            testEngine.Run(@"
class Foo
{
}

Factory.SetLine(Foo, {:});
Factory.Line(Foo).Create = fun (){return Foo();};

var foo = Factory.Line(Foo).Create();

Assert.AreNotEqual(null, foo);
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Create_WhenSetSimpleDynInline_ShouldReturnNonNull()
        {
            testEngine.Run(@"
class Foo
{
}

Factory.SetLine(Foo, {Create: fun (){return Foo();}});

var foo = Factory.Line(Foo).Create();

Assert.AreNotEqual(null, foo);
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Create_WhenFooSet_ShouldReturnInstance()
        {
            testEngine.Run(@"
class Foo
{
}

class FooLine
{
    static Create()
    {
        return Foo();
    }
}

Factory.SetLine(Foo, FooLine);

var foo = Factory.Line(Foo).Create();

Assert.AreEqual(typeof(foo), Foo);
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
    }
}