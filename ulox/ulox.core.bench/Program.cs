using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace ULox.Core.Bench
{
    [MemoryDiagnoser]
    public class Program
    {
        private CompiledScript _loopCompiled;
        private CompiledScript _ifCompiled;
        private CompiledScript _scriptCompiled;

        [GlobalSetup]
        public void Setup()
        {
            var engine = Engine.CreateDefault();
            _loopCompiled = engine.Context.CompileScript(BenchmarkScripts.Loop);
            _ifCompiled = engine.Context.CompileScript(BenchmarkScripts.If);
            _scriptCompiled = engine.Context.CompileScript(CompileVsExecute.Script);
        }

        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Program>(args: args);
        }

        [Benchmark]
        public void ScriptVsNativeFunctional_UloxMethods()
        {
            var engine = Engine.CreateDefault();
            //todo need to look at the byteocode for this, see if we can speed it up
            engine.RunScript(new Script("", ScriptVsNativeFunctional.FunctionalUlox));
        }

        [Benchmark]
        public void ScriptVsNativeFunctional_NativeMethods()
        {
            var engine = Engine.CreateDefault();
            engine.RunScript(new Script("", ScriptVsNativeFunctional.FunctionalNative));
        }

        [Benchmark]
        public void CompileVsExecute_All()
        {
            var engine = Engine.CreateDefault();
            engine.RunScript(CompileVsExecute.Script);
        }

        [Benchmark]
        public void CompileVsExecute_CompileOnly()
        {
            var engine = Engine.CreateDefault();
            engine.Context.CompileScript(CompileVsExecute.Script);
        }

        [Benchmark]
        public void Looping_Loop()
        {
            var engine = Engine.CreateDefault();
            engine.Context.Vm.Interpret(_loopCompiled.TopLevelChunk);
        }

        [Benchmark]
        public void Conditional_If()
        {
            var engine = Engine.CreateDefault();
            engine.Context.Vm.Interpret(_ifCompiled.TopLevelChunk);
        }

        [Benchmark]
        public string Dissasm_Script()
        {
            var dis = new Disassembler();
            dis.Iterate(_scriptCompiled);
            return dis.GetString();
        }
    }
}
