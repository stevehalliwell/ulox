using System.Collections.Generic;

namespace ULox
{
    public sealed class CompiledScript
    {
        public Chunk TopLevelChunk { get; }
        public int ScriptHash { get; }
        public List<Chunk> AllChunks { get; }
        public List<CompilerMessage> CompilerMessages = new List<CompilerMessage>();

        public CompiledScript(
            Chunk topLevelChunk,
            int scriptHash,
            List<Chunk> allChunks,
            List<CompilerMessage> compilerMessages)
        {
            TopLevelChunk = topLevelChunk;
            ScriptHash = scriptHash;
            AllChunks = allChunks;
            CompilerMessages = compilerMessages;
        }
    }
}
