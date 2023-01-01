using NUnit.Framework;
using ULox;

namespace ulox.core.tests
{
    [TestFixture]
    public class ByteCodeEngineTests : EngineTestBase
    {
        public static Chunk GenerateManualChunk()
        {
            var chunk = new Chunk("main", "native", FunctionType.Script);

            chunk.AddConstantAndWriteInstruction(Value.New(0.5), 1);
            chunk.AddConstantAndWriteInstruction(Value.New(1), 1);
            chunk.WritePacket(new ByteCodePacket(OpCode.NEGATE), 1);
            chunk.WritePacket(new ByteCodePacket(OpCode.ADD), 1);
            chunk.AddConstantAndWriteInstruction(Value.New(2), 1);
            chunk.WritePacket(new ByteCodePacket(OpCode.MULTIPLY), 1);
            chunk.WritePacket(new ByteCodePacket(OpCode.RETURN, ReturnMode.One), 2);

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

        [Test]
        public void Engine_Cycle_Global_Var_DoubleRun()
        {
            var script = @"var myVar = 10;
var myNull;
print (myVar);
print (myNull);

var myOtherVar = myVar * 2;

print (myOtherVar);";

            testEngine.Run(script);
            testEngine.Run(script);

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
        public void Engine_Compile_NativeFunc_Call()
        {
            testEngine.MyEngine.Context.Vm.SetGlobal(new HashedString("CallEmptyNative"), Value.New((vm, stack) =>
            {
                vm.PushReturn(Value.New("Native"));
                return NativeCallResult.SuccessfulExpression;
            }));

            testEngine.Run(@"print (CallEmptyNative());");

            Assert.AreEqual("Native", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_Return()
        {
            testEngine.Run(@"
fun A(){return 1;}

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

        //TODO single take of a multi return
        //todo multivar assign replace existing named variable

        [Test]
        public void Engine_Compile_Call_Mixed_Ops()
        {
            testEngine.Run(@"
fun A(){return 2;}
fun B(){return 3;}
fun C(){return 10;}

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
    if(v > 5)
        return 2;
    return -1;
}
fun B(){return 3;}
fun C(){return 10;}

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
        public void Engine_Compile_Recursive()
        {
            testEngine.Run(@"fun Recur(a)
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
    return inner;
  }

  print (""return from outer"");
  return middle;
}

var mid = outer();
var in = mid();
in();");

            Assert.AreEqual(@"return from outercreate inner closurevalue", testEngine.InterpreterResult);
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

  return count;
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
            testEngine.Run(@"
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
            testEngine.MyEngine.Context.Vm.SetGlobal(new HashedString("a"), Value.New(1));

            testEngine.Run(@"print (a);");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Call_External_NoParams()
        {
            testEngine.Run(@"fun Meth(){print (1);}");

            var meth = testEngine.MyEngine.Context.Vm.GetGlobal(new HashedString("Meth"));
            testEngine.MyEngine.Context.Vm.PushCallFrameAndRun(meth, 0);

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeFunc_Call_0Param_String()
        {
            NativeCallResult Func(Vm vm, int args)
            {
                vm.PushReturn(Value.New("Hello from native."));
                return NativeCallResult.SuccessfulExpression;
            }

            testEngine.MyEngine.Context.Vm.SetGlobal(new HashedString("Meth"), Value.New(Func));

            testEngine.Run(@"print (Meth());");

            Assert.AreEqual("Hello from native.", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeFunc_Call_1Param_String()
        {
            NativeCallResult Func(Vm vm, int args)
            {
                vm.PushReturn(Value.New($"Hello, {vm.GetArg(1).val.asString}, I'm native."));
                return NativeCallResult.SuccessfulExpression;
            }

            testEngine.MyEngine.Context.Vm.SetGlobal(new HashedString("Meth"), Value.New(Func));

            testEngine.Run(@"print (Meth(""Dad""));");

            Assert.AreEqual("Hello, Dad, I'm native.", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NestedCalls()
        {
            testEngine.Run(@"
fun A(){return 7;}
fun B(v){return 1+v;}

var res = B(A());
print (res);
");

            Assert.AreEqual("8", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Throw()
        {
            Assert.Throws<PanicException>(() => testEngine.Run(@"throw;"), "Null");
        }

        [Test]
        public void Engine_Throw_Exp()
        {
            Assert.Throws<PanicException>(() => testEngine.Run(@"throw 2+3;"), "5");
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

            StringAssert.StartsWith("'1' does not equal '2' at ip:'1' in native:'AreEqual'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Approx_Assert()
        {
            testEngine.Run(@"
Assert.AreApproxEqual(1,1);
Assert.AreApproxEqual(1,1.000000001);
Assert.AreApproxEqual(1,2);");

            StringAssert.StartsWith("'1' and '2' are '-1' apart.", testEngine.InterpreterResult);
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
    return 7;
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

            var engine2 = new ByteCodeInterpreterTestEngine(System.Console.WriteLine);
            engine2.MyEngine.Context.Vm.Run(program);

            Assert.AreEqual("2", testEngine.InterpreterResult);
            Assert.AreEqual(engine2.InterpreterResult, "2", engine2.InterpreterResult);
        }

        [Test]
        public void Engine_InternalSandbox_CanPassIn()
        {
            testEngine.Run(@"
fun InnerMain()
{
    print(globalIn);
}

var globalIn = 10;

var innerVM = VM();
innerVM.AddGlobal(""globalIn"",globalIn);
innerVM.AddGlobal(""print"",print);

innerVM.Start(InnerMain);");

            Assert.AreEqual("10", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_InternalSandbox_CanWriteGlobalOut()
        {
            testEngine.Run(@"
fun InnerMain()
{
    globalOut = 10;
}

var globalOut = 0;

var innerVM = VM();
innerVM.AddGlobal(""globalOut"",globalOut);

innerVM.Start(InnerMain);

print(innerVM.GetGlobal(""globalOut""));");

            Assert.AreEqual("10", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_InternalSandbox_CanReturnOut()
        {
            testEngine.Run(@"
fun InnerMain()
{
    return 10;
}

var innerVM = VM();

var res = innerVM.Start(InnerMain);

print(res);");

            Assert.AreEqual("10", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_InternalSandbox_YieldResume()
        {
            testEngine.Run(@"
var a = 2;
a = a + 2;
yield;
print(a);");

            testEngine.MyEngine.Context.Vm.Run();

            Assert.AreEqual("4", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_ChildVM_Run()
        {
            testEngine.Run(@"
fun InnerMain()
{
    print(""Hello from inner "" + a);
}

var a = ""10"";

var innerVM = VM();
innerVM.InheritFromEnclosing();
innerVM.Start(InnerMain);");

            Assert.AreEqual("Hello from inner 10", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Sandbox_CannotAccess()
        {
            testEngine.Run(@"
var a = 10;
fun InnerMain()
{
    a = 10;
}

var innerVM = VM();
innerVM.Start(InnerMain);");

            StringAssert.StartsWith("Global var of name 'a' was not found at ip:'2' in chunk:'InnerMain(test:5)'.", testEngine.InterpreterResult, testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Sandbox_AsGenerator()
        {
            testEngine.Run(@"
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

            Assert.AreEqual("112358", testEngine.InterpreterResult);
        }
        [Test]
        public void Engine_Duplicate_Number_Matches()
        {
            testEngine.Run(@"
var a = 1;
var b = Duplicate(a);
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
var b = Duplicate(a);
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
        public void Engine_StringConcat_ViaFunc()
        {
            testEngine.Run(@"
var a = 3;
var b = ""Foo"";
print(str(a)+str(b));");

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
}
"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void RuntimeException_WhenAddNullGlobal_ShouldThrow()
        {
            testEngine.Run(@"
var a = null;
var b = 1;
var c = b + a;
"
            );

            Assert.AreEqual(@"Cannot perform op across types 'Double' and 'Null' at ip:'7' in chunk:'unnamed_chunk(test:4)'.
===Stack===
<closure unnamed_chunk upvals:0>

===CallStack===
chunk:'unnamed_chunk(test)'

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

            Assert.AreEqual(@"Cannot perform op across types 'Double' and 'Null' at ip:'3' in chunk:'f(test:5)'.
===Stack===
1
null
<closure f upvals:0>
<closure unnamed_chunk upvals:0>

===CallStack===
chunk:'f(test)'
chunk:'unnamed_chunk(test)'

", testEngine.InterpreterResult);
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

            StringAssert.StartsWith("Already a variable with name 'a'", testEngine.InterpreterResult);
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
        public void VarNumber_UsedAsArray_ShouldError()
        {
            testEngine.Run(@"
var a = 1;
a[1] = 2;");

            StringAssert.StartsWith("Cannot perform set index on type", testEngine.InterpreterResult);
        }

        //        [Test]
        //        public void Engine_Cycle_Minus_Equals_Expression()
        //        {
        //            testEngine.Run(@"
        //var a = 1;
        //a -= 2;
        //print (a);");

        //            Assert.AreEqual("-1", testEngine.InterpreterResult);
        //        }
    }
}
