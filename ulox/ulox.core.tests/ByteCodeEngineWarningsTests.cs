using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class ByteCodeEngineWarningsTests : EngineTestBase
    {
        [Test]
        public void Argument_WhenUnused_ShouldHaveMessageUnused()
        {
            testEngine.Run(@"
fun UnusedLocals(a)
{
}
");

            StringAssert.Contains("Local 'a' is unused", testEngine.JoinedCompilerMessages);
        }

        [Test]
        public void Argument_WhenUsed_ShouldNotHaveMessageUnused()
        {
            testEngine.Run(@"
fun UsedLocals(a)
{
    a = 1;
}
");

            Assert.AreEqual("", testEngine.JoinedCompilerMessages);
        }

        [Test]
        public void Argument_WhenPassedToAnotherFunc_ShouldNotHaveMessageUnused()
        {
            testEngine.Run(@"
fun ForwardedLocal(a,b)
{
    b = 1;
    UsedLocals(a);
}

fun UsedLocals(a)
{
    a = 1;
}
");

            Assert.AreEqual("", testEngine.JoinedCompilerMessages);
        }

        [Test]
        public void Argument_WhenPassedToAnotherFuncInLoop_ShouldNotHaveMessageUnused()
        {
            testEngine.Run(@"
fun ForwardedLocal(a,b)
{
    b = 1;
    var c = [1,2,3];
    loop c
    {
        Something.UsedLocals(b, a);
    }
}
");

            Assert.AreEqual("", testEngine.JoinedCompilerMessages);
        }

        [Test]
        public void Argument_WhenPassedToAnotherFuncInLoopInMethod_ShouldNotHaveMessageUnused()
        {
            testEngine.Run(@"
fun ForwardedLocal(a,b)
{
    b = 1;
    var c = [1,2,3];
    loop c
    {
        Something.UsedLocals(b, a);
    }
}
");

            Assert.AreEqual("", testEngine.JoinedCompilerMessages);
        }
    }
}