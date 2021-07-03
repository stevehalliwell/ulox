namespace ULox
{
    public class ByteCodeInterpreterEngine
    {
        public Program Program { get; private set; } = new Program();
        private readonly VM _vm = new VM();
        public VM VM => _vm;

        public string Disassembly => Program.Disassembly;

        public virtual void Run(string testString)
        {
            var chunk = Program.Compile(testString);
            _vm.Interpret(chunk.TopLevelChunk);
        }

        public virtual void Execute(Program program)
        {
            Program = program;
            _vm.Run(Program);
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
