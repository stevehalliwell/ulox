﻿using System;
using System.Collections.Generic;

namespace ULox
{
    public sealed class CompiledScript
    {
        public Chunk TopLevelChunk { get; }
        public int ScriptHash { get; }
        public List<Chunk> AllChunks { get; }
        public List<CompilerMessage> CompilerMessages = new();

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

        public CompiledScript DeepClone()
        {
            var newTopLevel = TopLevelChunk.DeepClone();
            var newAllChunks = new List<Chunk>();
            foreach (var chunk in AllChunks)
            {
                newAllChunks.Add(chunk.DeepClone());
            }

            return new CompiledScript(newTopLevel, ScriptHash, newAllChunks, CompilerMessages);
        }
    }
}
