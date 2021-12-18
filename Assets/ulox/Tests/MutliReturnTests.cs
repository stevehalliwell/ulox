using NUnit.Framework;
using System;

namespace ULox.Tests
{
    [TestFixture]
    public class MutliReturnTests : EngineTestBase
    {
        //cannot work as it things there are 2 return values but it doesn't know that
        [Test]
        public void Run_WhenReturn4OfTheReturn2WithOperationsInMiddleStack_ShouldMatchExpected()
        {
            testEngine.Run(@"
fun Outter(){return (1,2);}

fun A2()
{
    var (c,d) = Outter();
    c += 1;
    d = c*c + d;
    return (Outter(),c,d);
}

var (a,b,c,d) = A2();

print(a);
print(b);
print(c);
print(d);");

            Assert.AreEqual("12310", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturnFromExternalFunction_ShouldMatchExpected()
        {
            testEngine.Vm.SetGlobal(new HashedString("A1"), Value.New(Return1Thing));

            testEngine.Run(@"
var a = A1();

print(a);");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        private NativeCallResult Return1Thing(VMBase vm, int arg2)
        {
            vm.PushReturn(Value.New(1));
            return NativeCallResult.Success;
        }

        [Test]
        public void Run_WhenReturn2FromExternalFunction_ShouldMatchExpected()
        {
            testEngine.Vm.SetGlobal(new HashedString("A2"), Value.New(Return2Thing));

            testEngine.Run(@"
var (a,b) = A2();

print(a);
print(b);");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        private NativeCallResult Return2Thing(VMBase vm, int arg2)
        {
            vm.PushReturn(Value.New(1));
            vm.PushReturn(Value.New(2));
            return NativeCallResult.Success;
        }

        [Test]
        public void Run_WhenReturn2AndTake2_ShouldMatchExpected()
        {
            testEngine.Run(@"
fun A(){return (1,2);}

var (res1,res2) = A();

print(res1);
print(res2);");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        //cannot work as it things there are 2 return values but it doesn't know that
        [Test]
        public void Run_WhenReturn2OfTheReturnOf2AndTake2_ShouldMatchExpected()
        {
            testEngine.Run(@"
fun Outter(){return (1,2);}

fun A2(){return (Outter(),Outter());}

var (a,b,c,d) = A2();

print(a);
print(b);
print(c);
print(d);");

            Assert.AreEqual("1212", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturn2Take1_ShouldError()
        {
            testEngine.Run(@"
fun A(){return (1,2);}

var (res1) = A(); //2 is left on stack

print (res1);");

            Assert.AreEqual("error", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturn2Take3_ShouldError()
        {
            testEngine.Run(@"
fun A(){return (1,2);}

var (res1,res2,res3) = A();

print (res1);
print (res2);
print (res3);");

            Assert.AreEqual("error", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturn5AndTake5_ShouldMatchExpected()
        {
            testEngine.Run(@"
fun A(){return (1,2,3,4,5);}

var (a,b,c,d,e) = A();

print(a);
print(b);
print(c);
print(d);
print(e);");

            Assert.AreEqual("12345", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturnNoneTake1_ShouldError()
        {
            testEngine.Run(@"
fun A(){return;}

var res1 = A();");

            Assert.AreEqual("error", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturnTheOfReturnTheOfReturnOf2AndTake2_ShouldMatchExpected()
        {
            testEngine.Run(@"

fun Outter(){return (1,2);}
fun ReturnPassThrough(){return (Outter());}
fun ReturnPassThroughPassThrough(){return (ReturnPassThrough());}

fun A(){return (ReturnPassThroughPassThrough());}

var (a,b) = A();

print(a);
print(b);");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturnTheReturnOf2AndTake2_ShouldMatchExpected()
        {
            testEngine.Run(@"
fun Outter(){return (1,2);}
fun ReturnPassThrough(){return (Outter());}

fun A(){return (ReturnPassThrough());}

var (a,b) = A();

print(a);
print(b);");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }
    }
}
