using System.Collections.Generic;

namespace ULox
{
    public class Context : IContext
    {
        private Dictionary<string, IULoxLibrary> _libraries = new Dictionary<string, IULoxLibrary>();
        private List<CompiledScript> _compiledChunks = new List<CompiledScript>();

        public Context(
            IProgram program,
            IVm vm)
        {
            Program = program;
            VM = vm;
        }

        public IProgram Program { get; private set; }
        public IVm VM { get; private set; }
        public IEnumerable<string> LibraryNames => _libraries.Keys;

        public void DeclareLibrary(IULoxLibrary lib)
        {
            _libraries[lib.Name] = lib;
        }

        public void BindLibrary(string name)
        {
            if (!_libraries.TryGetValue(name, out var lib))
                throw new VMException($"No library of name '{name}' found.");

            var toAdd = lib.GetBindings();

            foreach (var item in toAdd)
            {
                VM.SetGlobal(item.Key, item.Value);
            }
        }

        public void AddLibrary(IULoxLibrary lib)
        {
            DeclareLibrary(lib);
            BindLibrary(lib.Name);
        }

        public CompiledScript CompileScript(string script)
        {
            var res = Program.Compile(script);
            _compiledChunks.Add(res);
            return res;
        }
    }
}
