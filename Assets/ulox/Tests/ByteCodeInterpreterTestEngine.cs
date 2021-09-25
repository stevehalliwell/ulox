namespace ULox.Tests
{
    public class ByteCodeInterpreterTestEngine
    {
        private readonly System.Action<string> _logger;
        private readonly Vm _vm;
        private readonly Engine _engine;

        public Vm Vm => _vm;
        public IProgram Program => _engine.Context.Program;
        public Engine Engine => _engine;

        public ByteCodeInterpreterTestEngine(System.Action<string> logger)
        {
            _vm = new Vm();
            _engine = new Engine(new ScriptLocator(),new Context(new Program(),_vm));

            _logger = logger;
            _engine.Context.AddLibrary(new CoreLibrary(x => 
            {
                _logger(x);
                AppendResult(x);
            }));
        }

        public void AddLibrary(IULoxLibrary lib)
        {
            _engine.Context.AddLibrary(lib);
        }

        protected void AppendResult(string str) => InterpreterResult += str;
        public string InterpreterResult { get; private set; } = string.Empty;

        public void Run(string testString)
        {
            try
            {
                _engine.RunScript(testString);
            }
            catch (LoxException e)
            {
                AppendResult(e.Message);
            }
            finally
            {
                _logger(_vm.TestRunner.GenerateDump());
                _logger(InterpreterResult);
                _logger(_engine.Context.Program.Disassembly);
                _logger(_engine.Context.VM.GenerateGlobalsDump());
            }
        }

        internal void Execute(IProgram program)
        {
            _engine.Context.VM.Run(program);
        }
    }
}