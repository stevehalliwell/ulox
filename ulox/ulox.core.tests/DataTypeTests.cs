using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class DataTypeTests : EngineTestBase
    {
        [Test]
        public void ClassInstanceFields_WhenAccessed_ShouldHaveInitialValues()
        {
            testEngine.Run(@"
class Foo { var A = true, review, taste = ""Full""}
var b = Foo();
print (b.A);
print (b.review);
print (b.taste);");

            Assert.AreEqual("TruenullFull", testEngine.InterpreterResult);
        }

        [Test]
        public void ClassMixin_WhenCreated_ShouldHaveValues()
        {
            testEngine.Run(@"
class Foo {var A = true, review, taste = ""Full""}
class Bar {
    mixin Foo; 
var 
    B = 1;
}
var b = Bar();
print (b.A);
print (b.review);
print (b.taste);
print (b.B);");

            Assert.AreEqual("TruenullFull1", testEngine.InterpreterResult);
        }

        [Test]
        public void Delcared_WhenTrailingCommaInVarList_ShouldCompile()
        {
            testEngine.Run(@"
class Foo {var a,b,c,}
print (Foo);");

            Assert.AreEqual("<Class Foo>", testEngine.InterpreterResult);
        }
    }
}
