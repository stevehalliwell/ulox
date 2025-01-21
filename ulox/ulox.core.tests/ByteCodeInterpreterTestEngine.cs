using System;
using System.Linq;
using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class ByteCodeInterpreterTestEngine
    {
        public Engine MyEngine { get; private set; }

        public bool ReThrow { get; set; }

        public ByteCodeInterpreterTestEngine()
        {
            var platform = new GenericPlatform<DirectoryLimitedPlatform, LogIOPlatform>(new(new(Environment.CurrentDirectory), ("temp:", new(TestContext.CurrentContext.TestDirectory))), new(AppendResult));
            MyEngine = new Engine(new Context(new Program(), new Vm(), platform));
            MyEngine.Context.Vm.Tracing = new VmTracingReporter();
        }

        public string InterpreterResult { get; private set; } = string.Empty;

        public string JoinedCompilerMessages =>
            string.Join(
                Environment.NewLine, 
                MyEngine.Context.Program.CompiledScripts.SelectMany(x => x.CompilerMessages));

        public void Run(string testString)
        {
            Run(new Script("test", testString));
        } 
        
        public void Run(Script script)
        {
            try
            {
                MyEngine.RunScript(script);
            }
            catch (UloxException e)
            {
                AppendResult(e.Message);
                if (ReThrow)
                    throw;
            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                Console.WriteLine(MyEngine.Context.Vm.TestRunner.GenerateDump());
                Console.WriteLine(InterpreterResult);
                Console.WriteLine(MyEngine.Context.Program.Disassembly);
                Console.WriteLine(JoinedCompilerMessages);
                Console.WriteLine(VmUtil.GenerateGlobalsDump(MyEngine.Context.Vm));
                Console.WriteLine(VmUtil.GenerateValueStackDump(MyEngine.Context.Vm));
                Console.WriteLine(VmStatisticsReport.Create(MyEngine.Context.Vm.Tracing.PerChunkStats).GenerateStringReport());
                Console.WriteLine(MyEngine.Context.Program.Optimiser.OptimisationReporter?.GetReport().GenerateStringReport() ?? string.Empty);
            }
        }

        private void AppendResult(string str) => InterpreterResult += str;
    }
}