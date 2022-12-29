namespace ULox
{
    public abstract class CompiledScriptIterator
    {
        public int CurrentInstructionIndex { get; private set; }
        public Chunk CurrentChunk { get; private set; }

        public void Iterate(CompiledScript compiledScript)
        {
            Iterate(compiledScript, compiledScript.TopLevelChunk);

            foreach (var c in compiledScript.AllChunks)
            {
                if (compiledScript.TopLevelChunk == c) continue;
                Iterate(compiledScript, c);
            }
        }

        public void Iterate(CompiledScript compiledScript, Chunk chunk)
        {
            PreChunkInterate(compiledScript, chunk);

            ChunkIterate(chunk);

            PostChunkIterate(compiledScript, chunk);
        }

        private void ChunkIterate(Chunk chunk)
        {
            CurrentChunk = chunk;
            for (CurrentInstructionIndex = 0; CurrentInstructionIndex < chunk.Instructions.Count; CurrentInstructionIndex++)
            {
                var packet = chunk.Instructions[CurrentInstructionIndex];

                CurrentInstructionIndex++;
                ProcessPacket(packet);
            }
        }

        protected abstract void PostChunkIterate(CompiledScript compiledScript, Chunk chunk);
        protected abstract void PreChunkInterate(CompiledScript compiledScript, Chunk chunk);
        protected abstract void ProcessPacket(ByteCodePacket packet);
    }
}
