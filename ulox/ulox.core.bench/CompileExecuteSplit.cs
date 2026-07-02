using BenchmarkDotNet.Attributes;

namespace ULox.Core.Bench
{
    [MemoryDiagnoser]
    public class CompileExecuteSplit
    {
        private Engine _engine;
        private CompiledScript _compiled;

        [IterationSetup]
        public void Setup()
        {
            _engine = Program.CreateEngine();
            _compiled = _engine.Context.Program.Compile(Vec2Variants.Type);
            _engine.Context.Vm.PrepareTypes(_engine.Context.Program.TypeInfo);
        }

        [Benchmark]
        public InterpreterResult Execute()
        {
            return _engine.Context.Vm.Interpret(_compiled.TopLevelChunk);
        }
    }
}
