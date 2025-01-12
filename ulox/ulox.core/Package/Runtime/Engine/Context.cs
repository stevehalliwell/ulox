using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public interface IULoxLibrary
    {
        Table GetBindings();
    }
    
    public interface IScriptLocator
    {
        Script Find(string name);
    }
    
    public sealed class Context
    {
        private readonly List<CompiledScript> _compiledScripts = new();

        public Context(
            IScriptLocator scriptLocator,
            Program program,
            Vm vm,
            IPlatform platform)
        {
            ScriptLocator = scriptLocator;
            Program = program;
            Vm = vm;
            Platform = platform;
        }

        public IScriptLocator ScriptLocator { get; }
        public Program Program { get; }
        public Vm Vm { get; }
        public IPlatform Platform { get; }

        public event Action<string> OnLog;

        public void AddLibrary(IULoxLibrary lib)
        {
            var toAdd = lib.GetBindings();

            Vm.Globals.CopyFrom(toAdd);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CompiledScript CompileScript(Script script, Action<CompiledScript> compiledScriptAction = null)
        {
            var res = Program.Compile(script);
            if(!_compiledScripts.Contains(res))
            { 
                _compiledScripts.Add(res);
                compiledScriptAction?.Invoke(res);
            }
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(string x)
            => OnLog?.Invoke(x);
    }
}
