using System.Collections.Generic;

namespace ULox
{
    public sealed class CompiledScript
    {
        public Chunk TopLevelChunk { get; }
        public int ScriptHash { get; }
        public List<Chunk> AllChunks { get; }

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
