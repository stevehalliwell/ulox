using NUnit.Framework;

namespace ULox.Core.Tests
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
fun Del(a,b) {retval = a+b;}

fun DelInvoker(d, lhs, rhs) 
{
    retval = d(lhs,rhs);
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
    retval = lhs+rhs;
}

fun Foo(a,b,c)
{
    retval = a(b,c);
}

var res = Foo(Meth,1,2);
print(res);
");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Fun_WhenNamedAndRhsOfAssign_ShouldAssignAndBeGlobal()
        {
            testEngine.Run(@"
var foo = {:};
foo.bar = fun Bar(a)
{
    print(a);
};

foo.bar(1);
Bar(2);
");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        [Test]
        public void Fun_WhenAnonAndRhsOfAssign_ShouldAssignAndNotBeGlobal()
        {
            testEngine.Run(@"
var foo = {:};
foo.bar = fun (a)
{
    print(a);
};

foo.bar(1);
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Fun_WhenNamedReturns_ShouldPass()
        {
            testEngine.Run(@"
fun T(a,b) (c)
{
    c = a+b;
    retval = c;
}
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Fun_WhenNamedReturnsC_ShouldPrint3()
        {
            testEngine.Run(@"
fun T(a,b) (c)
{
    c = a+b;
}

var res = T(1,2);
print(res);
");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Fun_WhenNamedReturnsImplicit_ShouldPrint3()
        {
            testEngine.Run(@"
fun T(a,b) (c)
{
    c = a+b;
    return;
}

var res = T(1,2);
print(res);
");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Fun_WhenMultiNamedReturnsImplicit_ShouldPrint3AndNeg1()
        {
            testEngine.Run(@"
fun T(a,b) (c,d)
{
    c = a+b;
    d = a-b;
    return;
}

var (add, sub)= T(1,2);
print(add);
print(sub);
");

            Assert.AreEqual("3-1", testEngine.InterpreterResult);
        }

        [Test]
        public void Fun_WhenMultiNamedOmittedReturns_ShouldPrint3AndNeg1()
        {
            testEngine.Run(@"
fun T(a,b) (c,d)
{
    c = a+b;
    d = a-b;
}

var (add, sub) = T(1,2);
print(add);
print(sub);
");

            Assert.AreEqual("3-1", testEngine.InterpreterResult);
        }

        [Test]
        public void Fun_WhenManyMultiNamedOmittedReturns_ShouldPrintMany()
        {
            testEngine.Run(@"
fun T(a,b) (c,d,e,f,g)
{
    c = a+b;
    d = a-b;
    e = a*b;
    f = a/b;
    g = a%b;
}

var (add, sub, mul, div, mod) = T(1,2);
print(add);
print(sub);
print(mul);
print(div);
print(mod);
");

            Assert.AreEqual("3-120.51", testEngine.InterpreterResult);
        }
    }
}