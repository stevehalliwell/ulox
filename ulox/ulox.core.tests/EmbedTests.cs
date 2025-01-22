using System;
using NUnit.Framework;

namespace ULox.Core.Tests
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
            vm.Globals.Get(new HashedString("Setup"), out var setupMeth);
            vm.PushCallFrameAndRun(setupMeth,0);
            vm.Globals.Get(new HashedString("Update"), out var updateMeth);
            vm.Push(Value.New(0.5));
            vm.PushCallFrameAndRun(updateMeth,1);
            vm.Push(Value.New(0.5));
            vm.PushCallFrameAndRun(updateMeth,1);

            Assert.IsFalse(setupMeth.IsFalsey());

            Assert.AreEqual("1", testEngine.InterpreterResult);
            vm.Globals.Get(new HashedString("runningTime"), out var running); 
            Assert.AreEqual(1, running.val.asDouble);
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
            vm.Globals.Get(new HashedString("Setup"), out var setupMeth);
            vm.Globals.Get(new HashedString("Update"), out var updateMeth);
            var platform = new GenericPlatform<DirectoryLimitedPlatform, ConsolePrintPlatform>(new(new(Environment.CurrentDirectory)), new());
            var newEngine = new Engine(new Context(new Program(), new Vm(), platform));
            var newVm = newEngine.Context.Vm;
            newVm.CopyFrom(vm);
            newVm.PushCallFrameAndRun(setupMeth, 0);
            newVm.Push(Value.New(0.5));
            newVm.PushCallFrameAndRun(updateMeth, 1);
            newVm.Push(Value.New(0.5));
            newVm.PushCallFrameAndRun(updateMeth, 1);

            Assert.AreEqual("1", testEngine.InterpreterResult);
            newVm.Globals.Get(new HashedString("runningTime"), out var running);
            Assert.AreEqual(1, running.val.asDouble);
        }
    }
}