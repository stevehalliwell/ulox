using NUnit.Framework;

namespace ULox.Tests
{
    public class MixinTests : EngineTestBase
    {
        [Test]
        public void Mixin_WhenDeclared_ShouldCompileCleanly()
        {
            var expected = "1";
            var script = @"
class MixMe
{
    var a = 1;
}

class Foo 
{
    mixin MixMe;
}

print(1);";

            testEngine.Run(script);

            Assert.AreEqual(expected, testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenCombined_ShouldHaveMixinVar()
        {
            var expected = "1";
            var script = @"
class MixMe
{
    var a = 1,b,c;
}

class Foo 
{
    mixin MixMe;
}

var foo = Foo();
print(foo.a);
";

            testEngine.Run(script);

            Assert.AreEqual(expected, testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenCombined_ShouldHaveMixinVarAndSelf()
        {
            var expected = "11";
            var script = @"
class MixMe
{
    var a = 1,b,c;
}

class Foo 
{
    mixin MixMe;

    var e = 1,f,g;
}

var foo = Foo();
print(foo.a);
print(foo.e);
";

            testEngine.Run(script);

            Assert.AreEqual(expected, testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenCombinedAndNamesClash_ShouldHaveSelfVar()
        {
            var expected = "2";
            var script = @"
class MixMe
{
    var a = 1,b,c;
}

class Foo 
{
    mixin MixMe;
    var a = 2;
}

var foo = Foo();
print(foo.a);
";

            testEngine.Run(script);

            Assert.AreEqual(expected, testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenManyCombined_ShouldHaveAll()
        {
            var expected = "1234";
            var script = @"
class MixMe
{
    var a = 1;
}
class MixMe2
{
    var b = 2;
}
class MixMe3
{
    var c = 3;
}


class Foo 
{
    mixin 
        MixMe,
        MixMe2;
    mixin MixMe3;

    var d = 4;
}

var foo = Foo();
print(foo.a);
print(foo.b);
print(foo.c);
print(foo.d);
";

            testEngine.Run(script);

            Assert.AreEqual(expected, testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenCombined_ShouldHaveOriginalVar()
        {
            var expected = "2";
            var script = @"
class MixMe
{
    var a = 1,b,c;
}

class Foo 
{
    mixin MixMe;

    var bar = 2;
}

var foo = Foo();
print(foo.bar);
";

            testEngine.Run(script);

            Assert.AreEqual(expected, testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenCombined_ShouldHaveFlavourMethod()
        {
            var expected = "MixMe";
            var script = @"
class MixMe
{
    Speak(){print(cname);}
}

class Foo 
{
    mixin MixMe;
    var bar = 2;
}

var foo = Foo();
foo.Speak();
";

            testEngine.Run(script);

            Assert.AreEqual(expected, testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenCombined_ShouldHaveSelfMethod()
        {
            var expected = "Foo";
            var script = @"
class MixMe
{
    Speak(){print(cname);}
}

class Foo 
{
    mixin MixMe;
    Speaketh(){print(cname);}
}

var foo = Foo();
foo.Speaketh();
";

            testEngine.Run(script);

            Assert.AreEqual(expected, testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenMultipleCombined_ShouldHaveSelfMethod()
        {
            var expected = "Foo";
            var script = @"
class MixMe
{
    Speak(){print(cname);}
}

class MixMe2
{
    Speak(){print(cname);}
}

class Foo 
{
    mixin 
        MixMe,
        MixMe2;

    Speaketh(){print(cname);}
}

var foo = Foo();
foo.Speaketh();
";

            testEngine.Run(script);

            Assert.AreEqual(expected, testEngine.InterpreterResult);
        }


        [Test]
        public void Mixin_WhenCombinedAndNamesClash_ShouldHaveSelfMethod()
        {
            var expected = "Foo";
            var script = @"
class MixMe
{
    Speak(){print(cname);}
}

class Foo 
{
    mixin MixMe;

    Speak(){print(cname);}
}

var foo = Foo();
foo.Speak();
";

            testEngine.Run(script);

            Assert.AreEqual(expected, testEngine.InterpreterResult);
        }
    }
}