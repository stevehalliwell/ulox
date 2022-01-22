using System;
using System.Collections.Generic;

namespace ULox
{
    public interface IContext
    {
        IVm VM { get; }
        IEnumerable<string> LibraryNames { get; }
        IProgram Program { get; }
        IScriptLocator ScriptLocator { get; }

        event Action<string> OnLog;

        void AddLibrary(IULoxLibrary lib);

        void BindLibrary(string name);

        CompiledScript CompileScript(string script);

        void DeclareLibrary(IULoxLibrary lib);
    }
}
