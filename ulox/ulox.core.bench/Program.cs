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
    }
}
