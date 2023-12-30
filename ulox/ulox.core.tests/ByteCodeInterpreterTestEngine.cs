using System;

namespace ULox.Core.Tests
{
    public class ByteCodeInterpreterTestEngine
    {
        private readonly Action<string> _logger;
        public Engine MyEngine { get; private set; }

        public bool ReThrow { get; set; }

        public ByteCodeInterpreterTestEngine(Action<string> logger)
        {
            _logger = logger;
            MyEngine = Engine.CreateDefault();
            MyEngine.Context.OnLog += logger;
            MyEngine.Context.OnLog += AppendResult;
        }

        public string InterpreterResult { get; private set; } = string.Empty;

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
                _logger(MyEngine.Context.Vm.TestRunner.GenerateDump());
                _logger(InterpreterResult);
                _logger(MyEngine.Context.Program.Disassembly);
                _logger(VmUtil.GenerateGlobalsDump(MyEngine.Context.Vm));
                _logger(VmUtil.GenerateValueStackDump(MyEngine.Context.Vm));
                _logger(VmUtil.GenerateReturnDump(MyEngine.Context.Vm));
            }
        }

        private void AppendResult(string str) => InterpreterResult += str;
    }
}