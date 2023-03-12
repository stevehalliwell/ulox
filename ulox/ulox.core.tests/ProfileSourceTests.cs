using NUnit.Framework;
using ULox.Core.Bench;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class ProfileSourceTests : EngineTestBase
    {
        [Test]
        public void Profile()
        {
            testEngine.Run(BouncingBallProfileScript.Script);

            testEngine.MyEngine.Context.Vm.PushCallFrameAndRun(testEngine.MyEngine.Context.Vm.GetGlobal(new HashedString("SetupGame")), 0);
            testEngine.MyEngine.Context.Vm.PushCallFrameAndRun(testEngine.MyEngine.Context.Vm.GetGlobal(new HashedString("Update")), 0);

            Assert.AreEqual("Setting Up GameUpdating", testEngine.InterpreterResult);
        }
    }
}
