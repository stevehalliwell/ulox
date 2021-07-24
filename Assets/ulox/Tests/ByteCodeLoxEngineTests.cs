using NUnit.Framework;
using UnityEngine;

//TODO 
//      vm find func with arity is unused
//      

namespace ULox.Tests
{


    public class ByteCodeLoxEngineTests
    {
        private ByteCodeInterpreterTestEngine engine;

        [SetUp]
        public void Setup()
        {
            engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);
        }

        public static Chunk GenerateManualChunk()
        {
            var chunk = new Chunk("main");

            chunk.AddConstantAndWriteInstruction(Value.New(0.5), 1);
            chunk.AddConstantAndWriteInstruction(Value.New(1), 1);
            chunk.WriteSimple(OpCode.NEGATE, 1);
            chunk.WriteSimple(OpCode.ADD, 1);
            chunk.AddConstantAndWriteInstruction(Value.New(2), 1);
            chunk.WriteSimple(OpCode.MULTIPLY, 1);
            chunk.WriteSimple(OpCode.RETURN, 2);

            return chunk;
        }

        [Test]
        public void Manual_Chunk_Disasemble()
        {
            var chunk = GenerateManualChunk();
            var dis = new Disassembler();

            dis.DoChunk(chunk);

            Debug.Log(dis.GetString());
        }

        [Test]
        public void Manual_Chunk_VM()
        {
            var chunk = GenerateManualChunk();
            VM vm = new VM();

            Assert.AreEqual(InterpreterResult.OK, vm.Interpret(chunk));
        }

        [Test]
        public void Engine_Cycle_Math_Expression()
        {
            

            engine.Run("print (1+2);");

            Assert.AreEqual("3", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Minus_Equals_Expression()
        {
            engine.Run(@"
var a = 1;
a -= 2;
print (a);");

            Assert.AreEqual("-1", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Logic_Not_Expression()
        {
            

            engine.Run("print (!true);");

            Assert.AreEqual("False",engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Logic_Compare_Expression()
        {

            engine.Run("print (1 < 2 == false);");

            Assert.AreEqual("False", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_String_Add_Expression()
        {
            

            engine.Run("print (\"hello\" + \" \" + \"world\");");

            Assert.AreEqual("hello world", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Print_Math_Expression()
        {
            

            engine.Run("print (1 + 2 * 3);");

            Assert.AreEqual("7", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Global_Var()
        {
            

            engine.Run(@"var myVar = 10; 
var myNull; 
print (myVar); 
print (myNull);

var myOtherVar = myVar * 2;

print (myOtherVar);");

            Assert.AreEqual("10null20", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Global_Var_DoubleRun()
        {
            
            var script = @"var myVar = 10; 
var myNull; 
print (myVar); 
print (myNull);

var myOtherVar = myVar * 2;

print (myOtherVar);";

            engine.Run(script);
            engine.Run(script);

            Assert.AreEqual("10null2010null20", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Blocks_Constants()
        {
            

            engine.Run(@"{print (1+2);}");

            Assert.AreEqual("3", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Blocks_Globals()
        {
            

            engine.Run(@"
var a = 2; 
var b = 1;
{
    print( a+b);
}");

            Assert.AreEqual("3", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Blocks_Locals()
        {
            

            engine.Run(@"
{
    var a = 2; 
    var b = 1;
    print (a+b);
    {
        var c = 3;
        print (a+b+c);
    }
}");

            Assert.AreEqual("36", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Blocks_PopCheck()
        {
            

            engine.Run(@"
{
    var a = 2; 
    var b = 1;
    print (a+b);
}");

            Assert.AreEqual("3", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Blocks_Locals_Sets()
        {
            

            engine.Run(@"
{
    var a = 2; 
    var b = 1;
    var ans = a+b;
    print (ans);
    {
        var c = 3;
        ans = a+b+c;
        print (ans);
    }
}");

            Assert.AreEqual("36", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_If_Jump_Constants()
        {
            

            engine.Run(@"if(1 > 2) print (""ERROR""); print (""End"");");

            Assert.AreEqual("End", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_If_Else_Constants()
        {
            

            engine.Run(@"
if(1 > 2) 
    print (""ERROR""); 
else 
    print (""The ""); 
print (""End"");");

            Assert.AreEqual("The End", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_If_Else_Logic_Constants()
        {
            

            engine.Run(@"
if(1 > 2 or 2 > 3) 
    print( ""ERROR""); 
else if (1 == 1 and 2 == 2)
    print (""The ""); 
print (""End"");");

            Assert.AreEqual("The End", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_While()
        {
            

            engine.Run(@"
var i = 0;
while(i < 2)
{
    print (""hip, "");
    i = i + 1;
}

print (""hurray"");");


            Assert.AreEqual("hip, hip, hurray", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_While_Break()
        {
            

            engine.Run(@"
var i = 0;
while(i < 5)
{
    print (""success?"");
    break;
    print (""FAIL"");
    print(""FAIL"");
    print(""FAIL"");
    print(""FAIL"");
    i = i + 1;
}

print (""hurray"");");


            Assert.AreEqual("success?hurray", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_While_Continue()
        {
            

            engine.Run(@"
var i = 0;
while(i < 3)
{
    i = i + 1;
    print (i);
    continue;
    print (""FAIL"");
}");


            Assert.AreEqual("123", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_For_Continue()
        {


            engine.Run(@"
for(var i = 0;i < 3; i += 1)
{
    print (i);
    continue;
    print (""FAIL"");
}");


            Assert.AreEqual("012", engine.InterpreterResult);
        }


        [Test]
        public void Engine_Cycle_While_Nested()
        {
            

            engine.Run(@"
var i = 0;
var j = 0;
while(i < 5)
{
    j= 0;
    while(j < 5)
    {
        j = j + 1;
        print (j);
        print (i);
    }    
    i = i + 1;
    print (i);
}");

            Assert.AreEqual("1020304050111213141512122232425231323334353414243444545", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_While_Nested_LocalInner()
        {
            

            engine.Run(@"
var i = 0;
while(i < 5)
{
    var j = 0;
    while(j < 5)
    {
        j = j + 1;
        print (j);
        print (i);
    }    
    i = i + 1;
    print (i);
}");

            Assert.AreEqual("1020304050111213141512122232425231323334353414243444545", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_While_Nested_LocalsAndGlobals()
        {
            

            engine.Run(@"
var i = 0;
fun DoIt(){
while(i < 5)
{
    var j = 0;
    while(j < 5)
    {
        j = j + 1;
        print (j);
        print (i);
    }    
    i = i + 1;
    print (i);
}}

DoIt();");


            Assert.AreEqual("1020304050111213141512122232425231323334353414243444545", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_While_Nested_Locals()
        {
            

            engine.Run(@"
fun DoIt(){
var i = 0;
var j = 0;
while(i < 5)
{
    j= 0;
    while(j < 5)
    {
        j = j + 1;
        print (j);
        print (i);
    }    
    i = i + 1;
    print (i);
}}

DoIt();");


            Assert.AreEqual("1020304050111213141512122232425231323334353414243444545", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_For()
        {
            

            engine.Run(@"
for(var i = 0; i < 2; i = i + 1)
{
    print (""hip, "");
}

print (""hurray"");");


            Assert.AreEqual("hip, hip, hurray", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_Func_Do_Nothing()
        {
            

            engine.Run(@"
fun T()
{
    var a = 2;
}

print (T);");

            Assert.AreEqual( "<closure T upvals:0>", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_Func_Call()
        {
            

            engine.Run(@"
fun MyFunc()
{
    print (2);
}

MyFunc();");

            Assert.AreEqual("2", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_NativeFunc_Call()
        {
            
            engine.VM.SetGlobal("CallEmptyNative", Value.New((vm, stack) => Value.New("Native")));

            engine.Run(@"print (CallEmptyNative());");

            Assert.AreEqual("Native", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_Call_Mixed_Ops()
        {
            

            engine.Run(@"
fun A(){return 2;}
fun B(){return 3;}
fun C(){return 10;}

print (A()+B()*C());");

            Assert.AreEqual("32", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_Func_Inner_Logic()
        {
            

            engine.Run(@"
fun A(v)
{
    if(v > 5)
        return 2;
    return -1;
}
fun B(){return 3;}
fun C(){return 10;}

print( A(1)+B()*C());

print (A(10)+B()*C());");

            Assert.AreEqual("2932", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_Var_Mixed_Ops()
        {
            

            engine.Run(@"
var a = 2;
var b = 3;
var c = 10;

print (a+b*c);");

            Assert.AreEqual("32", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_Var_Mixed_Ops_InFunc()
        {
            

            engine.Run(@"
fun Func(){
var a = 2;
var b = 3;
var c = 10;

print (a+b*c);
}

Func();");

            Assert.AreEqual("32", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_Recursive()
        {
            engine.Run(@"fun Recur(a)
{
    if(a > 0) 
    {
        print (a);
        Recur(a-1);
    }
}

Recur(5);");

            Assert.AreEqual("54321", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Closure_Inner_Outer()
        {
            

            engine.Run(@"
var x = ""global"";
var A = ""ERROR"";
fun outer() {
    var y = ""ERROR"";
    var x = ""outer"";
    var z = ""ERROR"";
    fun inner()
    {
        print (x);
    }
    inner();
}
outer(); ");

            Assert.AreEqual("outer", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Closure_Tripup()
        {
            

            engine.Run(@"
fun outer() {
  var x = ""value"";
  fun middle() {
    fun inner() {
      print (x);
    }

    print (""create inner closure"");
    return inner;
  }

  print (""return from outer"");
  return middle;
}

var mid = outer();
var in = mid();
in();");

            Assert.AreEqual(@"return from outercreate inner closurevalue", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Closure_StillOnStack()
        {
            

            engine.Run(@"
fun outer() {
  var x = ""outside"";
  fun inner() {
        print (x);
    }
    inner();
}
outer();");

            Assert.AreEqual("outside", engine.InterpreterResult);
        }

        [Test]
        public void Engine_NestedFunc_ExampleDissasembly()
        {
            

            engine.Run(@"
fun outer() {
  fun middle() {
    fun inner() {
    }
  }
}");
        }

        [Test]
        public void Engine_Closure_ExampleDissasembly()
        {
            

            engine.Run(@"
fun outer() {
  var a = 1;
  var b = 2;
  fun middle() {
    var c = 3;
    var d = 4;
    fun inner() {
      print (a + c + b + d);
    }
  }
}");
        }

        [Test]
        public void Engine_Closure_Counter()
        {
            

            engine.Run(@"
fun makeCounter() {
    var a = ""A"";
    print (a);
  var i = 0;
    print (i);
  fun count() {
    i = i + 1;
    print (i);
  }

  return count;
}

var c1 = makeCounter();

c1();
c1();

var c2 = makeCounter();
c2();
c2();");

            Assert.AreEqual("A012A012", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Closure_Counter_NoPrint()
        {


            engine.Run(@"
fun makeCounter() 
{
    var i = 0;
    fun count() 
    {
        i = i + 1;
        return i;
    }

    return count;
}

var c1 = makeCounter();

c1();
var res = c1();

print(res);
");

            Assert.AreEqual("2", engine.InterpreterResult);
        }
        [Test]
        public void Engine_Class_Empty()
        {
            

            engine.Run(@"
class Brioche {}
print (Brioche);");

            Assert.AreEqual("<class Brioche>", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Instance_Empty()
        {
            

            engine.Run(@"
class Brioche {}
print (Brioche());");

            Assert.AreEqual("<inst Brioche>", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Instance_Method()
        {
            

            engine.Run(@"
class Brioche 
{
    Meth(){print (""Method Called"");}
}
Brioche().Meth();");

            Assert.AreEqual("Method Called", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Instance_Method_This()
        {
            

            engine.Run(@"
class Brioche 
{
    Meth(){return this;}
}

print (Brioche().Meth());");

            Assert.AreEqual("<inst Brioche>", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Instance_Simple0()
        {
            

            engine.Run(@"
class Toast {}
Toast().a = 3;");

            Assert.AreEqual("", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Instance_Simple1()
        {
            

            engine.Run(@"
class Toast {}
var toast = Toast();
print (toast.jam = ""grape"");");

            Assert.AreEqual("grape", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Instance_Simple2()
        {
            

            engine.Run(@"
class Pair {}

var pair = Pair();
pair.first = 1;
pair.second = 2;
print( pair.first + pair.second);");

            Assert.AreEqual("3", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Method_Simple1()
        {
            

            engine.Run(@"
class T 
{
    Say(){print (7);}
}

var t = T();
t.Say();");

            Assert.AreEqual("7", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Method_Simple2()
        {
            

            engine.Run(@"
class T 
{
    Say(){print (this.name);}
}

var t = T();
t.name = ""name"";
t.Say();");

            Assert.AreEqual("name", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Set_Existing_From_Const()
        {
            

            engine.Run(@"
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

            Assert.AreEqual("7", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Set_Existing_From_Arg()
        {
            

            engine.Run(@"
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

            Assert.AreEqual("7", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Set_New_From_Const()
        {
            

            engine.Run(@"
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

            Assert.AreEqual("7", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Set_New_From_Arg()
        {
            

            engine.Run(@"
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

            Assert.AreEqual("7", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Manual_Init_Simple1()
        {
            

            engine.Run(@"
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

            Assert.AreEqual("Enjoy your cup of coffee and chicory", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Init_Simple1()
        {
            

            engine.Run(@"
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

            Assert.AreEqual("Enjoy your cup of coffee and chicory", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_BoundMethod()
        {
            

            engine.Run(@"
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

            Assert.AreEqual("Enjoy your cup of coffee and chicory", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Field_As_Method()
        {
            

            engine.Run(@"
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

            Assert.AreEqual("Enjoy your cup of coffee", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Inher_Simple1()
        {
            

            engine.Run(@"
class A{MethA(){print (1);}}
class B < A {MethB(){print (2);}}

var b = B();
b.MethA();
b.MethB();");

            Assert.AreEqual("12", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Inher_Simple2()
        {
            

            engine.Run(@"
class A{MethA(){print (1);}}
class B < A {MethB(){this.MethA();print (2);}}

var b = B();
b.MethB();");

            Assert.AreEqual("12", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Inher_Poly()
        {
            

            engine.Run(@"
class A{MethA(){print (1);}}
class B < A {MethA(){print (2);}}

var b = B();
b.MethA();");

            Assert.AreEqual("2", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Inher_Super()
        {
            

            engine.Run(@"
class A{MethA(){print (1);}}
class B < A {MethA(){super.MethA(); print (2);}}

var b = B();
b.MethA();");

            Assert.AreEqual("12", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Inher_Super_BoundReturn()
        {
            

            engine.Run(@"
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

            Assert.AreEqual("21", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Multiple_Scripts1()
        {
            

            engine.Run(@"var a = 10;");
            engine.Run(@"print (a);");

            Assert.AreEqual("10", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Multiple_Scripts2()
        {
            

            engine.Run(@"
class A{MethA(){print (1);}}");

            engine.Run(@"var a = A();");
            engine.Run(@"a.MethA();");

            Assert.AreEqual("1", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Read_External_Global()
        {
            

            engine.VM.SetGlobal("a", Value.New(1));

            engine.Run(@"print (a);");

            Assert.AreEqual("1", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Call_External_NoParams()
        {
            

            engine.Run(@"fun Meth(){print (1);}");

            var meth = engine.VM.GetGlobal("Meth");
            engine.VM.CallFunction(meth,0);

            Assert.AreEqual("1", engine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeFunc_Call_0Param_String()
        {
            

            Value Func(VMBase vm, int args)
            {
                return Value.New("Hello from native.");
            }

            engine.VM.SetGlobal("Meth", Value.New(Func));

            engine.Run(@"print (Meth());");

            Assert.AreEqual("Hello from native.", engine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeFunc_Call_1Param_String()
        {
            

            Value Func(VMBase vm, int args)
            {
                return Value.New($"Hello, {vm.GetArg(1).val.asString}, I'm native.");
            }

            engine.VM.SetGlobal("Meth", Value.New(Func));

            engine.Run(@"print (Meth(""Dad""));");

            Assert.AreEqual("Hello, Dad, I'm native.", engine.InterpreterResult);
        }

        [Test]
        public void Engine_NestedCalls()
        {
            

            engine.Run(@"
fun A(){return 7;}
fun B(v){return 1+v;}

var res = B(A());
print (res);
");

            Assert.AreEqual("8", engine.InterpreterResult);
        }

        [Test]
        public void Engine_List()
        {
            

            engine.AddLibrary(new StandardClassesLibrary());

            engine.Run(@"
var list = List();

for(var i = 0; i < 5; i += 1)
    list.Add(i);

var c = list.Count();
print(c);

for(var i = 0; i < c; i += 1)
    print(list.Get(i));

for(var i = 0; i < c; i +=1)
    print(list.Set(i, -i));
");

            Assert.AreEqual("5012340-1-2-3-4", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Throw()
        {
            

            Assert.Throws<PanicException>(() => engine.Run(@"throw;"),"Null");
        }

        [Test]
        public void Engine_Throw_Exp()
        {
            

            Assert.Throws<PanicException>(() => engine.Run(@"throw 2+3;"), "5");
        }

        [Test]
        public void Engine_Class_Init()
        {


            engine.Run(@"
class T
{
    init(){}
}

var t = T();
t.a = null;
print(t.a);");

            Assert.AreEqual("null", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_InitReplacesDefaults()
        {


            engine.Run(@"
class T
{
    var a= 10, b = 20;
    init(a,b){}
}

var t = T(1,2);
print(t.a);
print(t.b);");

            Assert.AreEqual("12", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Var()
        {
            

            engine.Run(@"
class T
{
    var a;
}

var t = T();
print(t.a);");

            Assert.AreEqual("null", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_2Var()
        {
            

            engine.Run(@"
class T
{
    var a;
    var b;
}

var t = T();
print(t.a);
print(t.b);");

            Assert.AreEqual("nullnull", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_MultiVar()
        {
            

            engine.Run(@"
class T
{
    var a,b;
}

var t = T();
print(t.a);
print(t.b);");

            Assert.AreEqual("nullnull", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_VarAfterInit_Throws()
        {
            engine.Run(@"
class T
{
    init(){}
    var a,b;
}

var t = T();
print(t.a);
print(t.b);");

            Assert.AreEqual("Encountered unexpected var declaration in class T. Class vars must come before init or methods.", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_MethodBeforeInit_Throws()
        {
            engine.Run(@"
class T
{
    Meth(){}
    init(){}
}

var t = T();");

            Assert.AreEqual("Encountered init in class at Method, in class T. This is not allowed.", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Local_MultiVar()
        {
            

            engine.Run(@"
var a = 1,b = 2, c;

print(a);
print(b);
print(c);");

            Assert.AreEqual("12null", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Assert()
        {
            

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
Assert.AreEqual(1,1);
Assert.AreEqual(1,2);
Assert.AreEqual(1,1);");

            Assert.AreEqual("'1' does not equal '2'.", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Approx_Assert()
        {
            

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
Assert.AreApproxEqual(1,1);
Assert.AreApproxEqual(1,1.000000001);
Assert.AreApproxEqual(1,2);");

            Assert.AreEqual("'1' and '2' are '-1' apart.", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Loop()
        {
            

            engine.Run(@"
var i = 0;
loop
{
    print (i);
    i = i + 1;
    if(i > 5)
        break;
    print (i);
}");

            Assert.AreEqual("01122334455", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Loop_NoTerminate()
        {
            

            engine.Run(@"
var i = 0;
loop
{
    print (i);
    i = i + 1;
}");

            Assert.AreEqual("Loops must contain an termination.", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_VarInitChain()
        {
            engine.Run(@"
var aVal = 10;
class T
{
    var a = aVal;
}

var t = T();
print(t.a);");

            Assert.AreEqual("10", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_VarInitChain2()
        {
            

            engine.Run(@"
class T
{
    var a = 1, b = 2;
}

var t = T();
print(t.a);
print(t.b);");

            Assert.AreEqual("12", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_VarInitChain_AndInit()
        {
            

            engine.Run(@"
var aVal = 10;
class T
{
    var a = aVal;

    init(){this.a = this.a * 2;}
}

var t = T();
print(t.a);");

            Assert.AreEqual("20", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_VarInitChainEmpty_AndInit()
        {
            engine.Run(@"
class T
{
    var a;

    init(){this.a = 20;}
}

var t = T();
print(t.a);");

            Assert.AreEqual("20", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_VarInitChain_AndInitPrint()
        {


            engine.Run(@"
class T
{
    var a = 20;

    init(){print(this.a);}
}

var t = T();");

            Assert.AreEqual("20", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_VarWithInit()
        {


            engine.Run(@"
class T
{
    var a;

    init(_a) { this.a =_a; }
}

var t = T(1);
print(t.a);");

            Assert.AreEqual("1", engine.InterpreterResult);
        }

        [Test]
        public void Engine_DynamicType_SameFunc()
        {
            

            engine.Run(@"
fun AddPrint(obj)
{
    print(obj.a + obj.b);
}

class T1 
{
    var z,a=1,b=2;
}
class T2 
{
    var x,y,z,a=""Hello "",b=""World"";
}
class T3
{
}

var t1 = T1();
var t2 = T2();
var t3 = T3();
t3.a = 1;
t3.b = 1;

AddPrint(t1);
AddPrint(t2);
AddPrint(t3);
");

            Assert.AreEqual("3Hello World2", engine.InterpreterResult);
        }

        [Test]
        public void Engine_DynamicType_SameInvoke()
        {
            

            engine.Run(@"
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

            Assert.AreEqual("33", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Func_Paramless()
        {
            

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
fun T
{
    return 7;
}

print(T());");

            Assert.AreEqual("7", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Method_Paramless()
        {
            

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
class T
{
    Meth
    {
        return 7;
    }
}

print(T().Meth());");

            Assert.AreEqual("7", engine.InterpreterResult);
        }

        [Test]
        public void Engine_NoThis_Method_WorksAsStatic()
        {
            

            engine.Run(@"
class T
{
    NoMemberMethod()
    {
        return 7;
    }
}

print(T.NoMemberMethod());");

            Assert.AreEqual("7", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Static_Method_OnClass()
        {
            

            engine.Run(@"
class T
{
    static StaticMethod()
    {
        return 7;
    }
}

print(T.StaticMethod());");

            Assert.AreEqual("7", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Static_Method_OnInstance()
        {
            

            engine.Run(@"
class T
{
    static StaticMethod()
    {
        return 7;
    }
}

print(T().StaticMethod());");

            Assert.AreEqual("7", engine.InterpreterResult);
        }

        [Test]
        public void Engine_TestCase_Simple1()
        {
            

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
test T
{
    testcase A
    {
        print(2);
    }
}");

            Assert.AreEqual("2", engine.InterpreterResult);
        }

        [Test]
        public void Engine_TestCase_Simple2()
        {
            

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
test T
{
    testcase A
    {
        Assert.AreEqual(2,2);
    }
}");

            Assert.AreEqual("", engine.InterpreterResult);
        }

        [Test]
        public void Engine_TestCase_Simple3()
        {
            

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
test T
{
    testcase A
    {
        var a = 2;
        var b = 3;
        Assert.AreNotEqual(a,b);
    }
}");

            Assert.AreEqual("", engine.InterpreterResult);
        }

        [Test]
        public void Engine_TestCase_Simple4()
        {
            

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
test T
{
    testcase A
    {
        var a = 2;
        var b = 3;
        var c = a + b;
        Assert.AreEqual(c,5);
    }
}");

            Assert.AreEqual("", engine.InterpreterResult);
            Assert.AreEqual("T:A Completed", engine.VM.TestRunner.GenerateDump());
        }

        [Test]
        public void Engine_TestCase_SimpleInClass()
        {


            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
class Klass
{
    testcase A
    {
        var a = 2;
        var b = 3;
        var c = a + b;
        Assert.AreEqual(c,5);
    }
}");

            Assert.AreEqual("", engine.InterpreterResult);
            Assert.AreEqual("Klass:A Completed", engine.VM.TestRunner.GenerateDump());
        }

        [Test]
        public void Engine_TestCase_Simple4_Skipped()
        {
            

            engine.AddLibrary(new AssertLibrary());
            engine.VM.TestRunner.Enabled = false;

            engine.Run(@"
test T
{
    testcase A
    {
        var a = 2;
        var b = 3;
        var c = a + b;
        Assert.AreEqual(c,5);
    }
}

print(T);");

            Assert.AreEqual("null", engine.InterpreterResult);
            Assert.AreEqual("", engine.VM.TestRunner.GenerateDump());
        }

        [Test]
        public void Engine_TestCase_Intertwinned()
        {
            

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
test T
{
    var a = 2;
    testcase A
    {
        var testInst = T();
        Assert.AreEqual(7,testInst.Combine());
    }
    static var d = 3;
    var b = 2;

    Combine(){return this.a + this.b + T.d;}
}");

            Assert.AreEqual("", engine.InterpreterResult);
        }

        [Test]
        public void Engine_TestCase_TestIsNullAfterRun()
        {
            

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
test T
{
    testcase A
    {
    }
}

print(T);");

            Assert.AreEqual("null", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_Fields()
        {
            

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
class T{ }

T.a = 2;

print(T.a);");

            Assert.AreEqual("2", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_StaticFields()
        {
            

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
class T
{
    static var a = 2;
}
print(T.a);");

            Assert.AreEqual("2", engine.InterpreterResult);
        }

        [Test]
        public void Engine_SelfAssign()
        {
            

            engine.Run(@"
var a = 2;
a = a + 2;
print(a);
a += 2;
print(a);");

            Assert.AreEqual("46", engine.InterpreterResult);
        }

        [Test]
        public void Engine_SingleProgram_MultiVMs()
        {
            

            var script = @"print(2);";

            engine.Run(script);

            var program = engine.Program;


            var engine2 = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);
            engine2.Execute(program);

            Assert.AreEqual("2", engine.InterpreterResult);
            Assert.AreEqual(engine2.InterpreterResult, "2", engine2.InterpreterResult);
        }

        [Test]
        public void Engine_InternalSandbox_CanPassIn()
        {
            

            engine.AddLibrary(new VMLibrary());

            engine.Run(@"
fun InnerMain()
{
    print(globalIn);
}

var globalIn = 10;

var innerVM = VM();
innerVM.AddGlobal(""globalIn"",globalIn);
innerVM.AddGlobal(""print"",print);

innerVM.Start(InnerMain);");

            Assert.AreEqual("10", engine.InterpreterResult);
        }

        [Test]
        public void Engine_InternalSandbox_CanWriteGlobalOut()
        {
            

            engine.AddLibrary(new VMLibrary());

            engine.Run(@"
fun InnerMain()
{
    globalOut = 10;
}

var globalOut = 0;

var innerVM = VM();
innerVM.AddGlobal(""globalOut"",globalOut);

innerVM.Start(InnerMain);

print(innerVM.GetGlobal(""globalOut""));");

            Assert.AreEqual("10", engine.InterpreterResult);
        }

        [Test]
        public void Engine_InternalSandbox_CanReturnOut()
        {
            

            engine.AddLibrary(new VMLibrary());

            engine.Run(@"
fun InnerMain()
{
    return 10;
}

var innerVM = VM();

var res = innerVM.Start(InnerMain);

print(res);");

            Assert.AreEqual("10", engine.InterpreterResult);
        }

        [Test]
        public void Engine_InternalSandbox_YieldResume()
        {
            

            engine.Run(@"
var a = 2;
a = a + 2;
yield;
print(a);");

            engine.VM.Run();

            Assert.AreEqual("4", engine.InterpreterResult);
        }

        [Test]
        public void Engine_ChildVM_Run()
        {
            

            engine.AddLibrary(new VMLibrary());

            engine.Run(@"
fun InnerMain()
{
    print(""Hello from inner "" + a);
}

var a = ""10"";

var innerVM = VM();
innerVM.InheritFromEnclosing();
innerVM.Start(InnerMain);");

            Assert.AreEqual("Hello from inner 10", engine.InterpreterResult);
        }

        [Test]
        public void Engine_Sandbox_CannotAccess()
        {
            

            engine.AddLibrary(new VMLibrary());

            engine.Run(@"
var a = 10;
fun InnerMain()
{
    a = 10;
}

var innerVM = VM();
innerVM.Start(InnerMain);");

            Assert.AreEqual("Global var of name 'a' was not found.", engine.InterpreterResult, engine.InterpreterResult);
        }

        [Test]
        public void Engine_Sandbox_AsGenerator()
        {
            

            engine.AddLibrary(new VMLibrary());

            engine.Run(@"
fun InnerMain()
{
    globalOut = 1;
    yield;
    globalOut = 1;
    yield;
    globalOut = 2;
    yield;
    globalOut = 3;
    yield;
    globalOut = 5;
    yield;
    globalOut = 8;
    yield;
    globalOut = null;
}

var globalOut = 0;

var innerVM = VM();
innerVM.AddGlobal(""globalOut"",globalOut);

innerVM.Start(InnerMain);
loop
{
    var curVal = innerVM.GetGlobal(""globalOut"");
    if(curVal != null)
    {
        print(curVal);
        innerVM.Resume();
    }
    else
    {
        break;
    }
}");

            Assert.AreEqual("112358", engine.InterpreterResult);
        }

        //todo yield should be able to multi return, use a yield stack in the vm and clear it at each use?
    }

    public class ByteCodeInterpreterTestEngine : ByteCodeInterpreterEngine
    {
        private System.Action<string> _logger;

        public ByteCodeInterpreterTestEngine(System.Action<string> logger)
        {
            _logger = logger;

            Value Print(VMBase vm, int args)
            {
                var str = vm.GetArg(1).ToString();
                _logger(str);
                AppendResult(str);
                return Value.Null();
            }

            VM.SetGlobal("print", Value.New(Print));
        }
        protected void AppendResult(string str) => InterpreterResult += str;
        public string InterpreterResult { get; private set; } = string.Empty;

        public override void Run(string testString)
        {
            try
            {
                base.Run(testString);
            }
            catch (LoxException e)
            {
                AppendResult(e.Message);
            }
            finally
            {
                _logger(VM.TestRunner.GenerateDump());
                _logger(InterpreterResult);
                _logger(Disassembly);
                _logger(VM.GenerateGlobalsDump());
            }
        }
    }

}