using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ULox
{
    public interface IULoxLibrary
    {
        Table GetBindings();
    }

    public sealed class Context
    {
        private readonly List<CompiledScript> _compiledScripts = new();
        public IReadOnlyList<CompiledScript> CompiledScripts => _compiledScripts;

        public Context(
            Program program,
            Vm vm,
            IPlatform platform)
        {
            Program = program;
            Vm = vm;
            Platform = platform;
        }

        public Program Program { get; }
        public Vm Vm { get; }
        public IPlatform Platform { get; }

        public void AddLibrary(IULoxLibrary lib)
        {
            var toAdd = lib.GetBindings();

            Vm.Globals.CopyFrom(toAdd);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CompiledScript CompileScript(Script script)
        {
            var existing = _compiledScripts.Find(x => x.ScriptHash == script.ScriptHash);
            if (existing != null)
                return existing;

            var res = Program.Compile(script);
            _compiledScripts.Add(res);
            Vm.PrepareTypes(Program.TypeInfo);
            Vm.Clear();
            Vm.Interpret(res.TopLevelChunk);
            return res;
        }
    }
}
