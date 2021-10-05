using NUnit.Framework;

namespace ULox.Tests
{
    public class ClassTests : EngineTestBase
    {
        [Test]
        public void Engine_Class_Empty()
        {
            testEngine.Run(@"
class Brioche {}
print (Brioche);");

            Assert.AreEqual("<class Brioche>", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Instance_Empty()
        {
            testEngine.Run(@"
class Brioche {}
print (Brioche());");

            Assert.AreEqual("<inst Brioche>", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Instance_Method()
        {
            testEngine.Run(@"
class Brioche
{
    Meth(){print (""Method Called"");}
}
Brioche().Meth();");

            Assert.AreEqual("Method Called", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Instance_Method_This()
        {
            testEngine.Run(@"
class Brioche
{
    Meth(){return this;}
}

print (Brioche().Meth());");

            Assert.AreEqual("<inst Brioche>", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Instance_Simple0()
        {
            testEngine.Run(@"
class Toast {}
Toast().a = 3;");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Instance_Simple1()
        {
            testEngine.Run(@"
class Toast {}
var toast = Toast();
print (toast.jam = ""grape"");");

            Assert.AreEqual("grape", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Instance_Simple2()
        {
            testEngine.Run(@"
class Pair {}

var pair = Pair();
pair.first = 1;
pair.second = 2;
print( pair.first + pair.second);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Method_Simple1()
        {
            testEngine.Run(@"
class T
{
    Say(){print (7);}
}

var t = T();
t.Say();");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Method_Simple2()
        {
            testEngine.Run(@"
class T
{
    Say(){print (this.name);}
}

var t = T();
t.name = ""name"";
t.Say();");

            Assert.AreEqual("name", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Set_Existing_From_Const()
        {
            testEngine.Run(@"
class T
{
    Set()
{
this.a = 7;
}
}

var t = T();
t.a = 1;
t.Set();
print (t.a);");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Set_Existing_From_Arg()
        {
            testEngine.Run(@"
class T
{
    Set(v)
{
this.a = v;
}
}

var t = T();
t.a = 1;
t.Set(7);
print (t.a);");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Set_New_From_Const()
        {
            testEngine.Run(@"
class T
{
    Set()
{
this.name = 7;
}
    Say(){print (this.name);}
}

var t = T();
t.Set();
t.Say();");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Set_New_From_Arg()
        {
            testEngine.Run(@"
class T
{
    Set(v)
{
this.a = v;
}
}

var t = T();
t.Set(7);
print (t.a);");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Manual_Init_Simple1()
        {
            testEngine.Run(@"
class CoffeeMaker {
    Set(_coffee) {
        this.coffee = _coffee;
        return this;
    }

    brew() {
        print (""Enjoy your cup of "" + this.coffee);

        // No reusing the grounds!
        this.coffee = null;
    }
}

var maker = CoffeeMaker();
maker.Set(""coffee and chicory"");
maker.brew();");

            Assert.AreEqual("Enjoy your cup of coffee and chicory", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Init_Simple1()
        {
            testEngine.Run(@"
class CoffeeMaker {
    init(_coffee) {
        this.coffee = _coffee;
    }

    brew() {
        print (""Enjoy your cup of "" + this.coffee);

        // No reusing the grounds!
        this.coffee = null;
    }
}

var maker = CoffeeMaker(""coffee and chicory"");
maker.brew();");

            Assert.AreEqual("Enjoy your cup of coffee and chicory", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_BoundMethod()
        {
            testEngine.Run(@"
class CoffeeMaker {
    init(_coffee) {
        this.coffee = _coffee;
    }

    brew() {
        print (""Enjoy your cup of "" + this.coffee);

        // No reusing the grounds!
        this.coffee = null;
    }
}

var maker = CoffeeMaker(""coffee and chicory"");
var delegate = maker.brew;
delegate();");

            Assert.AreEqual("Enjoy your cup of coffee and chicory", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_BoundMethod_ViaReturn()
        {
            testEngine.Run(@"
class CoffeeMaker {
    init(_coffee) {
        this.coffee = _coffee;
    }

    brew() {
        print (""Enjoy your cup of "" + this.coffee);

        // No reusing the grounds!
        this.coffee = null;
    }
}

fun GetDelegate()
{
    var maker = CoffeeMaker(""coffee and chicory"");
    return maker.brew;
}

var delegate = GetDelegate();

delegate();");

            Assert.AreEqual("Enjoy your cup of coffee and chicory", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_BoundMethod_ViaReturn_InOtherObject()
        {
            testEngine.Run(@"
class CoffeeMaker {
    init(_coffee) {
        this.coffee = _coffee;
    }

    brew() {
        print (""Enjoy your cup of "" + this.coffee);

        // No reusing the grounds!
        this.coffee = null;
    }
}

class Nothing
{
    RunIt(del)
    {
        del();
    }
}

fun GetDelegate()
{
    var maker = CoffeeMaker(""coffee and chicory"");
    return maker.brew;
}

var delegate = GetDelegate();
var runner = Nothing();

runner.RunIt(delegate);");

            Assert.AreEqual("Enjoy your cup of coffee and chicory", testEngine.InterpreterResult);
        }

        //TODO: add test to version with params
        [Test]
        public void Engine_Class_BoundMethod_InternalAndReturn()
        {
            testEngine.Run(@"
class CoffeeMaker {
    init(_coffee) {
        this.coffee = _coffee;
    }

    brew() {
        print (""Enjoy your cup of "" + this.coffee);

        // No reusing the grounds!
        this.coffee = null;
    }

    brewLater()
    {
        return this.brew;
    }
}

var maker = CoffeeMaker(""coffee and chicory"");

var delegate = maker.brewLater();

delegate();");

            Assert.AreEqual("Enjoy your cup of coffee and chicory", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Field_As_Method()
        {
            testEngine.Run(@"
class CoffeeMaker {
    init(_coffee) {
        this.coffee = _coffee;
    }
}

var maker = CoffeeMaker(""coffee and chicory"");

    fun b() {
        print (""Enjoy your cup of coffee"");
    }

maker.brew = b;
maker.brew();");

            Assert.AreEqual("Enjoy your cup of coffee", testEngine.InterpreterResult);
        }

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
        public void Engine_Class_Init()
        {
            testEngine.Run(@"
class T
{
    init(){}
}

var t = T();
t.a = null;
print(t.a);");

            Assert.AreEqual("null", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_AutoInit()
        {
            testEngine.Run(@"
class T
{
    var a;
    init(a)
    {
        print(this.a);
    }
}

var t = T(3);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_AutoInit2()
        {
            testEngine.Run(@"
class T
{
    var a,b;
    init(a,b)
    {
        print(this.a);
        print(this.b);
    }
}

var t = T(1,2);");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_AutoInitReplacesDefaults()
        {
            testEngine.Run(@"
class T
{
    var a= 10, b = 20, c = 30;
    init(a,b)
    {
        print(this.a);
        print(this.b);
        print(this.c);
    }
}

var t = T(1,2);");

            Assert.AreEqual("1230", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Var()
        {
            testEngine.Run(@"
class T
{
    var a;
}

var t = T();
print(t.a);");

            Assert.AreEqual("null", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_2Var()
        {
            testEngine.Run(@"
class T
{
    var a;
    var b;
}

var t = T();
print(t.a);
print(t.b);");

            Assert.AreEqual("nullnull", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_MultiVar()
        {
            testEngine.Run(@"
class T
{
    var a,b;
}

var t = T();
print(t.a);
print(t.b);");

            Assert.AreEqual("nullnull", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_VarAfterInit_Throws()
        {
            testEngine.Run(@"
class T
{
    init(){}
    var a,b;
}

var t = T();
print(t.a);
print(t.b);");

            Assert.AreEqual("Class 'T', encountered element of stage 'Var' too late, class is at stage 'Init'. This is not allowed.", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_MethodBeforeInit_Throws()
        {
            testEngine.Run(@"
class T
{
    Meth(){}
    init(){}
}

var t = T();");

            Assert.AreEqual("Class 'T', encountered element of stage 'Init' too late, class is at stage 'Method'. This is not allowed.", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_VarInitChain()
        {
            testEngine.Run(@"
var aVal = 10;
class T
{
    var a = aVal;
}

var t = T();
print(t.a);");

            Assert.AreEqual("10", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_VarInitChain2()
        {
            testEngine.Run(@"
class T
{
    var a = 1, b = 2;
}

var t = T();
print(t.a);
print(t.b);");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_VarInitChain_AndInit()
        {
            testEngine.Run(@"
var aVal = 10;
class T
{
    var a = aVal;

    init(){this.a = this.a * 2;}
}

var t = T();
print(t.a);");

            Assert.AreEqual("20", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_VarInitChainEmpty_AndInit()
        {
            testEngine.Run(@"
class T
{
    var a;

    init(){this.a = 20;}
}

var t = T();
print(t.a);");

            Assert.AreEqual("20", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_VarInitChain_AndInitPrint()
        {
            testEngine.Run(@"
class T
{
    var a = 20;

    init(){print(this.a);}
}

var t = T();");

            Assert.AreEqual("20", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_VarWithInit()
        {
            testEngine.Run(@"
class T
{
    var a;

    init(_a) { this.a =_a; }
}

var t = T(1);
print(t.a);");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Fields()
        {
            testEngine.AddLibrary(new AssertLibrary(() => new Vm()));

            testEngine.Run(@"
class T{ }

T.a = 2;

print(T.a);");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_StaticFields()
        {
            testEngine.AddLibrary(new AssertLibrary(() => new Vm()));

            testEngine.Run(@"
class T
{
    static var a = 2;
}
print(T.a);");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_InherClassSuperInitNoParams()
        {
            testEngine.AddLibrary(new AssertLibrary(() => new Vm()));

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
            testEngine.AddLibrary(new AssertLibrary(() => new Vm()));

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
            testEngine.AddLibrary(new AssertLibrary(() => new Vm()));

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
            testEngine.AddLibrary(new AssertLibrary(() => new Vm()));

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
            testEngine.AddLibrary(new AssertLibrary(() => new Vm()));

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
            testEngine.AddLibrary(new AssertLibrary(() => new Vm()));

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

        [Test]
        public void Engine_CNAME_Usage()
        {
            testEngine.Run(@"
class Foo
{
    var n = cname;

    Method(){return cname;}
}

var f = Foo();

print(f.n);
print(f.Method());");

            Assert.AreEqual("FooFoo", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Duplicate_ClassInstance_Matches()
        {
            testEngine.Run(@"
class Foo
{
    var Bar = ""Hello World!"";

    Speak(){print(this.Bar);}
}

var a = Foo();
var b = Duplicate(a);
print(b);
b.Speak();
b.Bar = ""Bye"";
b.Speak();
a.Speak();");

            Assert.AreEqual("<inst Foo>Hello World!ByeHello World!", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Multiple_Scripts2()
        {
            testEngine.Run(@"
class A{MethA(){print (1);}}");

            testEngine.Run(@"var a = A();");
            testEngine.Run(@"a.MethA();");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Method_Paramless()
        {
            testEngine.AddLibrary(new AssertLibrary(() => new Vm()));

            testEngine.Run(@"
class T
{
    Meth
    {
        return 7;
    }
}

print(T().Meth());");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NoThis_Method_WorksAsStatic()
        {
            testEngine.Run(@"
class T
{
    NoMemberMethod()
    {
        return 7;
    }
}

print(T.NoMemberMethod());");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Static_Method_OnClass()
        {
            testEngine.Run(@"
class T
{
    static StaticMethod()
    {
        return 7;
    }
}

print(T.StaticMethod());");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Static_Method_OnInstance()
        {
            testEngine.Run(@"
class T
{
    static StaticMethod()
    {
        return 7;
    }
}

print(T().StaticMethod());");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_OperatorOverload_ClassAdd()
        {
            testEngine.Run(@"
class V
{
    var a;
    init(a){}
    _add(lhs,rhs){return V(lhs.a + rhs.a);}
}

var v1 = V(1);
var v2 = V(2);
var res = v1 + v2;
print(res.a);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }
    }
}
