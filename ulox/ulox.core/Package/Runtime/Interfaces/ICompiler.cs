using System.Collections.Generic;

namespace ULox
{
    public interface ICompiler
    {
        Chunk Compile(List<Token> inTokens);
        void Reset();
    }
}