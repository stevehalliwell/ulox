using NUnit.Framework;

namespace ulox.core.tests
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
        public void Mixin_WhenManyCombinedAndMixinsOfMixins_ShouldHaveAll()
        {
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

class MixMeSub1
{
    var e = 5;
}

class MixMeSub2
{
    var f = 6;
}

class MixMe4
{
    mixin MixMeSub1, MixMeSub2;
    
    var g = 7;
}

class Foo 
{
    mixin 
        MixMe,
        MixMe2;
    mixin MixMe3;
    mixin MixMe4;

    var d = 4;
}

var foo = Foo();
print(foo.a);
print(foo.b);
print(foo.c);
print(foo.d);
print(foo.e);
print(foo.f);
print(foo.g);
";

            testEngine.Run(script);

            Assert.AreEqual("1234567", testEngine.InterpreterResult);
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
        public void Mixin_WhenCombined_ShouldHaveBoth()
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
        public void Mixin_WhenMultipleCombined_ShouldHaveAll()
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
        public void Mixin_WhenCombinedAndNamesClash_ShouldHaveAllPrint()
        {
            var script = @"

class MixMe
{
    Speak(){print(cname);}
}

class MixMe2
{
    Speak(){print(cname);}
}


class MixMe3
{
    Speak(){print(cname);}
}

class Foo 
{
    mixin MixMe, MixMe2, MixMe3;

    Speak(){print(cname);}
}

var foo = Foo();
foo.Speak();
";

            testEngine.Run(script);

            Assert.AreEqual("MixMeMixMe2MixMe3Foo", testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenInstanceMethodsCombinedAndNamesClash_ShouldHaveAllPrint()
        {
            var script = @"

class MixMe
{
    var MixMeName = cname;

    Speak(){print(this.MixMeName);}
}

class MixMe2
{
    var MixMeName2 = cname;

    Speak(){print(this.MixMeName2);}
}


class MixMe3
{
    Speak(){print(cname);}
}

class Foo 
{
    mixin MixMe, MixMe2, MixMe3;

    Speak(){print(cname);}
}

var foo = Foo();
foo.Speak();
";

            testEngine.Run(script);

            Assert.AreEqual("MixMeMixMe2MixMe3Foo", testEngine.InterpreterResult);
        }


        [Test]
        public void Mixin_WhenDuplicateFlavours_ShouldHaveOnlyOnePresent()
        {
            var script = @"

var globalCounter = 0;

class MixMe
{
    var a = (globalCounter += 1);
}

class Combo1
{
    mixin MixMe;
}

class Combo2
{
    mixin MixMe;
}

class Foo 
{
    mixin Combo1, Combo2;
}

var foo = Foo();
print(foo.a);
";

            testEngine.Run(script);

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void MixinInits_WhenMultipleInit_ShouldDoAll()
        {
            var script = @"
class Foo
{
    var fizz = 1, negative = -1;
}

class Bar
{
    var buzz = 2, bitcount;
}

class FooBar
{
    mixin Foo, Bar;

    init(fizz, buzz, bitcount){}
}

var expectedFizz = 10;
var expectedBuzz = 20;
var expectedBitcount = 30;
var result = -1;

var fooBar = FooBar(expectedFizz, expectedBuzz, expectedBitcount);
result = fooBar.fizz + fooBar.negative + fooBar.buzz + fooBar.bitcount;

print(result);
";

            testEngine.Run(script);

            Assert.AreEqual("59", testEngine.InterpreterResult);
        }
    }
}