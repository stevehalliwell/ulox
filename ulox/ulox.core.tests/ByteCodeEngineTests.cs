using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class ByteCodeEngineTests : EngineTestBase
    {
        public static Chunk GenerateManualChunk()
        {
            var chunk = new Chunk("main", "native", "");

            var at = chunk.AddConstant(Value.New(0.5));
            chunk.WritePacket(new ByteCodePacket(OpCode.PUSH_CONSTANT, at, 0, 0), 1);
            at = chunk.AddConstant(Value.New(1));
            chunk.WritePacket(new ByteCodePacket(OpCode.PUSH_CONSTANT, at, 0, 0), 1);
            chunk.WritePacket(new ByteCodePacket(OpCode.NEGATE), 1);
            chunk.WritePacket(new ByteCodePacket(OpCode.ADD), 1);
            at = chunk.AddConstant(Value.New(2));
            chunk.WritePacket(new ByteCodePacket(OpCode.PUSH_CONSTANT, at, 0, 0), 1);
            chunk.WritePacket(new ByteCodePacket(OpCode.MULTIPLY), 1);
            chunk.WritePacket(new ByteCodePacket(OpCode.RETURN), 2);

            return chunk;
        }

        [Test]
        public void Manual_Chunk_VM()
        {
            var chunk = GenerateManualChunk();
            Vm vm = new Vm();

            Assert.AreEqual(InterpreterResult.OK, vm.Interpret(chunk));
        }

        [Test]
        public void Engine_Cycle_Math_Expression()
        {
            testEngine.Run("print (1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void ByteCodePacket_Size_4()
        {
            Assert.AreEqual(3, Marshal.SizeOf<ByteCodePacket.TestOpDetails>());
            Assert.AreEqual(3, Marshal.SizeOf<ByteCodePacket.ClosureDetails>());
            Assert.AreEqual(3, Marshal.SizeOf<ByteCodePacket.LabelDetails>());
            Assert.AreEqual(4, Marshal.SizeOf<ByteCodePacket>());
        }

        [Test]
        public void Engine_Cycle_Minus_Equals_Expression()
        {
            testEngine.Run(@"
var a = 1;
a -= 2;
print (a);");

            Assert.AreEqual("-1", testEngine.InterpreterResult);
        }

        [Test]
        public void UnclosedBlockComment_WhenCompiled_ShouldCompileClean()
        {
            testEngine.Run(@"
/*
var a = 1;
a -= 2;
print (a);");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Logic_Not_Expression()
        {
            testEngine.Run("print (!true);");

            Assert.AreEqual("False", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Logic_Compare_Expression()
        {
            testEngine.Run("print (1 < 2 == false);");

            Assert.AreEqual("False", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_String_Add_Expression()
        {
            testEngine.Run("print (\"hello\" + \" \" + \"world\");");

            Assert.AreEqual("hello world", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_UselessDeclare()
        {
            testEngine.Run(@"
var a = 7;");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_UselessExpression()
        {
            testEngine.Run(@"
var a = 7;
a;");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Print_Math_Expression()
        {
            testEngine.Run("print (1 + 2 * 3);");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Global_Var()
        {
            testEngine.Run(@"var myVar = 10;
var myNull;
print (myVar);
print (myNull);

var myOtherVar = myVar * 2;

print (myOtherVar);");

            Assert.AreEqual("10null20", testEngine.InterpreterResult);
        }

        //fair, redeclaring global should be an error
        [Test]
        public void Engine_Cycle_Global_Var_DoubleRun()
        {
            testEngine.Run(@"var myVar = 10;
var myNull;
print (myVar);
print (myNull);

var myOtherVar = myVar * 2;

print (myOtherVar);");
            testEngine.Run(@"var myVar = 10;
var myNull;
print (myVar);
print (myNull);
var myOtherVar = myVar * 2;
print (myOtherVar);");

            Assert.AreEqual("10null2010null20", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Blocks_Constants()
        {
            testEngine.Run(@"{print (1+2);}");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Blocks_Globals()
        {
            testEngine.Run(@"
var a = 2;
var b = 1;
{
    print( a+b);
}");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Blocks_Locals()
        {
            testEngine.Run(@"
{
    var a = 2;
    var b = 1;
    print (a+b);
    {
        var c = 3;
        print (a+b+c);
    }
}");

            Assert.AreEqual("36", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Blocks_PopCheck()
        {
            testEngine.Run(@"
{
    var a = 2;
    var b = 1;
    print (a+b);
}");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_Blocks_Locals_Sets()
        {
            testEngine.Run(@"
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

            Assert.AreEqual("36", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_Func_Do_Nothing()
        {
            testEngine.Run(@"
fun T()
{
    var a = 2;
}

print (T);");

            Assert.AreEqual("<closure T upvals:0>", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_Func_Call()
        {
            testEngine.Run(@"
fun MyFunc()
{
    print (2);
}

MyFunc();");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_Return()
        {
            testEngine.Run(@"
fun A(){retval = 1;}

print (A());");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }


        [Test]
        public void Engine_Cycle_While_Nested_LocalsAndGlobals()
        {
            testEngine.Run(@"
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

            Assert.AreEqual("1020304050111213141512122232425231323334353414243444545", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Cycle_While_Nested_Locals()
        {
            testEngine.Run(@"
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

            Assert.AreEqual("1020304050111213141512122232425231323334353414243444545", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_Call_Mixed_Ops()
        {
            testEngine.Run(@"
fun A(){retval = 2;}
fun B(){retval = 3;}
fun C(){retval = 10;}

print (A()+B()*C());");

            Assert.AreEqual("32", testEngine.InterpreterResult);
        }

        [Test]
        public void Function_WhenCalledWithDifferentTypesButCompatibleLogic_ShouldSucceed()
        {
            testEngine.Run(@"
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

var t1 = T1();
var t2 = T2();

AddPrint(t1);
AddPrint(t2);
");

            Assert.AreEqual("3Hello World", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_Func_Inner_Logic()
        {
            testEngine.Run(@"
fun A(v)
{
    retval = 2;
    
    if(v > 5)
        return;
    retval = -1;
}
fun B(){retval = 3;}
fun C(){retval = 10;}

print( A(1)+B()*C());

print (A(10)+B()*C());");

            Assert.AreEqual("2932", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_Var_Mixed_Ops()
        {
            testEngine.Run(@"
var a = 2;
var b = 3;
var c = 10;

print (a+b*c);");

            Assert.AreEqual("32", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_Var_Mixed_Ops_InFunc()
        {
            testEngine.Run(@"
fun Func(){
var a = 2;
var b = 3;
var c = 10;

print (a+b*c);
}

Func();");

            Assert.AreEqual("32", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_MinorRecursive()
        {
            testEngine.Run(@"
fun Recur(a)
{
    if(a > 0)
    {
        print (a);
        Recur(a-1);
    }
}

Recur(3);");

            Assert.AreEqual("321", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_Recursive()
        {
            testEngine.Run(@"
fun Recur(a)
{
    if(a > 0)
    {
        print (a);
        Recur(a-1);
    }
}

Recur(5);");

            Assert.AreEqual("54321", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_MajorRecursive()
        {
            testEngine.Run(@"
fun Recur(a)
{
    if(a > 0)
    {
        print (a);
        Recur(a-1);
    }
}

Recur(15);");

            Assert.AreEqual("151413121110987654321", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Closure_Inner_Outer()
        {
            testEngine.Run(@"
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

            Assert.AreEqual("outer", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Closure_Tripup()
        {
            testEngine.Run(@"
fun outer() {
  var x = ""value"";
  fun middle() {
    fun inner() {
      print (x);
    }

    print (""create inner closure"");
    retval = inner;
  }

  print (""retval = from outer"");
  retval = middle;
}

var mid = outer();
var in = mid();
in();");

            Assert.AreEqual(@"retval = from outercreate inner closurevalue", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Closure_StillOnStack()
        {
            testEngine.Run(@"
fun outer() {
  var x = ""outside"";
  fun inner() {
        print (x);
    }
    inner();
}
outer();");

            Assert.AreEqual("outside", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NestedFunc_ExampleDissasembly()
        {
            testEngine.Run(@"
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
            testEngine.Run(@"
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
            testEngine.Run(@"
fun makeCounter() {
    var a = ""A"";
    print (a);
  var i = 0;
    print (i);
  fun count() {
    i = i + 1;
    print (i);
  }

  retval = count;
}

var c1 = makeCounter();

c1();
c1();

var c2 = makeCounter();
c2();
c2();");

            Assert.AreEqual("A012A012", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Closure_Counter_NoPrint()
        {
            //This test actually hits the closure upvalue packet
            testEngine.Run(@"
fun makeCounter()
{
    var i = 0;
    fun count()
    {
        i = i + 1;
        retval = i;
    }

    retval = count;
}

var c1 = makeCounter();

c1();
var res = c1();

print(res);
");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Multiple_Scripts1()
        {
            testEngine.Run(@"var a = 10;");
            testEngine.Run(@"print (a);");

            Assert.AreEqual("10", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Read_External_Global()
        {
            testEngine.MyEngine.Context.Vm.Globals.AddOrSet(new HashedString("a"), Value.New(1));

            testEngine.Run(@"print (a);");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Call_External_NoParams()
        {
            testEngine.Run(@"fun Meth(){print (1);}");

            testEngine.MyEngine.Context.Vm.Globals.Get(new HashedString("Meth"), out var meth);
            testEngine.MyEngine.Context.Vm.PushCallFrameAndRun(meth, 0);

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NestedCalls()
        {
            testEngine.Run(@"
fun A(){retval = 7;}
fun B(v){retval = 1+v;}

var res = B(A());
print (res);
");

            Assert.AreEqual("8", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Throw()
        {
            testEngine.ReThrow = true;
            Assert.Throws<RuntimeUloxException>(() => testEngine.Run(@"throw;"), "Null");
        }

        [Test]
        public void Engine_Throw_Exp()
        {
            testEngine.ReThrow = true;
            Assert.Throws<RuntimeUloxException>(() => testEngine.Run(@"throw 2+3;"), "5");
        }

        [Test]
        public void Engine_Local_MultiVar()
        {
            testEngine.Run(@"
var a = 1,b = 2, c;

print(a);
print(b);
print(c);");

            Assert.AreEqual("12null", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Assert()
        {
            testEngine.Run(@"
Assert.AreEqual(1,1);
Assert.AreEqual(1,2);
Assert.AreEqual(1,1);");

            StringAssert.Contains("'1' does not equal '2' at ip:'1' in native:'AreEqual'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Approx_Assert()
        {
            testEngine.Run(@"
Assert.AreApproxEqual(1,1);
Assert.AreApproxEqual(1,1.000000001);
Assert.AreApproxEqual(1,2);");

            StringAssert.Contains("'1' and '2' are '-1' apart.", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Assert_Throws()
        {
            testEngine.Run(@"
fun WillThrow()
{
    throw;
}
Assert.Throws(WillThrow);");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }


        [Test]
        public void Engine_Func_Paramless()
        {
            testEngine.Run(@"
fun T
{
    retval = 7;
}

print(T());");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_SelfAssign()
        {
            testEngine.Run(@"
var a = 2;
a = a + 2;
print(a);
a += 2;
print(a);");

            Assert.AreEqual("46", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_SingleProgram_MultiVMs()
        {
            var script = @"print(2);";

            testEngine.Run(script);

            var program = testEngine.MyEngine.Context.Program;

            var engine2 = new ByteCodeInterpreterTestEngine();
            engine2.MyEngine.Context.Vm.Interpret(program.CompiledScripts.First().TopLevelChunk);

            Assert.AreEqual("2", testEngine.InterpreterResult);
            Assert.AreEqual(engine2.InterpreterResult, "2", engine2.InterpreterResult);
        }

        [Test]
        public void Engine_Duplicate_Number_Matches()
        {
            testEngine.Run(@"
var a = 1;
var b = Object.Duplicate(a);
print(b);
b = 2;
print(b);
print(a);");

            Assert.AreEqual("121", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Duplicate_String_Matches()
        {
            testEngine.Run(@"
var a = ""Foo"";
var b = Object.Duplicate(a);
print(b);
b = 2;
print(b);
print(a);");

            Assert.AreEqual("Foo2Foo", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_StringConcat()
        {
            testEngine.Run(@"
var a = 3;
var b = ""Foo"";
print(a+b);");

            Assert.AreEqual("3Foo", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_ContextAssertLibrary_IsFound()
        {
            testEngine.Run(@"
Assert.AreNotEqual(1,2);");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void GenerateByteCodeForLocalAssignment()
        {
            testEngine.Run(@"
fun T(e)
{
    var a;
    var b = null;
    var c = 1;
    var d = a;
    var f = e;
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void RuntimeException_WhenAddNullGlobal_ShouldThrow()
        {
            testEngine.Run(@"
var a = null;
var b = 1;
var c = b + a;");

            Assert.AreEqual(@"Cannot perform op across types 'Double' and 'Null' at ip:'7' in chunk:'root(test:4)'.
===Stack===
<closure root upvals:0>
===CallStack===
chunk:'root(test)'

", testEngine.InterpreterResult);
        }

        [Test]
        public void RuntimeException_WhenAddNullLocal_ShouldThrow()
        {
            testEngine.Run(@"
fun f(a,b)
{
var c = b + a;
}

f(null,1);"
            );

            StringAssert.StartsWith(@"Cannot perform op across types 'Double' and 'Null' ", testEngine.InterpreterResult);
        }

        [Test]
        public void Var_OwnInitialiser_ShouldError()
        {
            testEngine.Run(@"
fun Foo()
{
    var a = a;
}");

            StringAssert.StartsWith("Cannot referenece variable 'a' in it's own initialiser", testEngine.InterpreterResult);
        }

        [Test]
        public void Var_Redeclare_ShouldError()
        {
            testEngine.Run(@"
fun Foo()
{
    var a = 1;
    var a = 2; 
}");

            StringAssert.StartsWith("Cannot declare var with name 'a'", testEngine.InterpreterResult);
        }

        [Test]
        public void VarNumber_UsedAsInstance_ShouldError()
        {
            testEngine.Run(@"
var a = 1;
a.val = 2;");

            StringAssert.StartsWith("Only classes and instances have", testEngine.InterpreterResult);
        }

        [Test]
        public void Global_Missing_ShouldError()
        {
            testEngine.Run(@"
print(hello);");

            StringAssert.StartsWith("No global of name 'hello' could be found", testEngine.InterpreterResult);
        }

        [Test]
        public void VarNumber_UsedAsArray_ShouldError()
        {
            testEngine.Run(@"
var a = 1;
a[1] = 2;");

            StringAssert.StartsWith("Cannot perform set index on type", testEngine.InterpreterResult);
        }

        [Test]
        public void NoLocal_WhenUpValue_ShouldModify()
        {
            testEngine.Run(@"
fun Foo()
{
    var a = 10;

    fun Bar()
    {
        a = 7;
    }

    Bar();
    print(a);
}

Foo();");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }
    }
}