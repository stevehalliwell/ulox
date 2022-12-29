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
                var opCode = (OpCode)chunk.Instructions[CurrentInstructionIndex];

                CurrentInstructionIndex++;
                var b1 = chunk.Instructions[CurrentInstructionIndex];
                CurrentInstructionIndex++;
                var b2 = chunk.Instructions[CurrentInstructionIndex];
                CurrentInstructionIndex++;
                var b3 = chunk.Instructions[CurrentInstructionIndex];
                ProcessPacket(new ByteCodePacket(opCode, b1, b2, b3));
            }
        }

        protected abstract void PostChunkIterate(CompiledScript compiledScript, Chunk chunk);
        protected abstract void PreChunkInterate(CompiledScript compiledScript, Chunk chunk);
        protected abstract void ProcessPacket(ByteCodePacket packet);
    }
}
