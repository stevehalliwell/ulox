using BenchmarkDotNet.Attributes;

namespace ULox.Core.Bench
{
    [MemoryDiagnoser]
    [BenchmarkCategory("ALL", "LITE", "MODERATE")]
    public class CompileExecuteSplit
    {
        private Engine _engine;
        private CompiledScript _compiled;

        [IterationSetup]
        public void Setup()
        {
            _engine = Program.CreateEngine();
            _compiled = _engine.Context.Program.Compile(Vec2Variants.Tuple);
            _engine.Context.Vm.PrepareTypes(_engine.Context.Program.TypeInfo);
        }

        [Benchmark]
        public InterpreterResult Execute()
        {
            return _engine.Context.Vm.Interpret(_compiled.TopLevelChunk);
        }
    }
}
