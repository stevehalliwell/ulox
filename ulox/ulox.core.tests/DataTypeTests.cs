using NUnit.Framework;

namespace ulox.core.tests
{
    public class DataTypeTests : EngineTestBase
    {
        [Test]
        public void Delcared_WhenAccessed_ShouldHaveDataObject()
        {
            testEngine.Run(@"
data Brioche {}
print (Brioche);");

            Assert.AreEqual("<Data Brioche>", testEngine.InterpreterResult);
        }

        [Test]
        public void DataInstance_WhenAccessed_ShouldHaveInstanceOfObject()
        {
            testEngine.Run(@"
data Brioche {}
var b = Brioche();
print (b);");

            Assert.AreEqual("<inst Brioche>", testEngine.InterpreterResult);
        }

        [Test]
        public void DataInstanceField_WhenAccessed_ShouldHaveDefaultValue()
        {
            testEngine.Run(@"
data Brioche {Butter}
var b = Brioche();
print (b.Butter);");

            Assert.AreEqual("null", testEngine.InterpreterResult);
        }

        [Test]
        public void DataInstanceField_WhenAccessed_ShouldHaveInitialiserValue()
        {
            testEngine.Run(@"
data Brioche {Butter = true}
var b = Brioche();
print (b.Butter);");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void DataInstanceFields_WhenAccessed_ShouldHaveInitialValues()
        {
            testEngine.Run(@"
data Brioche {Butter = true, review, taste = ""Full""}
var b = Brioche();
print (b.Butter);
print (b.review);
print (b.taste);");

            Assert.AreEqual("TruenullFull", testEngine.InterpreterResult);
        }
    }
}
