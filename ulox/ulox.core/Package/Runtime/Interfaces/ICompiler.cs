using System.Collections.Generic;

namespace ULox
{
    public interface ICompiler
    {
        CompiledScript Compile(List<Token> tokens, Script script);
        void Reset();
    }
}