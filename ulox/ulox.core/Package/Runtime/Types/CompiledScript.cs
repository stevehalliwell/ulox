using System.Collections.Generic;

namespace ULox
{
    public sealed class CompiledScript
    {
        public Chunk TopLevelChunk { get; private set; }
        public int ScriptHash { get; private set; }
        public List<Chunk> AllChunks { get; private set; }

        public CompiledScript(
            Chunk topLevelChunk,
            int scriptHash,
            List<Chunk> allChunks)
        {
            TopLevelChunk = topLevelChunk;
            ScriptHash = scriptHash;
            AllChunks = allChunks;
        }
    }
}
