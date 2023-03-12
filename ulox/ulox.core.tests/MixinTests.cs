using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class MixinTests : EngineTestBase
    {
        [Test]
        public void Mixin_WhenDeclared_ShouldCompileCleanly()
        {
            testEngine.Run(@"
class MixMe
{
    var a = 1;
}

class Foo 
{
    mixin MixMe;
}

print(1);");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenCombined_ShouldHaveMixinVar()
        {
            testEngine.Run(@"
class MixMe
{
    var a = 1,b,c;
}

class Foo 
{
    mixin MixMe;
}

var foo = Foo();
print(foo.a);");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenCombined_ShouldHaveMixinVarAndSelf()
        {
            testEngine.Run(@"
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
print(foo.e);");

            Assert.AreEqual("11", testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenCombinedAndNamesClash_ShouldHaveSelfVar()
        {
            testEngine.Run(@"
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
print(foo.a);");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenManyCombined_ShouldHaveAll()
        {
            testEngine.Run(@"
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
print(foo.d);");

            Assert.AreEqual("1234", testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenManyCombinedAndMixinsOfMixins_ShouldHaveAll()
        {
            testEngine.Run(@"
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
print(foo.g);");

            Assert.AreEqual("1234567", testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenCombined_ShouldHaveOriginalVar()
        {
            testEngine.Run(@"
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
print(foo.bar);");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenCombined_ShouldHaveFlavourMethod()
        {
            testEngine.Run(@"
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
foo.Speak();");

            Assert.AreEqual("MixMe", testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenCombined_ShouldHaveBoth()
        {
            testEngine.Run(@"
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
foo.Speaketh();");

            Assert.AreEqual("Foo", testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenMultipleCombined_ShouldHaveAll()
        {
            testEngine.Run(@"
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
foo.Speaketh();");

            Assert.AreEqual("Foo", testEngine.InterpreterResult);
        }


        [Test]
        public void Mixin_WhenCombinedAndNamesClash_ShouldHaveAllPrint()
        {
            testEngine.Run(@"

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
foo.Speak();");

            Assert.AreEqual("MixMeMixMe2MixMe3Foo", testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenInstanceMethodsCombinedAndNamesClash_ShouldHaveAllPrint()
        {
            testEngine.Run(@"
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
foo.Speak();");

            Assert.AreEqual("MixMeMixMe2MixMe3Foo", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Mixin_WhenDuplicateFlavours_ShouldHaveOnlyOnePresent()
        {
            testEngine.Run(@"
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
print(foo.a);");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenDataAndDuplicateFlavours_ShouldHaveOnlyOnePresent()
        {
            testEngine.Run(@"
var globalCounter = 0;

data MixMe
{
    a = (globalCounter += 1);
}

data Combo1
{
    mixin MixMe;
    b = 1;
}

data Combo2
{
    mixin MixMe;
    c = 2;
}

data Foo 
{
    mixin Combo1, Combo2;
}

var foo = Foo();
print(foo.a);
print(foo.b);
print(foo.c);");

            Assert.AreEqual("112", testEngine.InterpreterResult);
        }

        [Test]
        public void MixinInits_WhenMultipleInit_ShouldDoAll()
        {
            testEngine.Run(@"
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

print(result);");

            Assert.AreEqual("59", testEngine.InterpreterResult);
        }
    }
}