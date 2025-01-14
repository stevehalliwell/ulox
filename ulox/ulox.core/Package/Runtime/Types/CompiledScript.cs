using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public sealed class CompiledScript
    {
        public Chunk TopLevelChunk => AllChunks.Last();
        public readonly int ScriptHash;
        public readonly List<Chunk> AllChunks = new();
        public readonly List<CompilerMessage> CompilerMessages = new();

        public CompiledScript(
            int scriptHash,
            List<Chunk> allChunks,
            List<CompilerMessage> compilerMessages)
            : this (scriptHash)
        {
            AllChunks = allChunks;
            CompilerMessages = compilerMessages;
        }

        public CompiledScript(int scriptHash)
        {
            ScriptHash = scriptHash;
        }

        public CompiledScript DeepClone()
        {
            var newTopLevel = TopLevelChunk.DeepClone();
            var newAllChunks = new List<Chunk>();
            foreach (var chunk in AllChunks)
            {
                newAllChunks.Add(chunk.DeepClone());
            }

            return new CompiledScript(ScriptHash, newAllChunks, CompilerMessages);
        }
    }
}
