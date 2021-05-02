namespace ULox
{
    public class ByteCodeInterpreterEngine
    {
        private Program _program = new Program();
        private VM _vm = new VM();
        public VM VM => _vm;
        public Program Program => _program;

        public string Disassembly => _program.Disassembly;

        public virtual void Run(string testString)
        {
            var chunk = _program.Compile(testString);
            _vm.Interpret(chunk.TopLevelChunk);
        }

        public virtual void Execute(Program program)
        {
            _program = program;
            _vm.Run(_program);
        }

        public virtual void AddLibrary(ILoxByteCodeLibrary lib)
        {
            var toAdd = lib.GetBindings();

            foreach (var item in toAdd)
            {
                _vm.SetGlobal(item.Key, item.Value);
            }
        }
    }
}
