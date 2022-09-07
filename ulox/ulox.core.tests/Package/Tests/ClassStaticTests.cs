using NUnit.Framework;

namespace ULox.Tests
{
    public class ClassStaticTests : EngineTestBase
    {
        [Test]
        public void Engine_Class_StaticFields()
        {
            testEngine.Run(@"
class T
{
    static var a = 2;
}
print(T.a);");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Class_StaticFields_WhenClassModified_ShouldThrow()
        {
            testEngine.Run(@"
class T
{
    static var a = 2;
}

T.b = 5;");

            Assert.AreEqual("Attempted to Create a new field 'b' via SetField on a frozen object.", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NoThis_Method_WorksAsStatic()
        {
            testEngine.Run(@"
class T
{
    NoMemberMethod()
    {
        return 7;
    }
}

print(T.NoMemberMethod());");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Static_Method_OnClass()
        {
            testEngine.Run(@"
class T
{
    static StaticMethod()
    {
        return 7;
    }
}

print(T.StaticMethod());");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Static_Method_OnInstance()
        {
            testEngine.Run(@"
class T
{
    static StaticMethod()
    {
        return 7;
    }
}

print(T().StaticMethod());");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }
    }
}
