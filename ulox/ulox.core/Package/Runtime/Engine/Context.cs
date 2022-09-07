using System;
using System.Collections.Generic;

namespace ULox
{
    public class Context : IContext
    {
        private readonly Dictionary<string, IULoxLibrary> _libraries = new Dictionary<string, IULoxLibrary>();
        private readonly List<CompiledScript> _compiledChunks = new List<CompiledScript>();

        public Context(
            IScriptLocator scriptLocator,
            IProgram program,
            IVm vm)
        {
            ScriptLocator = scriptLocator;
            Program = program;
            VM = vm;
        }

        public IScriptLocator ScriptLocator { get; private set; }
        public IProgram Program { get; private set; }
        public IVm VM { get; private set; }
        public IEnumerable<string> LibraryNames => _libraries.Keys;

        public event Action<string> OnLog;

        public void DeclareLibrary(IULoxLibrary lib)
        {
            _libraries[lib.Name] = lib;
        }

        public void BindLibrary(string name)
        {
            if (!_libraries.TryGetValue(name, out var lib))
                VM.ThrowRuntimeException($"No library of name '{name}' found.");

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

        public void Log(string x)
            => OnLog?.Invoke(x);
    }
}
