using System.Collections.Generic;

namespace ULox
{
    public interface ICompiler
    {
        Chunk Compile(TokenIterator tokenIter);
        void Reset();
    }
}