using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Context : IContext
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DeclareLibrary(IULoxLibrary lib)
        {
            _libraries[lib.Name] = lib;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddLibrary(IULoxLibrary lib)
        {
            DeclareLibrary(lib);
            BindLibrary(lib.Name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CompiledScript CompileScript(Script script)
        {
            var res = Program.Compile(script);
            if(!_compiledChunks.Contains(res))
                _compiledChunks.Add(res);
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(string x)
            => OnLog?.Invoke(x);
    }
}
