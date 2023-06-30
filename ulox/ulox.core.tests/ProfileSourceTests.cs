using NUnit.Framework;
using ULox.Core.Bench;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class ProfileSourceTests : EngineTestBase
    {
        [Test]
        public void Run_BouncingBallProfileScript()
        {
            testEngine.Run(BouncingBallProfileScript.Script);

            try
            {
                testEngine.MyEngine.Context.Vm.Globals.Get(new HashedString("SetupGame"), out var setup);
                testEngine.MyEngine.Context.Vm.PushCallFrameAndRun(setup, 0);
                testEngine.MyEngine.Context.Vm.Globals.Get(new HashedString("Update"), out var update);
                testEngine.MyEngine.Context.Vm.PushCallFrameAndRun(update, 0);
            }
            catch (System.Exception)
            {
            }

            Assert.AreEqual("Setting Up Game200Updated", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WaterLineProfileScript()
        {
            testEngine.Run(WaterLineProfileScript.Script);

            testEngine.MyEngine.Context.Vm.Globals.Get(new HashedString("SetupGame"), out var setup);
            testEngine.MyEngine.Context.Vm.PushCallFrameAndRun(setup, 0);
            testEngine.MyEngine.Context.Vm.Globals.Get(new HashedString("Update"), out var update);
            testEngine.MyEngine.Context.Vm.PushCallFrameAndRun(update, 0);

            Assert.AreEqual("Setting Up GameUpdated", testEngine.InterpreterResult);
        }
    }
}
