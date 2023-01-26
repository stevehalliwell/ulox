using NUnit.Framework;
using ULox;

namespace ulox.core.tests
{
    [TestFixture]
    public class MutliReturnTests : EngineTestBase
    {
        [Test]
        public void Vec2Add_WhenGivenKnowValues_ShouldReturnExpected()
        {
            testEngine.Run(@"
var a = 1,b = 2,c = 3,d = 4;

fun Add(x1,y1, x2, y2) (x,y) 
{
    x = x1 + x2;
    y = y1 + y2;
}

var (x,y) = Add(a,b,c,d);

print(x);
print(y);");

            Assert.AreEqual("46", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenMultiVarAssignInline_ShouldMatchExpected()
        {
            testEngine.Run(@"
var (a,b) = (1,2);

print(a);
print(b);");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

//        [Test]
//        public void Run_WhenReturn4OfTheReturn2WithOperationsInMiddleStack_ShouldMatchExpected()
//        {
//            testEngine.Run(@"
//fun Outter(){return (1,2);}

//fun A2()
//{
//    var (c,d) = Outter();
//    c += 1;
//    d = c*c + d;
//    var (a,b) = Outter(); 
//    return (a,b,c,d);
//}

//var (a,b,c,d) = A2();

//print(a);
//print(b);
//print(c);
//print(d);");

//            Assert.AreEqual("12310", testEngine.InterpreterResult);
//        }

        [Test]
        public void Run_WhenReturnNothingFromExternalFunctionInMiddleOfMathOpsAndAllLocals_ShouldNotImpactMathOps()
        {
            testEngine.MyEngine.Context.Vm.SetGlobal(new HashedString("A"), Value.New(ReturnNothing));

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
        public void Run_WhenReturnNothing_ShouldNotHaveError()
        {
            testEngine.MyEngine.Context.Vm.SetGlobal(new HashedString("A"), Value.New(ReturnNothing));

            testEngine.Run(@"A();");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturnNothingFromExternalFunctionInMiddleOfMathOps_ShouldNotImpactMathOps()
        {
            testEngine.MyEngine.Context.Vm.SetGlobal(new HashedString("A"), Value.New(ReturnNothing));

            testEngine.Run(@"
var a = 1;
var b = 2;
A();

a += b + 1;

print(a);");

            Assert.AreEqual("4", testEngine.InterpreterResult);
        }

        private NativeCallResult ReturnNothing(Vm vm, int argc)
        {
            return NativeCallResult.SuccessfulExpression;
        }

        [Test]
        public void Run_WhenReturnFromExternalFunction_ShouldMatchExpected()
        {
            testEngine.MyEngine.Context.Vm.SetGlobal(new HashedString("A1"), Value.New(Return1Thing));

            testEngine.Run(@"
var a = A1();

print(a);");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        private NativeCallResult Return1Thing(Vm vm, int arg2)
        {
            vm.PushReturn(Value.New(1));
            return NativeCallResult.SuccessfulExpression;
        }

        [Test]
        public void Run_WhenReturn2FromExternalFunction_ShouldMatchExpected()
        {
            testEngine.MyEngine.Context.Vm.SetGlobal(new HashedString("A2"), Value.New(Return2Thing));

            testEngine.Run(@"
var (a,b) = A2();

print(a);
print(b);");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        private NativeCallResult Return2Thing(Vm vm, int arg2)
        {
            vm.PushReturn(Value.New(1));
            vm.PushReturn(Value.New(2));
            return NativeCallResult.SuccessfulExpression;
        }

        [Test]
        public void Run_WhenReturn2AndTake2_ShouldMatchExpected()
        {
            testEngine.Run(@"
fun A()(a,b){a=1;b=2;}

var (res1,res2) = A();

print(res1);
print(res2);");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturn2AndTake2WithParams_ShouldMatchExpected()
        {
            testEngine.Run(@"
fun A(a,b)(retA, retB){retA = a+1; retB = b+2;}

var (res1,res2) = A(1,2);

print(res1);
print(res2);");

            Assert.AreEqual("24", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturn2Take1_ShouldError()
        {
            testEngine.Run(@"
fun A()(a,b){a=1;b=2;}

var (res1) = A(); //2 is left on stack

print (res1);");

            StringAssert.StartsWith("Multi var assign to result mismatch. Taking '1' but results contains '2' at ip:'8' in chunk:'unnamed_chunk(test:4)'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturn2Take3_ShouldError()
        {
            testEngine.Run(@"
fun A()(a,b){a=1;b=2;}

var (res1,res2,res3) = A();

print (res1);
print (res2);
print (res3);");

            StringAssert.StartsWith("Multi var assign to result mismatch. Taking '3' but results contains '2' at ip:'8' in chunk:'unnamed_chunk(test:4)'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturn5AndTake5_ShouldMatchExpected()
        {
            testEngine.Run(@"
fun A() (a,b,c,d,e) {a=1;b=2;c=3;d=4;e=5;}

var (a,b,c,d,e) = A();

print(a);
print(b);
print(c);
print(d);
print(e);");

            Assert.AreEqual("12345", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturnNoneTake2_ShouldError()
        {
            testEngine.Run(@"
fun A(){return;}

var (res1,res2) = A();

print(res1);
print(res2);");

            StringAssert.StartsWith("Multi var assign to result mismatch. Taking '2' but results contains '1' at ip:'8' in chunk:'unnamed_chunk(test:4)'.", testEngine.InterpreterResult);
        }
    }
}
