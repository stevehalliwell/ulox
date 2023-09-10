﻿using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class ClassTests : EngineTestBase
    {
        [Test]
        public void ClassInstanceFields_WhenAccessed_ShouldHaveInitialValues()
        {
            testEngine.Run(@"
class Foo { var A = true, review, taste = ""Full""}
var b = Foo();
print (b.A);
print (b.review);
print (b.taste);");

            Assert.AreEqual("TruenullFull", testEngine.InterpreterResult);
        }

        [Test]
        public void ClassMixin_WhenCreated_ShouldHaveValues()
        {
            testEngine.Run(@"
class Foo {var A = true, review, taste = ""Full""}
class Bar {
    mixin Foo; 
var 
    B = 1;
}
var b = Bar();
print (b.A);
print (b.review);
print (b.taste);
print (b.B);");

            Assert.AreEqual("TruenullFull1", testEngine.InterpreterResult);
        }

        [Test]
        public void Delcared_WhenTrailingCommaInVarList_ShouldCompile()
        {
            testEngine.Run(@"
class Foo {var a,b,c,}
print (Foo);");

            Assert.AreEqual("<Class Foo>", testEngine.InterpreterResult);
        }

//        [Test]
//        public void Delcared_WhenVarOmittted_ShouldAssumeVars()
//        {
//            testEngine.Run(@"
//class Foo {a,b=1,c,}
//print (Foo().b);");

//            Assert.AreEqual("1", testEngine.InterpreterResult);
//        }


        [Test]
        public void Delcared_WhenAllStages_ShouldCompile()
        {
            testEngine.Run(@"
class Foo {var foo=1;}
class BarProto {var foo,a,b,c;}

class Bar
{
    static var a_static = 1;

    mixin Foo;
    
    signs BarProto;

    var a = 1, b = 2, c = 3;

    init()
    {
        this.a = 10;
    }

    Meth()
    {
        print (this.a + this.b + this.c);
    }
}

var bar = Bar();
print(bar.foo);
print(bar.a);
print(bar.b);
bar.Meth();
");

            Assert.AreEqual("110215", testEngine.InterpreterResult);
        }

        [Test]
        public void Delcared_WhenAccessed_ShouldHaveClassObject()
        {
            testEngine.Run(@"
class Brioche {}
print (Brioche);");

            Assert.AreEqual("<Class Brioche>", testEngine.InterpreterResult);
        }

        [Test]
        public void Class_Instance()
        {
            testEngine.Run(@"
class T
{
}

var t = T();
print(t);");

            Assert.AreEqual("<inst T>", testEngine.InterpreterResult);
        }

        [Test]
        public void Class_Instance_Init()
        {
            testEngine.Run(@"
class T
{
    init(){}
}

var t = T();
print(t);");

            Assert.AreEqual("<inst T>", testEngine.InterpreterResult);
        }

        [Test]
        public void Class_Instance_NoInit()
        {
            testEngine.Run(@"
class T
{
}

var t = T(7);");

            StringAssert.StartsWith("Expected zero args for class '<Class T>', as it does not have an 'init' method but got 1 args", testEngine.InterpreterResult);
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
        public void Engine_Class_AttemptToModifyInstance_ShouldThrow()
        {
            testEngine.Run(@"
class T
{
    var a;
    var b;
}

var t = T();
t.c = 10;");

            StringAssert.StartsWith("Attempted to Create a new field", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_AttemptToModifyClass_ShouldThrow()
        {
            testEngine.Run(@"
class T
{
    var a;
    var b;
}

T.c = 10;");

            StringAssert.StartsWith("Attempted to Create a new field", testEngine.InterpreterResult);
        }

        [Test]
        public void AutoInit_WhenSingleMatchingVarAndInitArgName_ShouldAssignThrough()
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
        public void Engine_Class_CannotReturnExpFromInit()
        {
            testEngine.Run(@"
class A 
{
    init(){return;}
}");

            Assert.AreEqual("Cannot return an expression from an 'init' in chunk 'init(test)' at 4:18 'return'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_This_OutsideClass()
        {
            testEngine.Run(@"
var a = this.a;");

            Assert.AreEqual("Cannot use the 'this' keyword outside of a class in chunk 'unnamed_chunk(test)' at 2:13 'this'.", testEngine.InterpreterResult);
        }

        [Test]
        public void AutoInit_WhenTwoMatchingVarAndInitArgNames_ShouldAssignThrough()
        {
            testEngine.Run(@"
class T
{
    var a,b;
    init(a,b)
    {
        print(this.a + this.b);
    }
}

var t = T(1,2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void AutoInit_WhenSubSetMatchVarAndInitArgNames_ShouldAssignThroughAndLeaveOthersDefault()
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
        retval = this.brew;
    }
}

var maker = CoffeeMaker(""coffee and chicory"");

var delegate = maker.brewLater();

delegate();");

            Assert.AreEqual("Enjoy your cup of coffee and chicory", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_BoundMethodWithParams_InternalAndReturn()
        {
            testEngine.Run(@"
class CoffeeMaker {
    init(_coffee) {
        this.coffee = _coffee;
    }

    brew(method) {
        print (""Enjoy your cup of "" + this.coffee + "" ("" + method  + "")"");

        // No reusing the grounds!
        this.coffee = null;
    }
}

var maker = CoffeeMaker(""coffee and chicory"");

var delegate = maker.brew;

delegate(""V60"");");

            Assert.AreEqual("Enjoy your cup of coffee and chicory (V60)", testEngine.InterpreterResult);
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
    retval = maker.brew;
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
    retval = maker.brew;
}

var delegate = GetDelegate();
var runner = Nothing();

runner.RunIt(delegate);");

            Assert.AreEqual("Enjoy your cup of coffee and chicory", testEngine.InterpreterResult);
        }

        [Test]
        public void FunctionInField_WhenAssignedAndCalled_ShouldReturnExpected()
        {
            testEngine.Run(@"
class CoffeeMaker {
var brew;
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
        public void Engine_Class_MethodBeforeInit_Throws()
        {
            testEngine.Run(@"
class T
{
    Meth(){}
    init(){}
}

var t = T();");

            Assert.AreEqual("Stage out of order. Type 'T' is at stage 'Method' has encountered a late 'Init' stage element in chunk 'unnamed_chunk(test)' at 5:9 'init'.", testEngine.InterpreterResult);
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

            Assert.AreEqual("Stage out of order. Type 'T' is at stage 'Init' has encountered a late 'Var' stage element in chunk 'unnamed_chunk(test)' at 5:8 'var'.", testEngine.InterpreterResult);
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
        public void Engine_CNAME_Usage()
        {
            testEngine.Run(@"
class Foo
{
    var n = cname;

    Method(){retval = cname;}
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
        public void Engine_Method_Paramless()
        {
            testEngine.Run(@"
class T
{
    Meth
    {
        retval = 7;
    }
}

print(T().Meth());");

            Assert.AreEqual("7", testEngine.InterpreterResult);
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
        public void Engine_OperatorOverload_ClassAdd()
        {
            testEngine.Run(@"
class V
{
    var a;
    init(a){}
    _add(lhs,rhs){retval = V(lhs.a + rhs.a);}
}

var v1 = V(1);
var v2 = V(2);
var res = v1 + v2;
print(res.a);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Init_WhenCreatingField_ShouldSucceed()
        {
            testEngine.Run(@"
class T
{
    init(){this.a = 1;}
}

var t = T();
print(t.a);");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Method_WhenAccessingSelfField_ShouldSucceed()
        {
            testEngine.Run(@"
class T
{
    init(){this.name = ""name"";}
    Say(){print (this.name);}
}
var t = T();
t.Say();");

            Assert.AreEqual("name", testEngine.InterpreterResult);
        }

        [Test]
        public void Method_WhenAssigningExistingFieldFromArg_ShouldSucceed()
        {
            testEngine.Run(@"
class T
{
    init(){this.a = 1;}
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
        public void Method_WhenAssigningExistingFieldFromConst_ShouldSucceed()
        {
            testEngine.Run(@"
class T
{
    init(){this.a = 1;}
    Set()
    {
     this.a = 7;
    }
}

var t = T();
t.Set();
print (t.a);");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Method_WhenAssigningExistingFieldFromConstAndInternalPrint_ShouldSucceed()
        {
            testEngine.Run(@"
class T
{
    init(){this.a = 1;}
    Set()
    {
        this.a = 7;
    }
    Say(){print (this.a);}
}

var t = T();
t.Set();
t.Say();");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Method_WhenCalledAndReturningThis_ShouldMatchSelf()
        {
            testEngine.Run(@"
class Brioche
{
    Meth(){retval = this;}
}

var b = Brioche();
print (b.Meth() == b);");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void Method_WhenCalledOnNewInstance_ShouldReturnExpectedValue()
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
        public void FuncCallMethod_WhenGivenDiffernTypesWithCompatLogic_ShouldSucceed()
        {
            testEngine.Run(@"
fun AddPrint(obj)
{
    obj.DoTheThing();
}

class T1
{
    var z,a=1,b=2;
    DoTheThing()
    {
        print (this.a + this.b);
    }
}
class T2
{
    var z,a=1,b=2;
    DoSomeOtherThing()
    {
        throw;
    }

    DoTheThing()
    {
        print (this.a + this.b);
    }
}

var t1 = T1();
var t2 = T2();

AddPrint(t1);
AddPrint(t2);
");

            Assert.AreEqual("33", testEngine.InterpreterResult);
        }

        [Test]
        public void InitWithoutArgsWithLocals_WhenCalled_ShouldSucceed()
        {
            testEngine.Run(@"
class T
{
    init()
    {
        var loc = 1;
        var c = loc + 2;
        ExtrnGlobal(c);
    }
}

fun PassedMethod()
{
    retval = 1;
}

fun ExtrnGlobal(in)
{
    print(in);
}

var t = T();
");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void InitWithArgsAndLocals_WhenCalled_ShouldSucceed()
        {
            testEngine.Run(@"
class T
{
    init(a,b)
    {
        var loc = a();
        var c = loc + b;
        ExtrnGlobal(c);
    }
}

fun PassedMethod()
{
    retval = 1;
}

fun ExtrnGlobal(in)
{
    print(in);
}

var t = T(PassedMethod, 2);
");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void InitWithManyArgsAndNoLocals_WhenCalled_ShouldSucceed()
        {
            testEngine.Run(@"
class T
{
    init(a,b,c,d,e,f,g)
    {
        print(d);
    }
}

var t = T(1,2,3,4,5,6,7);
");

            Assert.AreEqual("4", testEngine.InterpreterResult);
        }

        [Test]
        public void InitWithManyArgsAndOneLocals_WhenCalled_ShouldSucceed()
        {
            testEngine.Run(@"
class T
{
    init(a,b,c,d,e,f,g)
    {
        var h = 8;
        print(d);
        print(h);
    }
}

var t = T(1,2,3,4,5,6,7);
");

            Assert.AreEqual("48", testEngine.InterpreterResult);
        }

        [Test]
        public void MethodWithManyArgsAndOneLocals_WhenCalled_ShouldSucceed()
        {
            testEngine.Run(@"
class T
{
    Meth(a,b,c,d,e,f,g)
    {
        var h = 8;
        print(d);
        print(h);
    }
}

var t = T();
t.Meth(1,2,3,4,5,6,7);
");

            Assert.AreEqual("48", testEngine.InterpreterResult);
        }

        [Test]
        public void PropertyGet_WhenDeclaredAndCalledViaShortHand_ShouldReturnExpectedValue()
        {
            testEngine.Run(@"
class T
{
    var a = 0;
    Inc(){this.a = this.a + 1;}
    A{retval = this.a;}
}

var t = T();
t.Inc();
t.Inc();
print(t.A());
");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Engine_Class_StaticFields()
        {
            testEngine.Run(@"
class T
{
    static var a = 2;
}
print(T.a);");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_StaticFields_Modify()
        {
            testEngine.Run(@"
class T
{
    static var a = 2;
}
T.a = 1;

print(T.a);");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_StaticFields_WhenClassModified_ShouldThrow()
        {
            testEngine.Run(@"
class T
{
    static var a = 2;
}

T.b = 5;");

            StringAssert.StartsWith("Attempted to Create a new field", testEngine.InterpreterResult);
            StringAssert.Contains("on a frozen object.", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NoThis_Method_WorksAsStatic()
        {
            testEngine.Run(@"
class T
{
    NoMemberMethod()
    {
        retval = 7;
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
        retval = 7;
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
        retval = 7;
    }
}

print(T().StaticMethod());");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }
        
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