using NUnit.Framework;

namespace ULox.Tests
{
    public class FunctionAndDelegateTests : EngineTestBase
    {
        [Test]
        public void Delegate_WhenNoArgsAndCalled_ShouldInvoke()
        {
            testEngine.Run(@"
fun Del() {print(""Hello"");}
var del = Del;
del();
");

            Assert.AreEqual("Hello", testEngine.InterpreterResult);
        }

        [Test]
        public void Delegate_When1ArgsAndCalled_ShouldInvoke()
        {
            testEngine.Run(@"
fun Del(a) {print(a);}
var del = Del;
del(1);
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Delegate_When2ArgsAndCalled_ShouldInvoke()
        {
            testEngine.Run(@"
fun Del(a,b) {print(a+b);}
var del = Del;
del(1,2);
");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Delegate_WhenPassedAsAnArgAndInvoked_ShouldInvoke()
        {
            testEngine.Run(@"
fun Del(a,b) {print(a+b);}
var del = Del;

fun DelInvoker(d, lhs, rhs) 
{
    d(lhs,rhs);
}

DelInvoker(del,1,2);
");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Delegate_WhenPassedAsAnArgAndInvokedAndReturning_ShouldMatchExpected()
        {
            testEngine.Run(@"
fun Del(a,b) {return a+b;}

fun DelInvoker(d, lhs, rhs) 
{
    return d(lhs,rhs);
}

var del = Del;
var res = DelInvoker(del,1,2);

print(res);
");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Delegate_WhenPassedAsAnArgAndInvokedAndReturning_ShouldMatchExpected_Alt()
        {
            testEngine.Run(@"
fun Meth(lhs,rhs)
{
    return lhs+rhs;
}

fun Foo(a,b,c)
{
    return a(b,c);
}

var res = Foo(Meth,1,2);
print(res);
");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }
    }
}