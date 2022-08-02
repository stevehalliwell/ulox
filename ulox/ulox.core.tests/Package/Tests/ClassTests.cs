using NUnit.Framework;

namespace ULox.Tests
{
    public class ClassTests : EngineTestBase
    {
        [Test]
        public void Delcared_WhenAccessed_ShouldHaveClassObject()
        {
            testEngine.Run(@"
class Brioche {}
print (Brioche);");

            Assert.AreEqual("<class Brioche>", testEngine.InterpreterResult);
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

            Assert.AreEqual("Attempted to Create a new field 'c' via SetField on a frozen object. This is not allowed.", testEngine.InterpreterResult);
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

            Assert.AreEqual("Attempted to Create a new field 'c' via SetField on a frozen object. This is not allowed.", testEngine.InterpreterResult);
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
        return this.brew;
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
        public void Engine_Class_Fields()
        {
            testEngine.Run(@"
class T{ }

T.a = 2;

print(T.a);");

            Assert.AreEqual("Attempted to Create a new field 'a' via SetField on a frozen object. This is not allowed.", testEngine.InterpreterResult);
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

            Assert.AreEqual("Type 'T', encountered element of stage 'Init' too late, type is at stage 'Method'. This is not allowed.", testEngine.InterpreterResult);
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

            Assert.AreEqual("Type 'T', encountered element of stage 'Var' too late, type is at stage 'Init'. This is not allowed.", testEngine.InterpreterResult);
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
        public void Engine_Method_Paramless()
        {
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
    _add(lhs,rhs){return V(lhs.a + rhs.a);}
}

var v1 = V(1);
var v2 = V(2);
var res = v1 + v2;
print(res.a);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        //[Test]
        //public void Field_WhenAddedToExistingInstance_ShouldSucceed()
        //{
        //    testEngine.Run(@"
        //class Toast {}
        //var toast = Toast();
        //print (toast.jam = ""grape"");");

        //    Assert.AreEqual("grape", testEngine.InterpreterResult);
        //}

        //[Test]
        //public void Field_WhenAddedToNewInstance_ShouldSucceed()
        //{
        //    testEngine.Run(@"
        //class Toast {}
        //Toast().a = 3;");

        //    Assert.AreEqual("", testEngine.InterpreterResult);
        //}

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
        public void Instance_WhenCreated_ShouldHaveInstanceObject()
        {
            testEngine.Run(@"
class Brioche {}
print (Brioche());");

            Assert.AreEqual("<inst Brioche>", testEngine.InterpreterResult);
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

//        [Test]
//        public void Method_WhenAddingFieldToSelf_ShouldSucceed()
//        {
//            testEngine.Run(@"
//class T
//{
//    Set(v)
//    {
//    this.a = v;
//    }
//}

//var t = T();
//t.Set(7);
//print (t.a);");

//            Assert.AreEqual("7", testEngine.InterpreterResult);
//        }

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
    Meth(){return this;}
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
    return 1;
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
    return 1;
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
    A{return this.a;}
}

var t = T();
t.Inc();
t.Inc();
print(t.A());
");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }
    }
}

//TODO add class test for init with args and no init and using local vars