using BenchmarkDotNet.Attributes;

namespace ULox.Core.Bench
{
    [MemoryDiagnoser]
    [BenchmarkCategory("ALL", "MODERATE")]
    public class RunScriptThenUpdateMany
    {
        private const int UpdateRepeat = 10000;

        [Benchmark]
        public void OptimisationsOff()
        {
            Run(engine => engine.Context.Program.Optimiser.Enabled = false);
        }

        [Benchmark]
        public void OptimisationsOn()
        {
            Run(engine => engine.Context.Program.Optimiser.Enabled = true);
        }

        private static void Run(System.Action<Engine> engineAction)
        {
            var engine = Program.CreateEngine();
            engineAction(engine);
            engine.RunScript(BenchmarkScripts.RunOnceThenUpdateMany);

            var vm = engine.Context.Vm;
            vm.Globals.Get(new HashedString("SetupGame"), out var setupGame);
            vm.Globals.Get(new HashedString("Update"), out var update);

            vm.PushCallFrameAndRun(setupGame, 0);
            for (var i = 0; i < UpdateRepeat; i++)
            {
                vm.Push(Value.New(0.016));
                vm.PushCallFrameAndRun(update, 1);
            }
        }
    }
}
