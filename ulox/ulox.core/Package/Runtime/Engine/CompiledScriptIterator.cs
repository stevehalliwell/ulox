namespace ULox
{
    //TODO: collapse these two classes and change inher to composition
    public abstract class ChunkIterator
    {
        public int CurrentInstructionIndex { get; protected set; }
        public Chunk CurrentChunk { get; protected set; }

        protected abstract void ProcessPacket(ByteCodePacket packet);

        protected void ChunkIterate(Chunk chunk)
        {
            CurrentChunk = chunk;
            for (CurrentInstructionIndex = 0; CurrentInstructionIndex < chunk.Instructions.Count; CurrentInstructionIndex++)
            {
                var packet = chunk.Instructions[CurrentInstructionIndex];

                ProcessPacket(packet);
            }
        }
    }

    public abstract class CompiledScriptIterator : ChunkIterator
    {
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

        protected abstract void PostChunkIterate(CompiledScript compiledScript, Chunk chunk);
        protected abstract void PreChunkInterate(CompiledScript compiledScript, Chunk chunk);
    }
}
