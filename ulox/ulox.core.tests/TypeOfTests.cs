using NUnit.Framework;

namespace ulox.core.tests
{
    public class TypeOfTests : EngineTestBase
    {
        [Test]
        public void TypeOf_WhenCalledOn5_ShouldReturnNumber()
        {
            testEngine.Run(@"
var t = typeof(5);
print(t);
");

            Assert.AreEqual("<Native Number>", testEngine.InterpreterResult);
        }

        [Test]
        public void TypeOf_WhenCalledOnTrue_ShouldReturnBool()
        {
            testEngine.Run(@"
var t = typeof(true);
print(t);
");

            Assert.AreEqual("<Native Bool>", testEngine.InterpreterResult);
        }

        [Test]
        public void TypeOf_WhenCalledOnString_ShouldReturnString()
        {
            testEngine.Run(@"
var t = typeof(""string"");
print(t);
");

            Assert.AreEqual("<Native String>", testEngine.InterpreterResult);
        }

        [Test]
        public void TypeOf_WhenCalledOnNull_ShouldReturnNull()
        {
            testEngine.Run(@"
var t = typeof(null);
print(t);
");

            Assert.AreEqual("null", testEngine.InterpreterResult);
        }

        [Test]
        public void TypeOf_WhenCalledOnList_ShouldReturnList()
        {
            testEngine.Run(@"
var t = typeof([]);
print(t);
");

            Assert.AreEqual("<Native NativeList>", testEngine.InterpreterResult);
        }

        [Test]
        public void TypeOf_WhenCalledOnMap_ShouldReturnMap()
        {
            testEngine.Run(@"
var t = typeof([:]);
print(t);
");

            Assert.AreEqual("<Native NativeMap>", testEngine.InterpreterResult);
        }

        [Test]
        public void TypeOf_WhenCalledOnDynamic_ShouldReturnDynamic()
        {
            testEngine.Run(@"
var t = typeof({:});
print(t);
");

            Assert.AreEqual("<Native Dynamic>", testEngine.InterpreterResult);
        }

        [Test]
        public void TypeOf_WhenCalledOnUserClass_ShouldReturnUserTypeName()
        {
            testEngine.Run(@"
class MyClass {}
var t = typeof(MyClass);
print(t);
");

            Assert.AreEqual("<Class MyClass>", testEngine.InterpreterResult);
        }

        [Test]
        public void TypeOf_WhenCalledOnUserType_ShouldReturnUserTypeName()
        {
            testEngine.Run(@"
class MyClass {}
var myClassInst = MyClass();
var t = typeof(myClassInst);
print(t);
");

            Assert.AreEqual("<Class MyClass>", testEngine.InterpreterResult);
        }

        [Test]
        public void TypeOf_WhenCalledUserTypeComparedToClass_ShouldReturnTrue()
        {
            testEngine.Run(@"
class MyClass {}
var myClassInst = MyClass();
print(typeof(myClassInst) == MyClass);
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void TypeOf_WhenCalledOnTypeOfResult_ShouldReturnTrue()
        {
            testEngine.Run(@"
class MyClass {}
var myClassInst = MyClass();
var t = typeof(myClassInst);
print(typeof(t));
");

            Assert.AreEqual("<Class MyClass>", testEngine.InterpreterResult);
        }

        [Test]
        public void TypeOf_WhenComparedOnSameType_ShouldReturnTrue()
        {
            testEngine.Run(@"
print(typeof(true) == typeof(false));
");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }
    }
}