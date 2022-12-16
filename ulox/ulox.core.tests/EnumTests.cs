using NUnit.Framework;

namespace ulox.core.tests
{
    public class EnumTests : EngineTestBase
    {
        [Test]
        public void Delcared_WhenEmpty_ShouldPass()
        {
            testEngine.Run(@"
enum Foo
{
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Delcared_When2Values_ShouldPass()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Baz
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Declared_WhenGivenLitterals_ShouldPass()
        {
            testEngine.Run(@"
enum Foo
{
    Bar = 7,
    Baz = 8
}
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Auto0th_WhenPrinted_ShouldBe0()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Baz
}

print(Foo.Bar);");

            Assert.AreEqual("<EnumValue Foo.Bar (0)>", testEngine.InterpreterResult);
        }

        [Test]
        public void Declare_WhenDupKey_ShouldFail()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Bar
}");

            StringAssert.StartsWith("Duplicate Enum Key 'Bar'", testEngine.InterpreterResult);
        }

        [Test]
        public void Valued_WhenPrinted_ShouldBe7()
        {
            testEngine.Run(@"
enum Foo
{
    Bar = 7,
    Baz = 8,
}

print(Foo.Bar);");

            Assert.AreEqual("<EnumValue Foo.Bar (7)>", testEngine.InterpreterResult);
        }

        [Test]
        public void ValuedFollowingAuto_WhenPrinted_ShouldBe8()
        {
            testEngine.Run(@"
enum Foo
{
    Bar = 7,
    Baz = 8
}

print(Foo.Baz);");

            Assert.AreEqual("<EnumValue Foo.Baz (8)>", testEngine.InterpreterResult);
        }

        [Test]
        public void ValuedNonNumber_WhenPrinted_ShouldBeHello()
        {
            testEngine.Run(@"
enum Foo
{
    Bar = ""Hello"",
    Baz = ""World"",
}

print(Foo.Bar);");

            Assert.AreEqual("<EnumValue Foo.Bar (Hello)>", testEngine.InterpreterResult);
        }

        [Test]
        public void Valued_WhenWrite_ShouldError()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Baz
}

Foo.Bar = 1;");

            Assert.AreEqual("Attempted to Set field 'Bar', but instance is read only.", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Var_WhenAssignedFromEnum_ShouldBe0()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Baz
}

var b = Foo.Bar;
print(b);");

            Assert.AreEqual("<EnumValue Foo.Bar (0)>", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenEnumValues_Should()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Baz
}

var b = Foo.Bar;

match b
{
Foo.Bar: print(""Bar"");
Foo.Baz: print(""Baz"");
}");

            Assert.AreEqual("Bar", testEngine.InterpreterResult);
        }

        [Test]
        public void Delcared_When2ValuesAndTrailingComma_ShouldPass()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Baz,
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
    }
}

//todo type of enum is enum, not clas or instance
//todo should print print the value and what we have now be the typeof result
//todo how do we combine enums?