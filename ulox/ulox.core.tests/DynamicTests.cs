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

        [Test]
        public void DyanicProperty_GetExists_ShouldMatch()
        {
            testEngine.Run(@"
var foo = { a = 1 };
var res = foo[""a""];
print(res);");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void DyanicProperty_GetDoesNotExists_ShouldError()
        {
            testEngine.Run(@"
var foo = { a = 1 };
var res = foo[""b""];
print(res);");

            StringAssert.StartsWith("No field of name 'b' could be found on instance", testEngine.InterpreterResult);
        }

        [Test]
        public void DyanicProperty_GetInvalidType_ShouldError()
        {
            testEngine.Run(@"
var foo = 7;
var res = foo[""b""];
print(res);");

            StringAssert.StartsWith("Cannot perform get index on type 'Double'", testEngine.InterpreterResult);
        }

        [Test]
        public void DyanicProperty_SetExists_ShouldMatch()
        {
            testEngine.Run(@"
var foo = { a = 1 };
foo[""a""] = 2;
print(foo.a);");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void DyanicProperty_SetDoesNotExists_ShouldError()
        {
            testEngine.Run(@"
var foo = { a = 1 };
foo[""b""] = 2;
print(foo.a);");

            StringAssert.StartsWith("Attempted to create a new entry 'b' via Set.", testEngine.InterpreterResult);
        }

        [Test]
        public void DyanicProperty_SetInvalidType_ShouldError()
        {
            testEngine.Run(@"
var foo = 7;
foo[""b""] = 2;
print(foo.a);");

            StringAssert.StartsWith("Cannot perform set index on type 'Double'", testEngine.InterpreterResult);
        }
    }
}