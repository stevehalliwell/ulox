using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class MatchTests : EngineTestBase
    {
        [Test]
        public void Match_WhenBoolTrueAndFull_ShouldPass()
        {
            testEngine.Run(@"
var a = true;

match a
{
    true: print(1);
    false: print(2);
}");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Match_WhenBoolFalseAndFull_ShouldPass()
        {
            testEngine.Run(@"
var a = false;

match a
{
    true: print(1);
    false: print(2);
}
");
            
            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenBodyWithGlobals_ShouldPass()
        {
            testEngine.Run(@"
var a = true;
var b;
match a
{
    true: 
    {
        b = 1; 
        b += 2; 
        print(b);
    }
    false: print(2);
}
");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenBodyWithLocal_ShouldPass()
        {
            testEngine.Run(@"
var a = true;

match a
{
    true: 
    {
        var b = 1;
        print(b);
    }
    false: print(2);
}
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenBodyWithManyLocal_ShouldPass()
        {
            testEngine.Run(@"
var a = 7;

match a
{
    6: print(false);
    true: 
    {
        var b = 1;
        print(b);
    }
    7:
    {
        var c = 1;
        var d = a;
        var e = c+d;
        print(e);
    }
}
");

            Assert.AreEqual("8", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenBoolFalseAndMissing_ShouldThrow()
        {
            testEngine.ReThrow = true;
            void Act () => testEngine.Run(@"
var a = false;

match a
{
    true: print(1);
}
");

            var ex = Assert.Throws<RuntimeUloxException>(Act);
            StringAssert.Contains("Match on 'a' did have a matching case", ex.Message);
        }
        
        [Test]
        public void Match_WhenInt0AndFull_ShouldPass()
        {
            testEngine.Run(@"
var a = 0;

match a
{
    0: print(1);
    1: print(2);
}");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenInt1AndFull_ShouldPass()
        {
            testEngine.Run(@"
var a = 1;

match a
{
    0: print(1);
    1: print(2);
}");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenInt2AndUnmatched_ShouldFail()
        {
            testEngine.ReThrow = true;
            void Act() => testEngine.Run(@"
var a = 2;

match a
{
    0: print(1);
    1: print(2);
}");

            var ex = Assert.Throws<RuntimeUloxException>(Act);
            StringAssert.Contains("Match on 'a' did have a matching case", ex.Message);
        }

        [Test]
        public void Match_WhenInt2AndExpressionMatches_ShouldPass()
        {
            testEngine.Run(@"
var a = 2;

match a
{
    0+2: print(1);
    1: print(2);
}");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenString_ShouldPass()
        {
            testEngine.Run(@"
var a = ""Hello"";

match a
{
    ""Hello"": print(1);
    1: print(2);
}");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenStringAndReverseOrder_ShouldPass()
        {
            testEngine.Run(@"
var a = ""Hello"";

match a
{
    1: print(2);
    ""Hello"": print(1);
}");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenInt_ShouldPass()
        {
            testEngine.Run(@"
var a = 4;

match a
{
    0: print(0);
    1: print(1);
    2: print(2);
    3: print(3);
    4: print(4);
    5: print(5);
}");

            Assert.AreEqual("4", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenEnum_ShouldPass()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Baz
}

var f = Foo.Bar;

match f
{
    Foo.Bar: print(1);
    Foo.Baz: print(2);
}");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenEnumAndEmptyCase_ShouldPass()
        {
            testEngine.Run(@"
enum Foo
{
    Bar,
    Baz
}

var f = Foo.Bar;

match f
{
    Foo.Bar: ;
    Foo.Baz: print(2);
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Match_WhenIntWithBody_ShouldPass()
        {
            testEngine.Run(@"
var a = 4;

fun Foo(){retval = 10;}

match a
{
    0: 
    {
        var b = Foo() + 0;
        print(b);
    }
    1: 
    {
        var b = Foo() + 1;
        print(b);
    }
    2: 
    {
        var b = Foo() + 2;
        print(b);
    }
    3: 
    {
        var b = Foo() + 3;
        print(b);
    }
    4: 
    {
        var b = Foo() + 4;
        print(b);
    }
    5: 
    {
        var b = Foo() + 5;
        print(b);
    }
}");

            Assert.AreEqual("14", testEngine.InterpreterResult);
        }
    }
}
