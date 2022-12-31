using NUnit.Framework;

namespace ulox.core.tests
{
    [TestFixture]
    public class DITests : EngineTestBase
    {
        [Test]
        public void Engine_Register_Unused()
        {
            testEngine.Run(@"
register Seven 7;"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Inject_Error()
        {
            testEngine.Run(@"
var s = inject Seven;"
            );

            StringAssert.StartsWith("Inject failure. Nothing has been registered (yet) with name 'Seven' at ip:'1' in chunk:'unnamed_chunk(test:2)'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_RegisterAndInject()
        {
            testEngine.Run(@"
register Seven 7;
var s = inject Seven;
print(s);");

            Assert.AreEqual("7", testEngine.InterpreterResult);
        }

        [Test]
        public void DI_FreezeBeforeTest_ShouldPass()
        {
            testEngine.Run(@"
register Seven 7;
DI.Freeze();

test T
{
    test A
    {
        var s = inject Seven;
        expect s == 7;
    }
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Inject_Assign_ShouldFail()
        {
            testEngine.Run(@"
register Seven 7;
inject Seven = 8;");

            StringAssert.StartsWith("Invalid assignment target", testEngine.InterpreterResult);
        }

    }
}
