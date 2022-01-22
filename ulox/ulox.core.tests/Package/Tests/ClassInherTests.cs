using NUnit.Framework;

namespace ULox.Tests
{
    public class ClassInherTests : EngineTestBase
    {


        [Test]
        public void Engine_Class_Inher_Simple1()
        {
            testEngine.Run(@"
class A{MethA(){print (1);}}
class B < A {MethB(){print (2);}}

var b = B();
b.MethA();
b.MethB();");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Inher_Simple2()
        {
            testEngine.Run(@"
class A{MethA(){print (1);}}
class B < A {MethB(){this.MethA();print (2);}}

var b = B();
b.MethB();");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Inher_Poly()
        {
            testEngine.Run(@"
class A{MethA(){print (1);}}
class B < A {MethA(){print (2);}}

var b = B();
b.MethA();");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_CannotInherSelf()
        {
            testEngine.Run(@"
class A < A {}");

            Assert.AreEqual("A class cannot inher from itself. 'A' inherits from itself, not allowed.", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_CannotReturnExpFromInit()
        {
            testEngine.Run(@"
class A 
{
    init(){return 7;}
}");

            Assert.AreEqual("Cannot return an expression from an 'init'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_This_OutsideClass()
        {
            testEngine.Run(@"
var a = this.a;");

            Assert.AreEqual("Cannot use this outside of a class declaration.", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Super_OutsideClass()
        {
            testEngine.Run(@"
var a = super.a;");

            Assert.AreEqual("Cannot use super outside a class.", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Inher_Super()
        {
            testEngine.Run(@"
class A{MethA(){print (1);}}
class B < A {MethA(){super.MethA(); print (2);}}

var b = B();
b.MethA();");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Inher_Super_BoundReturn()
        {
            testEngine.Run(@"
class A{MethA(){print (1);}}
class B < A { MethA()
{
var bound = super.MethA;
print (2);
bound();
}
}

var b = B();
b.MethA();");

            Assert.AreEqual("21", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_InherClassSuperInitNoParams()
        {
            testEngine.MyEngine.Context.AddLibrary(new AssertLibrary(() => new Vm()));

            testEngine.Run(@"
class Base
{
    init()
    {
        this.a = 1;
    }
}

var binst = Base();

print(binst.a);

class Child < Base
{
    init()
    {
        this.b = 2;
    }
}

var cinst = Child();

print(cinst.a);
print(cinst.b);
");

            Assert.AreEqual("112", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_InherClassSuperInitParams()
        {
            testEngine.MyEngine.Context.AddLibrary(new AssertLibrary(() => new Vm()));

            testEngine.Run(@"
class Base
{
    init(a)
    {
        this.a = a;
    }
}

var binst = Base(1);

print(binst.a);

class Child < Base
{
    init(a,b)
    {
        this.b = b;
    }
}

var cinst = Child(1,2);

print(cinst.a);
print(cinst.b);
");

            Assert.AreEqual("112", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Inher2ClassSuperInitParams()
        {
            testEngine.Run(@"
class Base
{
    init(a)
    {
        this.a = a;
    }
}

var binst = Base(1);

print(binst.a);

class Child < Base
{
    init(a,b)
    {
        this.b = b;
    }
}

var cinst = Child(1,2);

print(cinst.a);
print(cinst.b);

class Childer < Child
{
    init(a,b,c)
    {
        this.c = c;
    }
}

var cerinst = Childer(1,2,3);

print(cerinst.a);
print(cerinst.b);
print(cerinst.c);
");

            Assert.AreEqual("112123", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_InherWithVarNoInit()
        {
            testEngine.MyEngine.Context.AddLibrary(new AssertLibrary(() => new Vm()));

            testEngine.Run(@"
class Base
{
    var a = 1;
}

var binst = Base();

print(binst.a);

class Child < Base
{
    var b = 2;
}

var cinst = Child();

print(cinst.a);
print(cinst.b);
");

            Assert.AreEqual("112", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_InherWithVarAndInitNoParams()
        {
            testEngine.MyEngine.Context.AddLibrary(new AssertLibrary(() => new Vm()));

            testEngine.Run(@"
class Base
{
    var a = 1,b;
    init()
    {
        this.b = 2;
    }
}

var binst = Base();

print(binst.a);
print(binst.b);

class Child < Base
{
    var c = 3,d;
    init()
    {
        this.d = 4;
    }
}

var cinst = Child();

print(cinst.a);
print(cinst.b);
print(cinst.c);
print(cinst.d);
");

            Assert.AreEqual("121234", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_InherWithVarAndInitAndParams_NoAutoVarInit()
        {
            testEngine.MyEngine.Context.AddLibrary(new AssertLibrary(() => new Vm()));

            testEngine.Run(@"
class Base
{
    var a = 1;
    init(b)
    {
        this.b = b;
    }
}

var binst = Base(2);

print(binst.a);
print(binst.b);

class Child < Base
{
    var c = 3;
    init(b,d)
    {
        this.d = d;
    }
}

var cinst = Child(2,4);

print(cinst.a);
print(cinst.b);
print(cinst.c);
print(cinst.d);
");

            Assert.AreEqual("121234", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_InherWithVarAndInitAndAutoVarParams()
        {
            testEngine.MyEngine.Context.AddLibrary(new AssertLibrary(() => new Vm()));

            testEngine.Run(@"
class Base
{
    var a = 1,b;
    init(b){}
}

var binst = Base(2);

print(binst.a);
print(binst.b);

class Child < Base
{
    var c = 3,d;
    init(b,d){}
}

var cinst = Child(2,4);

print(cinst.a);
print(cinst.b);
print(cinst.c);
print(cinst.d);
");

            Assert.AreEqual("121234", testEngine.InterpreterResult);
        }
    }
}
