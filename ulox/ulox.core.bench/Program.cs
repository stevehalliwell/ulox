using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ULox;

namespace ulox.core.bench
{
    [MemoryDiagnoser]
    public class Program
    {
        private CompiledScript _whileCompiled;
        private CompiledScript _loopCompiled;
        private CompiledScript _forCompiled;
        private CompiledScript _ifCompiled;
        private CompiledScript _matchCompiled;
        private CompiledScript _scriptCompiled;

        [GlobalSetup]
        public void Setup()
        {
            var engine = Engine.CreateDefault();
            _whileCompiled = engine.Context.CompileScript(BenchmarkScripts.While);
            _loopCompiled = engine.Context.CompileScript(BenchmarkScripts.Loop);
            _forCompiled = engine.Context.CompileScript(BenchmarkScripts.For);
            _ifCompiled = engine.Context.CompileScript(BenchmarkScripts.If);
            _matchCompiled = engine.Context.CompileScript(BenchmarkScripts.Match);
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
        public System.Collections.Generic.List<Token> CompileVsExecute_TokenizeOnly()
        {
            var scanner = new Scanner();
            return scanner.Scan(CompileVsExecute.Script);
        }

        [Benchmark]
        public void Looping_While()
        {
            var engine = Engine.CreateDefault();
            engine.Context.VM.Interpret(_whileCompiled.TopLevelChunk);
        }

        [Benchmark]
        public void Looping_For()
        {
            var engine = Engine.CreateDefault();
            engine.Context.VM.Interpret(_forCompiled.TopLevelChunk);
        }

        [Benchmark]
        public void Looping_Loop()
        {
            var engine = Engine.CreateDefault();
            engine.Context.VM.Interpret(_loopCompiled.TopLevelChunk);
        }

        [Benchmark]
        public void Conditional_If()
        {
            var engine = Engine.CreateDefault();
            engine.Context.VM.Interpret(_ifCompiled.TopLevelChunk);
        }

        [Benchmark]
        public void Conditional_Match()
        {
            var engine = Engine.CreateDefault();
            engine.Context.VM.Interpret(_matchCompiled.TopLevelChunk);
        }

        [Benchmark]
        public string Dissasm_Script()
        {
            var dis = new Disassembler();
            dis.Iterate(_scriptCompiled);
            return dis.GetString();
        }

        [Benchmark]
        public string Dissasm_While()
        {
            var dis = new Disassembler();
            dis.Iterate(_whileCompiled);
            return dis.GetString();
        }

        [Benchmark]
        public void Optimise_Script()
        {
            var opt = new ByteCodeOptimiser();
            opt.Optimise(_scriptCompiled);
        }

        [Benchmark]
        public void Optimise_While()
        {
            var opt = new ByteCodeOptimiser();
            opt.Optimise(_whileCompiled);
        }
    }
}
