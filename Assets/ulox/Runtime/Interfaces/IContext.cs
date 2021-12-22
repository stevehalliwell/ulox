using System.Collections.Generic;

namespace ULox
{
    public interface IContext
    {
        IVm VM { get; }
        IEnumerable<string> LibraryNames { get; }
        IProgram Program { get; }

        void AddLibrary(IULoxLibrary lib);

        void BindLibrary(string name);

        CompiledScript CompileScript(string script);

        void DeclareLibrary(IULoxLibrary lib);
    }
}
