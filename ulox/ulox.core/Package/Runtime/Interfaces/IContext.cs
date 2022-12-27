using System;
using System.Collections.Generic;

namespace ULox
{
    public interface IContext
    {
        IVm VM { get; }
        IProgram Program { get; }
        IScriptLocator ScriptLocator { get; }

        event Action<string> OnLog;

        void AddLibrary(IULoxLibrary lib);
        CompiledScript CompileScript(Script script);
        
    }
}
