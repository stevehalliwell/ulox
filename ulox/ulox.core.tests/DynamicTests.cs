using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class DynamicTests : EngineTestBase
    {
        [Test]
        public void Fields_WhenAddedToDynamic_ShouldSucceed()
        {
            testEngine.Run(@"
var obj = {=};

obj.a = 1;
obj.b = 2;
obj.c = 3;
obj.d = -1;

var d = obj.a + obj.b + obj.c;
obj.d = d;

print(obj.d);
");

            Assert.AreEqual("6", testEngine.InterpreterResult);
        }

        [Test]
        public void Field_WhenSingleDynamicInline_ShouldSucceed()
        {
            testEngine.Run(@"
var obj = {a=1,};
print(obj.a);
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Fields_WhenAddedToDynamicInline_ShouldSucceed()
        {
            testEngine.Run(@"
var obj = {a=1, b=2, c=3, d=-1,};

var d = obj.a + obj.b + obj.c;
obj.d = d;

print(obj.d);
");

            Assert.AreEqual("6", testEngine.InterpreterResult);
        }

        [Test]
        public void Dynamic_WhenCreated_ShouldPrintInstType()
        {
            testEngine.Run(@"
var obj = {=};

print(obj);
");

            Assert.AreEqual("<inst Dynamic>", testEngine.InterpreterResult);
        }

        [Test]
        public void Dynamic_WhenInlineNested_ShouldPrint()
        {
            testEngine.Run(@"
var obj = {a=1, b={innerA=2,}, c=3,};

print(obj.a);
print(obj.b);
print(obj.b.innerA);
print(obj.c);
");

            Assert.AreEqual("1<inst Dynamic>23", testEngine.InterpreterResult);
        }

        [Test]
        public void Dynamic_WhenInvalid_ShouldFail()
        {
            testEngine.Run(@"
var obj = {7};
");

            StringAssert.StartsWith("Expect identifier or '=' after '{'", testEngine.InterpreterResult);
        }

        [Test]
        public void Dyanic_RemoveFieldWhenReadOnly_ShouldError()
        {
            testEngine.Run(@"
var expected = false;
var result = 0;
var obj = {=};
obj.a = 7;
readonly obj;

obj.RemoveField(obj, ""a"");");

            StringAssert.StartsWith("Cannot remove field from read only", testEngine.InterpreterResult);
        }
    }
}