using System.Collections.Generic;

namespace ULox
{
    public interface IProgram
    {
        List<CompiledScript> CompiledScripts { get; }
        string Disassembly { get; }

        CompiledScript Compile(string script);
    }
}