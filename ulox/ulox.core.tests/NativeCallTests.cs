using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class NativeCallTests : EngineTestBase
    {
        private NativeCallResult ReturnNothingExpression(Vm vm)
        {
            return NativeCallResult.SuccessfulExpression;
        }

        [Test]
        public void Engine_Compile_NativeFunc_Call()
        {
            testEngine.MyEngine.Context.Vm.Globals.AddOrSet(new HashedString("CallEmptyNative"), Value.New((vm) =>
            {
                vm.SetNativeReturn(0, Value.New("Native"));
                return NativeCallResult.SuccessfulExpression;
            }, 1, 0));

            testEngine.Run(@"print (CallEmptyNative());");

            Assert.AreEqual("Native", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeFunc_Call_0Param_String()
        {
            NativeCallResult Func(Vm vm)
            {
                vm.SetNativeReturn(0, Value.New("Hello from native."));
                return NativeCallResult.SuccessfulExpression;
            }

            testEngine.MyEngine.Context.Vm.Globals.AddOrSet(new HashedString("Meth"), Value.New(Func,1,0));

            testEngine.Run(@"print (Meth());");

            Assert.AreEqual("Hello from native.", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeFunc_Call_1Param_String()
        {
            NativeCallResult Func(Vm vm)
            {
                vm.SetNativeReturn(0, Value.New($"Hello, {vm.GetArg(1).val.asString}, I'm native."));
                return NativeCallResult.SuccessfulExpression;
            }

            testEngine.MyEngine.Context.Vm.Globals.AddOrSet(new HashedString("Meth"), Value.New(Func,1,1));

            testEngine.Run(@"print (Meth(""Dad""));");

            Assert.AreEqual("Hello, Dad, I'm native.", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturnFromExternalFunction_ShouldMatchExpected()
        {
            testEngine.MyEngine.Context.Vm.Globals.AddOrSet(new HashedString("A1"), Value.New((vm) =>
            {
                vm.SetNativeReturn(0, Value.New(1));
                return NativeCallResult.SuccessfulExpression;
            }, 1, 0));

            testEngine.Run(@"
var a = A1();

print(a);");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturn2FromExternalFunction_ShouldMatchExpected()
        {
            testEngine.MyEngine.Context.Vm.Globals.AddOrSet(new HashedString("A2"), Value.New((vm) =>
            {
                vm.SetNativeReturn(0, Value.New(1));
                vm.SetNativeReturn(1, Value.New(2));
                return NativeCallResult.SuccessfulExpression;
            }, 2, 0));

            testEngine.Run(@"
var (a,b) = A2();

print(a);
print(b);");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturn5FromExternalFunction_ShouldMatchExpected()
        {
            testEngine.MyEngine.Context.Vm.Globals.AddOrSet(new HashedString("A5"), Value.New((vm) =>
            {
                vm.SetNativeReturn(0, Value.New(1));
                vm.SetNativeReturn(1, Value.New(2));
                vm.SetNativeReturn(2, Value.New(3));
                vm.SetNativeReturn(3, Value.New(4));
                vm.SetNativeReturn(4, Value.New(5));
                return NativeCallResult.SuccessfulExpression;
            }, 5, 0));

            testEngine.Run(@"
var (a,b,c,d,e) = A5();

print(a);
print(b);
print(c);
print(d);
print(e);");

            Assert.AreEqual("12345", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturnNothing_ShouldNotHaveError()
        {
            testEngine.MyEngine.Context.Vm.Globals.AddOrSet(new HashedString("A"), Value.New(ReturnNothingExpression, 1, 0));

            testEngine.Run(@"A();");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturnNothingFromExternalFunctionInMiddleOfMathOps_ShouldNotImpactMathOps()
        {
            testEngine.MyEngine.Context.Vm.Globals.AddOrSet(new HashedString("A"), Value.New(ReturnNothingExpression, 1, 0));

            testEngine.Run(@"
var a = 1;
var b = 2;
A();

a += b + 1;

print(a);");

            Assert.AreEqual("4", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturnNothingFromExternalFunctionInMiddleOfMathOpsAndAllLocals_ShouldNotImpactMathOps()
        {
            testEngine.MyEngine.Context.Vm.Globals.AddOrSet(new HashedString("A"), Value.New(ReturnNothingExpression, 1, 0));

            testEngine.Run(@"
fun Locals()
{
    var a = 1;
    var b = 2;
    A();

    a += b + 1;

    print(a);
}

Locals();");

            Assert.AreEqual("4", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenNativeFuncWithMultipleArgs_ShouldReceiveAllInOrder()
        {
            NativeCallResult Func(Vm vm)
            {
                var index = 0;
                var a = vm.GetNextArg(ref index).val.asString;
                var b = vm.GetNextArg(ref index).val.asString;
                var c = vm.GetNextArg(ref index).val.asString;
                vm.SetNativeReturn(0, Value.New($"Hello, {a}, {b}, and {c}, I'm native."));
                return NativeCallResult.SuccessfulExpression;
            }

            testEngine.MyEngine.Context.Vm.Globals.AddOrSet(new HashedString("Meth"), Value.New(Func, 1, 3));

            testEngine.Run(@"print (Meth(""you"", ""me"", ""everybody""));");

            Assert.AreEqual("Hello, you, me, and everybody, I'm native.", testEngine.InterpreterResult);
        }
    }
}
