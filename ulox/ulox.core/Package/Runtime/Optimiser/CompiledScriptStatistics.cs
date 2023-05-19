using System.Collections.Generic;

namespace ULox
{
    public sealed class CompiledScriptStatistics : CompiledScriptIterator
    {
        public sealed class ChunkStatistics
        {
            public int InstructionCount { get; set; }
            public int[] OpCodeOccurances = new int[byte.MaxValue];
        }

        public Dictionary<Chunk, ChunkStatistics> ChunkLookUp = new Dictionary<Chunk, ChunkStatistics>();
        private ChunkStatistics _current;

        protected override void PostChunkIterate(CompiledScript compiledScript, Chunk chunk)
        {
            _current.InstructionCount = chunk.Instructions.Count;
        }

        protected override void PreChunkInterate(CompiledScript compiledScript, Chunk chunk)
        {
            _current = new ChunkStatistics();
            ChunkLookUp.Add(chunk, _current);
        }

        protected override void ProcessPacket(ByteCodePacket packet)
        {
            _current.OpCodeOccurances[(byte)packet.OpCode]++;
        }
    }
}
