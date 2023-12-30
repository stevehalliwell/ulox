using System.Collections.Generic;
using System.Text;

namespace ULox
{
    public sealed class VmStatistics
    {
        public sealed class ChunkStatistics
        {
            public int[] OpCodeOccurances = new int[byte.MaxValue];
        }

        public Dictionary<Chunk, ChunkStatistics> ChunkLookUp = new Dictionary<Chunk, ChunkStatistics>();

        public void ProcessingOpCode(Chunk chunk, OpCode opCode)
        {
            if (!ChunkLookUp.TryGetValue(chunk, out var stats))
            {
                stats = new ChunkStatistics();
                ChunkLookUp.Add(chunk, stats);
            }

            stats.OpCodeOccurances[(byte)opCode]++;
        }

        public string GetReport()
        {
            var sb = new StringBuilder();

            foreach (var chunk in ChunkLookUp)
            {
                sb.AppendLine($"Chunk: {chunk.Key.Name}");
                var occurances = chunk.Value.OpCodeOccurances;
                for (int i = 0; i < occurances.Length; i++)
                {
                    var occur = occurances[i];
                    if (occur == 0) continue;
                    sb.AppendLine($"  {(OpCode)i}   {occur}");
                }
            }

            return sb.ToString();
        }
    }
}
