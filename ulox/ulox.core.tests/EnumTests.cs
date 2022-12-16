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
        public void Delcared_WhenPrinted_ShouldSayName()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Baz
}

print(Foo);");

            Assert.AreEqual("<Enum Foo>", testEngine.InterpreterResult);
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

        [Test]
        public void TypeOf_WhenEnum_ShouldBeEnum()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Baz,
}

print(typeof(Foo));
");

            Assert.AreEqual("<Enum Foo>", testEngine.InterpreterResult);
        }

        [Test]
        public void EnumValueEquality_WhenSameEnumValue_ShouldBeTrue()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Baz,
}

print(Foo.Bar == Foo.Bar);
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void EnumValueEquality_WhenDifferentEnumValue_ShouldBeFalse()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Baz,
}

print(Foo.Bar == Foo.Baz);
");

            Assert.AreEqual("False", testEngine.InterpreterResult);
        }

        [Test]
        public void EnumValue_WhenDotValue_ShouldBeHelloWorld()
        {
            testEngine.Run(@"
enum Foo
{
    Bar = ""Hello"",
    Baz = ""World"",
}

print(Foo.Bar.Value);
print(Foo.Baz.Value);
");

            Assert.AreEqual("HelloWorld", testEngine.InterpreterResult);
        }

        [Test]
        public void EnumValue_WhenDotName_ShouldBeBarBaz()
        {
            testEngine.Run(@"
enum Foo
{
    Bar = ""Hello"",
    Baz = ""World"",
}

print(Foo.Bar.Key);
print(Foo.Baz.Key);
");

            Assert.AreEqual("BarBaz", testEngine.InterpreterResult);
        }

        [Test]
        public void EnumValue_WhenDotEnum_ShouldBeTrue()
        {
            testEngine.Run(@"
enum Foo
{
    Bar = ""Hello"",
    Baz = ""World"",
}

print(Foo.Bar.Enum == Foo);
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void EnumMixin_WhenDifferentEnums_ShouldSeeAllOfAInB()
        {
            testEngine.Run(@"
enum A
{
    Bar,
    Baz,
}

enum B
{
    mixin A;
    Bax,
}

print(B.Bar);
print(B.Baz);
print(B.Bax);
");

            Assert.AreEqual("012", testEngine.InterpreterResult);
        }

        [Test]
        public void DotEnum_WhenMixinDifferentEnums_ShouldSeeMostSpecific()
        {
            testEngine.Run(@"
enum A
{
    Bar,
    Baz,
}

enum B
{
    mixin A;
    Bax,
}

print(B.Baz.Enum);
");

            Assert.AreEqual("B", testEngine.InterpreterResult);
        }

        [Test]
        public void FromValue_WhenExists_ShouldReturnMatch()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Baz,
}

print(Foo.FromValue(0).Key);
");

            Assert.AreEqual("Bar", testEngine.InterpreterResult);
        }

        [Test]
        public void FromValue_WhenDoesNotExists_ShouldReturnNull()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Baz,
}

print(Foo.FromValue(-1) == null);
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void All_WhenEmptyFoo_ShouldPrintNothing()
        {
            testEngine.Run(@"
enum Foo
{
}

var all = Foo.All;

loop (all)
{
    print(item);
}
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void All_WhenFoo_ShouldPrintAll()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Baz,
}

var all = Foo.All;

loop (all)
{
    print(item);
}
");

            Assert.AreEqual("BarBaz", testEngine.InterpreterResult);
        }

        [Test]
        public void EnumValue_WhenSetAttempted_ShouldFail()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Baz,
}

var fb = Foo.Bar;
fb.Value = 7;
");

            Assert.AreEqual("Attempted to Set field 'Value', but instance is read only.", testEngine.InterpreterResult);
        }
    }
}

//todo should print print the value and what we have now be the typeof result
//todo how else can user use readonly