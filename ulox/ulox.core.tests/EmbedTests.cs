﻿using NUnit.Framework;

namespace ulox.core.tests
{
    public class EmbedTests : EngineTestBase
    {
        [Test]
        public void PushCallFrameAndRun_WhenSameContext_ShouldPass()
        {
            testEngine.Run(@"
fun Setup()
{
    var a = 1;
    print(a);
}

var runningTime = 0;

fun Update(dt)
{
    runningTime += dt;
}
");

            var vm = testEngine.MyEngine.Context.Vm;
            var setupMeth = vm.GetGlobal(new ULox.HashedString("Setup"));
            vm.PushCallFrameAndRun(setupMeth,0);
            var updateMeth = vm.GetGlobal(new ULox.HashedString("Update"));
            vm.Push(ULox.Value.New(0.5));
            vm.PushCallFrameAndRun(updateMeth,1);
            vm.Push(ULox.Value.New(0.5));
            vm.PushCallFrameAndRun(updateMeth,1);

            Assert.IsFalse(setupMeth.IsFalsey());

            Assert.AreEqual("1", testEngine.InterpreterResult);
            Assert.AreEqual(1, vm.GetGlobal(new ULox.HashedString("runningTime")).val.asDouble);
        }
        
        [Test]
        public void PushCallFrameAndRun_WhenDifferentContext_ShouldPass()
        {
            testEngine.Run(@"
fun Setup()
{
    var a = 1;
    print(a);
}

var runningTime = 0;

fun Update(dt)
{
    runningTime += dt;
}
");

            var vm = testEngine.MyEngine.Context.Vm;
            var setupMeth = vm.GetGlobal(new ULox.HashedString("Setup"));
            var updateMeth = vm.GetGlobal(new ULox.HashedString("Update"));
            var newEngine = ULox.Engine.CreateDefault();
            var newVm = newEngine.Context.Vm;
            newVm.CopyFrom(vm);
            newVm.PushCallFrameAndRun(setupMeth, 0);
            newVm.Push(ULox.Value.New(0.5));
            newVm.PushCallFrameAndRun(updateMeth, 1);
            newVm.Push(ULox.Value.New(0.5));
            newVm.PushCallFrameAndRun(updateMeth, 1);

            Assert.AreEqual("1", testEngine.InterpreterResult);
            Assert.AreEqual(1, newVm.GetGlobal(new ULox.HashedString("runningTime")).val.asDouble);
        }
    }
}
