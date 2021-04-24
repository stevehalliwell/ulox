using NUnit.Framework;
using UnityEngine;

namespace ULox.Tests
{
    public class ByteCodeLoxEngineTests
    {
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

            Assert.AreEqual(vm.Interpret(chunk), InterpreterResult.OK);
        }

        [Test]
        public void Engine_Cycle_Math_Expression()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run("print (1+2);");

            Assert.AreEqual(engine.InterpreterResult, "3");
        }

        [Test]
        public void Engine_Cycle_Logic_Not_Expression()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run("print (!true);");

            Assert.AreEqual(engine.InterpreterResult, "False");
        }

        [Test]
        public void Engine_Cycle_Logic_Compare_Expression()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run("print (1 < 2 == false);");

            Assert.AreEqual(engine.InterpreterResult, "False");
        }

        [Test]
        public void Engine_Cycle_String_Add_Expression()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run("print (\"hello\" + \" \" + \"world\");");

            Assert.AreEqual(engine.InterpreterResult, "hello world");
        }

        [Test]
        public void Engine_Cycle_Print_Math_Expression()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run("print (1 + 2 * 3);");

            Assert.AreEqual(engine.InterpreterResult, "7");
        }

        [Test]
        public void Engine_Cycle_Global_Var()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"var myVar = 10; 
var myNull; 
print (myVar); 
print (myNull);

var myOtherVar = myVar * 2;

print (myOtherVar);");

            Assert.AreEqual(engine.InterpreterResult, "10null20");
        }

        [Test]
        public void Engine_Cycle_Blocks_Constants()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"{print (1+2);}");

            Assert.AreEqual(engine.InterpreterResult, "3");
        }

        [Test]
        public void Engine_Cycle_Blocks_Globals()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
var a = 2; 
var b = 1;
{
    print( a+b);
}");

            Assert.AreEqual(engine.InterpreterResult, "3");
        }

        [Test]
        public void Engine_Cycle_Blocks_Locals()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "36");
        }

        [Test]
        public void Engine_Cycle_Blocks_PopCheck()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
{
    var a = 2; 
    var b = 1;
    print (a+b);
}");

            Assert.AreEqual(engine.InterpreterResult, "3");
        }

        [Test]
        public void Engine_Cycle_Blocks_Locals_Sets()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "36");
        }

        [Test]
        public void Engine_Cycle_If_Jump_Constants()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"if(1 > 2) print (""ERROR""); print (""End"");");

            Assert.AreEqual(engine.InterpreterResult, "End");
        }

        [Test]
        public void Engine_Cycle_If_Else_Constants()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
if(1 > 2) 
    print (""ERROR""); 
else 
    print (""The ""); 
print (""End"");");

            Assert.AreEqual(engine.InterpreterResult, "The End");
        }

        [Test]
        public void Engine_Cycle_If_Else_Logic_Constants()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
if(1 > 2 or 2 > 3) 
    print( ""ERROR""); 
else if (1 == 1 and 2 == 2)
    print (""The ""); 
print (""End"");");

            Assert.AreEqual(engine.InterpreterResult, "The End");
        }

        [Test]
        public void Engine_Cycle_While()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
var i = 0;
while(i < 2)
{
    print (""hip, "");
    i = i + 1;
}

print (""hurray"");");


            Assert.AreEqual(engine.InterpreterResult, "hip, hip, hurray");
        }

        [Test]
        public void Engine_Cycle_While_Break()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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


            Assert.AreEqual(engine.InterpreterResult, "success?hurray");
        }

        [Test]
        public void Engine_Cycle_While_Continue()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
var i = 0;
while(i < 3)
{
    i = i + 1;
    print (i);
    continue;
    print (""FAIL"");
}");


            Assert.AreEqual(engine.InterpreterResult, "123");
        }

        [Test]
        public void Engine_Cycle_While_Nested()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "1020304050111213141512122232425231323334353414243444545");
        }

        [Test]
        public void Engine_Cycle_While_Nested_LocalInner()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "1020304050111213141512122232425231323334353414243444545");
        }

        [Test]
        public void Engine_Cycle_While_Nested_LocalsAndGlobals()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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


            Assert.AreEqual(engine.InterpreterResult, "1020304050111213141512122232425231323334353414243444545");
        }

        [Test]
        public void Engine_Cycle_While_Nested_Locals()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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


            Assert.AreEqual(engine.InterpreterResult, "1020304050111213141512122232425231323334353414243444545");
        }

        [Test]
        public void Engine_Cycle_For()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
for(var i = 0; i < 2; i = i + 1)
{
    print (""hip, "");
}

print (""hurray"");");


            Assert.AreEqual(engine.InterpreterResult, "hip, hip, hurray");
        }

        [Test]
        public void Engine_Compile_Func_Do_Nothing()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
fun T()
{
    var a = 2;
}

print (T);");

            Assert.AreEqual(engine.InterpreterResult, "<closure T upvals:0>");
        }

        [Test]
        public void Engine_Compile_Func_Call()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
fun MyFunc()
{
    print (2);
}

MyFunc();");

            Assert.AreEqual(engine.InterpreterResult, "2");
        }

        [Test]
        public void Engine_Compile_NativeFunc_Call()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);
            engine.VM.SetGlobal("CallEmptyNative", Value.New((vm, stack) => Value.New("Native")));

            engine.Run(@"print (CallEmptyNative());");

            Assert.AreEqual(engine.InterpreterResult, "Native");
        }

        [Test]
        public void Engine_Compile_Call_Mixed_Ops()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
fun A(){return 2;}
fun B(){return 3;}
fun C(){return 10;}

print (A()+B()*C());");

            Assert.AreEqual(engine.InterpreterResult, "32");
        }

        [Test]
        public void Engine_Compile_Func_Inner_Logic()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "2932");
        }

        [Test]
        public void Engine_Compile_Var_Mixed_Ops()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
var a = 2;
var b = 3;
var c = 10;

print (a+b*c);");

            Assert.AreEqual(engine.InterpreterResult, "32");
        }

        [Test]
        public void Engine_Compile_Var_Mixed_Ops_InFunc()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
fun Func(){
var a = 2;
var b = 3;
var c = 10;

print (a+b*c);
}

Func();");

            Assert.AreEqual(engine.InterpreterResult, "32");
        }

        //[Test]
        //public void Engine_Compile_NativeFunc_Args_Call()
        //{
        //    var engine = new ByteCodeLoxEngine();
        //    engine.VM.DefineNativeFunction("CallNative", (vm, stack) =>
        //    {
        //        var lhs = vm.
        //        return Value.New("Native");
        //    });

        //    engine.Run(@"print CalEmptylNative();");

        //    Assert.AreEqual(engine.InterpreterResult, "Native");
        //}

        [Test]
        [Ignore("long running manual test only")]
        public void Engine_Compile_Clocked_Fib()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);
            engine.VM.SetGlobal("clock", Value.New((vm, stack) =>
            {
                return Value.New(System.DateTime.Now.Ticks);
            }));

            engine.Run(@"
fun fib(n)
{
    if (n < 2) return n;
    return fib(n - 2) + fib(n - 1);
}

var start = clock();
print (fib(20));
print ("" in "");
print (clock() - start);");

            //Assert.AreEqual(engine.InterpreterResult, "Native");
        }

        [Test]
        public void Engine_Compile_Recursive()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"fun Recur(a)
{
    if(a > 0) 
    {
        print (a);
        Recur(a-1);
    }
}

Recur(5);");

            Assert.AreEqual(engine.InterpreterResult, "54321");
        }

        [Test]
        public void Engine_Closure_Inner_Outer()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "outer");
        }

        [Test]
        public void Engine_Closure_Tripup()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, @"return from outercreate inner closurevalue");
        }

        [Test]
        public void Engine_Closure_StillOnStack()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
fun outer() {
  var x = ""outside"";
  fun inner() {
        print (x);
    }
    inner();
}
outer();");

            Assert.AreEqual(engine.InterpreterResult, "outside");
        }

        [Test]
        public void Engine_NestedFunc_ExampleDissasembly()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "A012A012");
        }

        [Test]
        public void Engine_Class_Empty()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class Brioche {}
print (Brioche);");

            Assert.AreEqual(engine.InterpreterResult, "<class Brioche>");
        }

        [Test]
        public void Engine_Class_Instance_Empty()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class Brioche {}
print (Brioche());");

            Assert.AreEqual(engine.InterpreterResult, "<inst Brioche>");
        }

        [Test]
        public void Engine_Class_Instance_Method()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class Brioche 
{
    Meth(){print (""Method Called"");}
}
Brioche().Meth();");

            Assert.AreEqual(engine.InterpreterResult, "Method Called");
        }

        [Test]
        public void Engine_Class_Instance_Method_This()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class Brioche 
{
    Meth(){return this;}
}

print (Brioche().Meth());");

            Assert.AreEqual(engine.InterpreterResult, "<inst Brioche>");
        }

        [Test]
        public void Engine_Class_Instance_Simple0()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class Toast {}
Toast().a = 3;");

            Assert.AreEqual(engine.InterpreterResult, "");
        }

        [Test]
        public void Engine_Class_Instance_Simple1()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class Toast {}
var toast = Toast();
print (toast.jam = ""grape"");");

            Assert.AreEqual(engine.InterpreterResult, "grape");
        }

        [Test]
        public void Engine_Class_Instance_Simple2()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class Pair {}

var pair = Pair();
pair.first = 1;
pair.second = 2;
print( pair.first + pair.second);");

            Assert.AreEqual(engine.InterpreterResult, "3");
        }

        [Test]
        public void Engine_Class_Method_Simple1()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class T 
{
    Say(){print (7);}
}

var t = T();
t.Say();");

            Assert.AreEqual(engine.InterpreterResult, "7");
        }

        [Test]
        public void Engine_Class_Method_Simple2()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class T 
{
    Say(){print (this.name);}
}

var t = T();
t.name = ""name"";
t.Say();");

            Assert.AreEqual(engine.InterpreterResult, "name");
        }

        [Test]
        public void Engine_Class_Set_Existing_From_Const()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "7");
        }

        [Test]
        public void Engine_Class_Set_Existing_From_Arg()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "7");
        }

        [Test]
        public void Engine_Class_Set_New_From_Const()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "7");
        }

        [Test]
        public void Engine_Class_Set_New_From_Arg()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "7");
        }

        [Test]
        public void Engine_Class_Manual_Init_Simple1()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "Enjoy your cup of coffee and chicory");
        }

        [Test]
        public void Engine_Class_Init_Simple1()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "Enjoy your cup of coffee and chicory");
        }

        [Test]
        public void Engine_Class_BoundMethod()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "Enjoy your cup of coffee and chicory");
        }

        [Test]
        public void Engine_Class_Field_As_Method()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "Enjoy your cup of coffee");
        }

        [Test]
        public void Engine_Class_Inher_Simple1()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class A{MethA(){print (1);}}
class B < A {MethB(){print (2);}}

var b = B();
b.MethA();
b.MethB();");

            Assert.AreEqual(engine.InterpreterResult, "12");
        }

        [Test]
        public void Engine_Class_Inher_Simple2()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class A{MethA(){print (1);}}
class B < A {MethB(){this.MethA();print (2);}}

var b = B();
b.MethB();");

            Assert.AreEqual(engine.InterpreterResult, "12");
        }

        [Test]
        public void Engine_Class_Inher_Poly()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class A{MethA(){print (1);}}
class B < A {MethA(){print (2);}}

var b = B();
b.MethA();");

            Assert.AreEqual(engine.InterpreterResult, "2");
        }

        [Test]
        public void Engine_Class_Inher_Super()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class A{MethA(){print (1);}}
class B < A {MethA(){super.MethA(); print (2);}}

var b = B();
b.MethA();");

            Assert.AreEqual(engine.InterpreterResult, "12");
        }

        [Test]
        public void Engine_Class_Inher_Super_BoundReturn()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "21");
        }

        [Test]
        public void Engine_Multiple_Scripts1()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"var a = 10;");
            engine.Run(@"print (a);");

            Assert.AreEqual(engine.InterpreterResult, "10");
        }

        [Test]
        public void Engine_Multiple_Scripts2()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class A{MethA(){print (1);}}");

            engine.Run(@"var a = A();");
            engine.Run(@"a.MethA();");

            Assert.AreEqual(engine.InterpreterResult, "1");
        }

        [Test]
        public void Engine_Read_External_Global()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.VM.SetGlobal("a", Value.New(1));

            engine.Run(@"print (a);");

            Assert.AreEqual(engine.InterpreterResult, "1");
        }

        [Test]
        public void Engine_Call_External_NoParams()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"fun Meth(){print (1);}");

            var meth = engine.VM.GetGlobal("Meth");
            engine.VM.CallFunction(meth,0);

            Assert.AreEqual(engine.InterpreterResult, "1");
        }

        [Test]
        public void Engine_NativeFunc_Call_0Param_String()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            Value Func(VM vm, int args)
            {
                return Value.New("Hello from native.");
            }

            engine.VM.SetGlobal("Meth", Value.New(Func));

            engine.Run(@"print (Meth());");

            Assert.AreEqual(engine.InterpreterResult, "Hello from native.");
        }

        [Test]
        public void Engine_NativeFunc_Call_1Param_String()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            Value Func(VM vm, int args)
            {
                return Value.New($"Hello, {vm.GetArg(1).val.asString}, I'm native.");
            }

            engine.VM.SetGlobal("Meth", Value.New(Func));

            engine.Run(@"print (Meth(""Dad""));");

            Assert.AreEqual(engine.InterpreterResult, "Hello, Dad, I'm native.");
        }

        [Test]
        public void Engine_NestedCalls()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
fun A(){return 7;}
fun B(v){return 1+v;}

var res = B(A());
print (res);
");

            Assert.AreEqual(engine.InterpreterResult, "8");
        }

        [Test]
        public void Engine_List()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.AddLibrary(new StandardClassesLibrary());

            engine.Run(@"
var list = List();

for(var i = 0; i < 5; i = i + 1)
    list.Add(i);

var c = list.Count();
print(c);

for(var i = 0; i < c; i = i + 1)
    print(list.Get(i));
");

            Assert.AreEqual(engine.InterpreterResult, "501234");
        }

        [Test]
        public void Engine_Throw()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            Assert.Throws<PanicException>(() => engine.Run(@"throw;"),"Null");
        }

        [Test]
        public void Engine_Throw_Exp()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            Assert.Throws<PanicException>(() => engine.Run(@"throw 2+3;"), "5");
        }

        [Test]
        public void Engine_Class_Var()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class T
{
    var a;
}

var t = T();
print(t.a);");

            Assert.AreEqual(engine.InterpreterResult, "null");
        }

        [Test]
        public void Engine_Class_2Var()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class T
{
    var a;
    var b;
}

var t = T();
print(t.a);
print(t.b);");

            Assert.AreEqual(engine.InterpreterResult, "nullnull");
        }

        [Test]
        public void Engine_Class_MultiVar()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class T
{
    var a,b;
}

var t = T();
print(t.a);
print(t.b);");

            Assert.AreEqual(engine.InterpreterResult, "nullnull");
        }

        [Test]
        public void Engine_Assert()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
Assert.AreEqual(1,1);
Assert.AreEqual(1,2);
Assert.AreEqual(1,1);");

            Assert.AreEqual(engine.InterpreterResult, "'1' does not equal '2'.");
        }

        [Test]
        public void Engine_Approx_Assert()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
Assert.AreApproxEqual(1,1);
Assert.AreApproxEqual(1,1.000000001);
Assert.AreApproxEqual(1,2);");

            Assert.AreEqual(engine.InterpreterResult, "'1' and '2' are '-1' apart.");
        }

        [Test]
        public void Engine_Loop()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "01122334455");
        }

        [Test]
        public void Engine_Loop_NoTerminate()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
var i = 0;
loop
{
    print (i);
    i = i + 1;
}");

            Assert.AreEqual(engine.InterpreterResult, "Loops must contain an termination.");
        }

        [Test]
        public void Engine_Class_VarInitChain()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
var aVal = 10;
class T
{
    var a = aVal;
}

var t = T();
print(t.a);");

            Assert.AreEqual(engine.InterpreterResult, "10");
        }

        [Test]
        public void Engine_Class_VarInitChain2()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class T
{
    var a = 1, b = 2;
}

var t = T();
print(t.a);
print(t.b);");

            Assert.AreEqual(engine.InterpreterResult, "12");
        }

        [Test]
        public void Engine_Class_VarInitChain_AndInit()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
var aVal = 10;
class T
{
    var a = aVal;

    init(){this.a = this.a * 2;}
}

var t = T();
print(t.a);");

            Assert.AreEqual(engine.InterpreterResult, "20");
        }

        [Test]
        public void Engine_Class_VarInitChainEmpty_AndInit()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class T
{
    var a;

    init(){this.a = 20;}
}

var t = T();
print(t.a);");

            Assert.AreEqual(engine.InterpreterResult, "20");
        }

        [Test]
        public void Engine_DynamicType_SameFunc()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "3Hello World2");
        }

        [Test]
        public void Engine_DynamicType_SameInvoke()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "33");
        }

        [Test]
        public void Engine_Func_Paramless()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
fun T
{
    return 7;
}

print(T());");

            Assert.AreEqual(engine.InterpreterResult, "7");
        }

        [Test]
        public void Engine_Method_Paramless()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "7");
        }

        [Test]
        public void Engine_NoThis_Method_WorksAsStatic()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class T
{
    NoMemberMethod()
    {
        return 7;
    }
}

print(T.NoMemberMethod());");

            Assert.AreEqual(engine.InterpreterResult, "7");
        }

        [Test]
        public void Engine_Static_Method_OnClass()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class T
{
    static StaticMethod()
    {
        return 7;
    }
}

print(T.StaticMethod());");

            Assert.AreEqual(engine.InterpreterResult, "7");
        }

        [Test]
        public void Engine_Static_Method_OnInstance()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
class T
{
    static StaticMethod()
    {
        return 7;
    }
}

print(T().StaticMethod());");

            Assert.AreEqual(engine.InterpreterResult, "7");
        }

        [Test]
        public void Engine_TestCase_Simple1()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
test T
{
    testcase A
    {
        print(2);
    }
}");

            Assert.AreEqual(engine.InterpreterResult, "2");
        }

        [Test]
        public void Engine_TestCase_Simple2()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
test T
{
    testcase A
    {
        Assert.AreEqual(2,2);
    }
}");

            Assert.AreEqual(engine.InterpreterResult, "");
        }

        [Test]
        public void Engine_TestCase_Simple3()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "");
        }

        [Test]
        public void Engine_TestCase_Simple4()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "");
        }

        [Test]
        public void Engine_TestCase_Intertwinned()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

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

            Assert.AreEqual(engine.InterpreterResult, "");
        }

        [Test]
        public void Engine_TestCase_TestIsNullAfterRun()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
test T
{
    testcase A
    {
    }
}

print(T);");

            Assert.AreEqual(engine.InterpreterResult, "null");
        }

        [Test]
        public void Engine_Class_Fields()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
class T{ }

T.a = 2;

print(T.a);");

            Assert.AreEqual(engine.InterpreterResult, "2");
        }

        [Test]
        public void Engine_Class_StaticFields()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
class T
{
    static var a = 2;
}
print(T.a);");

            Assert.AreEqual(engine.InterpreterResult, "2");
        }

        [Test]
        public void Engine_ExternalSandboxing()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.AddLibrary(new AssertLibrary());

            engine.Run(@"
var a = ""Hello"";");

            var env1 = new ULoxScriptEnvironment(engine);
            var env2 = new ULoxScriptEnvironment(engine);

            env1.RunLocal(@"
var a = ""World"";
print(a);");

            env2.RunLocal(@"
print(a);");

            Assert.AreEqual(engine.InterpreterResult, "WorldHello");
        }

        [Test]
        public void Engine_InternalSandbox_CanShadow()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.AddLibrary(new VMLibrary());

            engine.Run(@"
var a = 0;
var a = 10;

print(a);
var env = CreateEnvironment();
SetEnvironment(env);

var a = 20;
print(a);

SetEnvironment(null);
print(a);");

            Assert.AreEqual(engine.InterpreterResult, "102010");
        }

        [Test]
        public void Engine_InternalSandbox_CanRestore()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.AddLibrary(new VMLibrary());

            engine.Run(@"
var outerEnv = CreateEnvironment();
SetEnvironment(outerEnv);
var a = 0;
var a = 10;

print(a);

var innerEnv = CreateEnvironment();
SetEnvironment(innerEnv);

var a = 20;
print(a);

SetEnvironment(outerEnv);
print(a);");

            Assert.AreEqual(engine.InterpreterResult, "102010");
        }

        [Test]
        public void Engine_InternalSandbox_CannotAccess()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.AddLibrary(new VMLibrary());

            engine.Run(@"
var outerEnv = CreateEnvironment();
SetEnvironment(outerEnv);
var a = 10;

print(a);

var innerEnv = CreateEnvironment();
SetEnvironment(innerEnv);

print(a);");

            Assert.AreEqual(engine.InterpreterResult, "10Global var of name 'a' was not found.");
        }

        [Test]
        public void Engine_SelfAssign()
        {
            var engine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);

            engine.Run(@"
var a = 2;
a = a + 2;
print(a);
a += 2;
print(a);");

            Assert.AreEqual(engine.InterpreterResult, "46");
        }
    }

    public class ByteCodeInterpreterTestEngine : ByteCodeInterpreterEngine
    {
        private System.Action<string> _logger;

        public ByteCodeInterpreterTestEngine(System.Action<string> logger)
        {
            _logger = logger;

            Value Print(VM vm, int args)
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

        public new void Run(string testString)
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