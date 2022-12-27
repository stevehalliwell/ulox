using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Context
    {
        private readonly List<CompiledScript> _compiledChunks = new List<CompiledScript>();

        public Context(
            IScriptLocator scriptLocator,
            Program program,
            Vm vm)
        {
            ScriptLocator = scriptLocator;
            Program = program;
            Vm = vm;
        }

        public IScriptLocator ScriptLocator { get; private set; }
        public Program Program { get; private set; }
        public Vm Vm { get; private set; }
        public event Action<string> OnLog;

        public void AddLibrary(IULoxLibrary lib)
        {
            var toAdd = lib.GetBindings();

            foreach (var item in toAdd)
            {
                Vm.SetGlobal(item.Key, item.Value);
            }
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
