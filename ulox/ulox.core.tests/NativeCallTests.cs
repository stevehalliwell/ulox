using NUnit.Framework;
using ULox;

namespace ulox.core.tests
{
    [TestFixture]
    public class NativeCallTests: EngineTestBase
    {
        private NativeCallResult ReturnNothingExpression(Vm vm, int argc)
        {
            return NativeCallResult.SuccessfulExpression;
        }
        
        private NativeCallResult ReturnNothingStatement(Vm vm, int argc)
        {
            return NativeCallResult.SuccessfulStatement;
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
        public void Run_WhenReturnFromExternalFunction_ShouldMatchExpected()
        {
            testEngine.MyEngine.Context.Vm.SetGlobal(new HashedString("A1"), Value.New((vm, args) =>
            {
                vm.PushReturn(Value.New(1));
                return NativeCallResult.SuccessfulExpression;
            }));

            testEngine.Run(@"
var a = A1();

print(a);");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturn2FromExternalFunction_ShouldMatchExpected()
        {
            testEngine.MyEngine.Context.Vm.SetGlobal(new HashedString("A2"), Value.New((vm, args) =>
            {
                vm.PushReturn(Value.New(1));
                vm.PushReturn(Value.New(2));
                return NativeCallResult.SuccessfulExpression;
            }));

            testEngine.Run(@"
var (a,b) = A2();

print(a);
print(b);");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturn5FromExternalFunction_ShouldMatchExpected()
        {
            testEngine.MyEngine.Context.Vm.SetGlobal(new HashedString("A5"), Value.New((vm, args) =>
            {
                vm.PushReturn(Value.New(1));
                vm.PushReturn(Value.New(2));
                vm.PushReturn(Value.New(3));
                vm.PushReturn(Value.New(4));
                vm.PushReturn(Value.New(5));
                return NativeCallResult.SuccessfulExpression;
            }));

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
            testEngine.MyEngine.Context.Vm.SetGlobal(new HashedString("A"), Value.New(ReturnNothingExpression));

            testEngine.Run(@"A();");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturnNothingFromExternalFunctionInMiddleOfMathOps_ShouldNotImpactMathOps()
        {
            testEngine.MyEngine.Context.Vm.SetGlobal(new HashedString("A"), Value.New(ReturnNothingExpression));

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
            testEngine.MyEngine.Context.Vm.SetGlobal(new HashedString("A"), Value.New(ReturnNothingExpression));

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
    }
}
