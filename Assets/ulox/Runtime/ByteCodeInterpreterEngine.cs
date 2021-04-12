namespace ULox
{
    public class ByteCodeInterpreterEngine
    {
        private Scanner _scanner;
        private Compiler _compiler;
        private VM _vm;
        private Disassembler _disassembler;

        public ByteCodeInterpreterEngine()
        {
            _scanner = new Scanner();
            _compiler = new Compiler();
            _disassembler = new Disassembler();
            _vm = new VM();
        }

        public string StackDump => _vm.GenerateStackDump();
        public string Disassembly => _disassembler.GetString();
        public VM VM => _vm;

        public virtual void Run(string testString)
        {
            _scanner.Reset();
            _compiler.Reset();

            var tokens = _scanner.Scan(testString);
            var chunk = _compiler.Compile(tokens);
            _disassembler.DoChunk(chunk);
            _vm.Interpret(chunk);
        }

        public virtual void AddLibrary(ILoxByteCodeLibrary lib)
        {
            lib.BindToEngine(this);
        }
    }
}
