using NUnit.Framework;

namespace ULox.Tests
{
    public class ContractTests : EngineTestBase
    {
        [Test]
        public void Meets_WhenITAndTMatch_ShouldCompileClean()
        {
            testEngine.Run(@"
class IT
{
    Required(){}
}

class T 
{
    meets IT;
    Required(){}
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenITAndTDoNotMatch_ShouldError()
        {
            testEngine.Run(@"
class IT
{
    Required(){}
}

class T 
{
    meets IT;
}");

            Assert.AreEqual("Meets violation. 'T' meets 'IT' does not contain matching method 'Required'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenITAndTDoNotArity_ShouldError()
        {
            testEngine.Run(@"
class IT
{
    Required(){}
}

class T 
{
    meets IT;
    Required(a){}
}");

            Assert.AreEqual("Meets violation. 'T' meets 'IT' has method 'Required' but expected arity of '0' but has '1'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenITAndTSubMatch_ShouldCompileClean()
        {
            testEngine.Run(@"
class IT
{
    Required(){}
}

class T 
{
    Required(){}
}

class TSub < T 
{
    meets IT;
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenITAndTMixinMatch_ShouldCompileClean()
        {
            testEngine.Run(@"
class IT
{
    Required(){}
}

class T 
{
    Required(){}
}

class TSub
{
    mixin T;
    meets IT;
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenLocalITAndTMismatch_ShouldFail()
        {
            testEngine.Run(@"
class IT
{
    local Required(){}
}

class T 
{
    meets IT;
    Required(){}
}");

            Assert.AreEqual("Meets violation. 'T' meets 'IT' expected local but is of type 'Method'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenITAndTLocalMismatch_ShouldCompileClean()
        {
            testEngine.Run(@"
class IT
{
    Required(){}
}

class T 
{
    meets IT;
    local Required(){}
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenLocalITAndTLocalMismatch_ShouldCompileClean()
        {
            testEngine.Run(@"
class IT
{
    local Required(){}
}

class T 
{
    meets IT;
    local Required(){}
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenPureITAndTMismatch_ShouldFail()
        {
            testEngine.Run(@"
class IT
{
    pure Required(){}
}

class T 
{
    meets IT;
    Required(){}
}");

            Assert.AreEqual("Meets violation. 'T' meets 'IT' expected pure but is of type 'Method'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenITAndTPureMismatch_ShouldCompileClean()
        {
            testEngine.Run(@"
class IT
{
    Required(){}
}

class T 
{
    meets IT;
    pure Required(){}
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenPureITAndTPureMismatch_ShouldCompileClean()
        {
            testEngine.Run(@"
class IT
{
    pure Required(){}
}

class T 
{
    meets IT;
    pure Required(){}
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenAInstAndBClassMismatch_ShouldFail()
        {
            testEngine.Run(@"
class A
{
    Meth(i){}
}

class B
{
    Meth(){}
}

var inst = A();

var res = inst meets B;
print(res);
");

            Assert.AreEqual("False", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenAInstAndBMatch_ShouldCompileClean()
        {
            testEngine.Run(@"
class A
{
    Meth(i){}
}

class B
{
    Meth(i){}
}

var inst = A();

var res = inst meets B;
print(res);
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenAInstAndBInstMatch_ShouldCompileClean()
        {
            testEngine.Run(@"
class A
{
    Meth(i){}
}

class B
{
    Meth(i){}
}

var inst = A();
var binst = B();

var res = inst meets binst;
print(res);
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenAInstAndBInstMismatch_ShouldFail()
        {
            testEngine.Run(@"
class A
{
    Meth(i){}
}

class B
{
    Meth(){}
}

var inst = A();
var binst = B();

var res = inst meets binst;
print(res);
");

            Assert.AreEqual("False", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenADynamicAndBDynamicMatch_ShouldCompile()
        {
            testEngine.Run(@"
var inst = {:};
fun instMeth(a){}
inst.Meth = instMeth;

var binst = {:};
fun binstMeth(){}
binst.Meth = binstMeth;


var res = inst meets binst;
print(res);
");

            Assert.AreEqual("False", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenADynamicAndBDynamicMismatch_ShouldCompile()
        {
            testEngine.Run(@"
var inst = {:};
fun instMeth(a){}
inst.Meth = instMeth;

var binst = {:};
binst.Meth = instMeth;


var res = inst meets binst;
print(res);
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }
        //todo compare fields not just methods/closures
    }
}
