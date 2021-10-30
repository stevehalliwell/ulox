using NUnit.Framework;

namespace ULox.Tests
{
    [TestFixture]
    public class MutliReturnTests : EngineTestBase
    {
        [Test]
        public void Engine_Compile_ReturnNothing()
        {
            testEngine.Run(@"
fun A(){return; var b = 2;}

var res1 = A(); //right now when nothing is returned gets a null

print (res1);");

            Assert.AreEqual("null", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_ReturnMultiple_ButNoneSupplied()
        {
            testEngine.Run(@"
fun A(){return ();}

var (res1) = A();

print (res1);");

            Assert.AreEqual("null", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_ReturnMultiple_AskingForMoreThanReturned()
        {
            testEngine.Run(@"
fun A(){return (1,2);}

var (res1,res2,res3) = A();

print (res1);
print (res2);
print (res3);");

            Assert.AreEqual("12null", testEngine.InterpreterResult);
        }


        [Test]
        public void Engine_Compile_ReturnMultiple_IntoExistingVars()
        {
            testEngine.Run(@"
fun A(){return (1,2,3);}

var a,b,c;

(a,b,c) = A();

print (a);
print (b);
print (c);");

            Assert.AreEqual("123", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_ReturnMultiple_TakeFirstOnly()
        {
            testEngine.Run(@"
fun A(){return (1,2);}

var (res1) = A(); //2 is left on stack

print (res1);");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_ReturnMultiple_TakeBoth_ViaValueStackTop()
        {
            testEngine.Run(@"
fun A(){return (1,2);}

var (res1) = A(); //2 is left on stack
var res2 = valuestacktop;

print(res1);
print(res2);");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_ReturnMultiple_TakeBoth_ViaComboAssign()
        {
            testEngine.Run(@"
fun A(){return (1,2);}

var (res1,res2) = A();

print(res1);
print(res2);");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Compile_ReturnMultiple()
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

//        [Test]
//        public void Engine_Compile_ReturnMultiple_TakeTooMany_ShouldError()
//        {
//            testEngine.Run(@"
//fun A(){return (1,2);}

//var (a,b,c) = A();

//print(a);
//print(b);
//print(c);");

//            Assert.AreEqual("123", testEngine.InterpreterResult);
//        }

        //fails as A thinks it knows there is 1 thing to return but there is many
        [Test]
        public void Engine_Compile_ReturnMultiple_Wrapped()
        {
            testEngine.Run(@"
fun Outter(){return (1,2);}

fun A(){return (Outter());}

var (a,b) = A();

print(a);
print(b);");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }
        [Test]
        public void Engine_Compile_ReturnMultiple_DoubleWrapped()
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

        //cannot work as it things there are 2 return values but it doesn't know that
        [Test]
        public void Engine_Compile_ReturnMultiple_Nested()
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

        //cannot work as it things there are 2 return values but it doesn't know that
        [Test]
        public void Engine_Compile_ReturnMultiple_Nested_WithOp()
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
    }
}
