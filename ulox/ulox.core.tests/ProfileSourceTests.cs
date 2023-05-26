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

            testEngine.MyEngine.Context.Vm.Globals.Get(new HashedString("SetupGame").Hash, out var setup);
            testEngine.MyEngine.Context.Vm.PushCallFrameAndRun(setup, 0);
            testEngine.MyEngine.Context.Vm.Globals.Get(new HashedString("Update").Hash, out var update);
            testEngine.MyEngine.Context.Vm.PushCallFrameAndRun(update, 0);

            Assert.AreEqual("Setting Up GameUpdating", testEngine.InterpreterResult);
        }
    }
}
