using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ULox;

namespace ulox.core.bench
{
    [MemoryDiagnoser]
    public class Program
    {
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
            engine.RunScript(new Script("", CompileVsExecute.Script));
        }

        [Benchmark]
        public void CompileVsExecute_CompileOnly()
        {
            var engine = Engine.CreateDefault();
            engine.Context.CompileScript(new Script("", CompileVsExecute.Script));
        }

        [Benchmark]
        public void CompileVsExecute_TokenizeOnly()
        {
            var scanner = new Scanner();
            var res = scanner.Scan(new Script("", CompileVsExecute.Script));
        }

        [Benchmark]
        public void Looping_While()
        {
            var engine = Engine.CreateDefault();
            engine.RunScript(new Script("", Looping.While));
        }

        [Benchmark]
        public void Looping_For()
        {
            var engine = Engine.CreateDefault();
            engine.RunScript(new Script("", Looping.For));
        }

        [Benchmark]
        public void Looping_Loop()
        {
            var engine = Engine.CreateDefault();
            engine.RunScript(new Script("", Looping.Loop));
        }

        [Benchmark]
        public void Conditional_If()
        {
            var engine = Engine.CreateDefault();
            engine.RunScript(new Script("", Conditional.If));
        }

        [Benchmark]
        public void Conditional_Match()
        {
            var engine = Engine.CreateDefault();
            engine.RunScript(new Script("", Conditional.Match));
        }
    }
}
