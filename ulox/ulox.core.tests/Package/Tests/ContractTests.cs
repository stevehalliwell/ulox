using NUnit.Framework;

namespace ULox.Tests
{
    public class ContractTests : EngineTestBase
    {
        [Test]
        public void Meets_WhenITAndTMatch_ShouldNotThrow()
        {
            testEngine.Run(@"
class IT
{
    Required(){}
}

class T 
{
    signs IT;
    Required(){}
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Meets_WhenITAndTMatchWithVars_ShouldNotThrow()
        {
            testEngine.Run(@"
class IT
{
    var a;
    Required(){}
}

class T 
{
    signs IT;
    var a;
    Required(){}
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }


        [Test]
        public void Meets_WhenITAndTDoNotMatch_ShouldThrow()
        {
            testEngine.Run(@"
class IT
{
    Required(){}
}

class T 
{
    signs IT;
}");

            Assert.AreEqual("'T' does not contain matching method 'Required'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenITAndTDoNotArity_ShouldThrow()
        {
            testEngine.Run(@"
class IT
{
    Required(){}
}

class T 
{
    signs IT;
    Required(a){}
}");

            Assert.AreEqual("Expected arity '0' but found '1'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenITAndTMixinMatch_ShouldNotThrow()
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
    signs IT;
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenLocalITAndTMismatch_ShouldThrow()
        {
            testEngine.Run(@"
class IT
{
    local Required(){}
}

class T 
{
    signs IT;
    Required(){}
}");

            Assert.AreEqual("Expected local but found 'Method'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenITAndTLocalMismatch_ShouldNotThrow()
        {
            testEngine.Run(@"
class IT
{
    Required(){}
}

class T 
{
    signs IT;
    local Required(){}
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenLocalITAndTLocalMatch_ShouldNotThrow()
        {
            testEngine.Run(@"
class IT
{
    local Required(){}
}

class T 
{
    signs IT;
    local Required(){}
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenPureITAndTMismatch_ShouldThrow()
        {
            testEngine.Run(@"
class IT
{
    pure Required(){}
}

class T 
{
    signs IT;
    Required(){}
}");

            Assert.AreEqual("Expected pure but found 'Method'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenITAndTPureMismatch_ShouldNotThrow()
        {
            testEngine.Run(@"
class IT
{
    Required(){}
}

class T 
{
    signs IT;
    pure Required(){}
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenPureITAndTPureMatch_ShouldNotThrow()
        {
            testEngine.Run(@"
class IT
{
    pure Required(){}
}

class T 
{
    signs IT;
    pure Required(){}
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenAInstAndBClassMismatch_ShouldReturnFalse()
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
        public void Meets_WhenAClassAndBClassMatch_ShouldReturnTrue()
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
var res = A meets B;
print(res);
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenAInstAndBMatch_ShouldReturnTrue()
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
        public void Meets_WhenAInstAndBInstMatch_ShouldReturnTrue()
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
        public void Meets_WhenAInstAndBInstMismatch_ShouldReturnFalse()
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
        public void Meets_WhenADynamicAndBDynamicMismatch_ShouldReturnFalse()
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
        public void Meets_WhenADynamicAndBDynamicMatch_ShouldReturnTrue()
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

        [Test]
        public void Meets_WhenADynamicAndBDynamicFieldMatch_ShouldReturnTrue()
        {
            testEngine.Run(@"
var inst = {:};
inst.a = 3;

var binst = {:};
binst.a = 7;

var res = inst meets binst;
print(res);
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenADynamicAndBEmptyMatch_ShouldReturnTrue()
        {
            testEngine.Run(@"
var inst = {:};
inst.a = 3;

var binst = {:};

var res = inst meets binst;
print(res);
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenAEmptyAndBMismatch_ShouldReturnFalse()
        {
            testEngine.Run(@"
var inst = {:};

var binst = {:};
binst.a = 3;

var res = inst meets binst;
print(res);
");

            Assert.AreEqual("False", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenAFromJsonAndBDynamicMatch_ShouldReturnTrue()
        {
            testEngine.Run(@"
var jsonString = ""{ \""a\"": 1.0,  \""b\"": 2.0,  \""c\"": 3.0 }"";
var inst = Serialise.FromJson(jsonString);

var binst = {:};
binst.a = 0;
binst.b = 0;
binst.c = 0;

var res = inst meets binst;
print(res);
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenADeepAndBDeepButMismatch_ShouldReturnFalse()
        {
            testEngine.Run(@"
var inst = {:};
inst.a = 1;
inst.b = {:};

var binst = {:};
binst.a = 3;
binst.b = {:};
binst.b.c = 3;

var res = inst meets binst;
print(res);
");

            Assert.AreEqual("False", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenADeepAndBDeepMatch_ShouldReturnTrue()
        {
            testEngine.Run(@"
var inst = {:};
inst.a = 1;
inst.b = {:};
inst.b.c = 3;

var binst = {:};
binst.a = 3;
binst.b = {:};
binst.b.c = 0;

var res = inst meets binst;
print(res);
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenADeepAndBClassMismatch_ShouldReturnTrue()
        {
            testEngine.Run(@"
var inst = {:};
inst.a = 1;
inst.b = {:};
inst.b.c = 3;

class B { }

var res = inst meets B;
print(res);
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenADeepAndBClassMatch_ShouldReturnTrue()
        {
            testEngine.Run(@"
var inst = {:};
inst.a = 1;
inst.b = {:};
inst.b.c = 3;

class B 
{
    var a,b;
}

var res = inst meets B;
print(res);
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenAJsonAndBClassMatch_ShouldReturnTrue()
        {
            testEngine.Run(@"
var jsonString = ""{ \""a\"": 1.0,  \""b\"": 2.0,  \""c\"": 3.0 }"";
var inst = Serialise.FromJson(jsonString);

class B 
{
    var a,b,c;
}

var res = inst meets B;
print(res);
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenAJsonAndBClassEmptyMatch_ShouldReturnTrue()
        {
            testEngine.Run(@"
var jsonString = ""{ \""a\"": 1.0,  \""b\"": 2.0,  \""c\"": 3.0 }"";
var inst = Serialise.FromJson(jsonString);

class B 
{
}

var res = inst meets B;
print(res);
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenAJsonAndBClassMismatch_ShouldReturnFalse()
        {
            testEngine.Run(@"
var jsonString = ""{ \""a\"": 1.0,  \""b\"": 2.0,  \""c\"": 3.0 }"";
var inst = Serialise.FromJson(jsonString);

class B 
{
    var e,f,g;
}

var res = inst meets B;
print(res);
");

            Assert.AreEqual("False", testEngine.InterpreterResult);
        }

        [Test]
        public void Meets_WhenAfterSignInCompileChunk_ShouldNotThrow()
        {
            testEngine.Run(@"
class I 
{
    var a;
    Meth(){}
}
class Implementation
{
    signs I;
    var a;
    Meth() 
    {
        //does something cool.
    }
}

class NotMatchingImp
{
    var a;
}
var doesMatchFirst = NotMatchingImp meets I;

class UnrelatedMatchAndMore
{
    var a,b,c;
    Meth(){}
    Foo(){}
    Bar(){}
}

var doesMatchSecond = UnrelatedMatchAndMore meets I;

print(doesMatchFirst);
print(doesMatchSecond);
");

            Assert.AreEqual("FalseTrue", testEngine.InterpreterResult);
        }

    }
}
