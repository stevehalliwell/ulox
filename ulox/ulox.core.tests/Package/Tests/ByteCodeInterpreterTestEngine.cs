using System;

namespace ULox.Tests
{
    public class ByteCodeInterpreterTestEngine
    {
        private readonly Action<string> _logger;
        public IEngine MyEngine { get; private set; }

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
            try
            {
                MyEngine.RunScript(testString);
            }
            catch (PanicException) { throw; }
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
                _logger(MyEngine.Context.VM.TestRunner.GenerateDump());
                _logger(InterpreterResult);
                _logger(MyEngine.Context.Program.Disassembly);
                _logger(MyEngine.Context.VM.GenerateGlobalsDump());
                _logger(MyEngine.Context.VM.GenerateValueStackDump());
            }
        }

        private void AppendResult(string str) => InterpreterResult += str;
    }
}