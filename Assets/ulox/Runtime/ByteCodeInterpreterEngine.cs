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

        public Chunk LastChunk { get; private set; }

        public virtual void Run(string testString)
        {
            _scanner.Reset();
            _compiler.Reset();

            var tokens = _scanner.Scan(testString);
            LastChunk = _compiler.Compile(tokens);
            _disassembler.DoChunk(LastChunk);
            _vm.Interpret(LastChunk);
        }

        public virtual void AddLibrary(ILoxByteCodeLibrary lib)
        {
            lib.BindToEngine(this);
        }
    }
}
