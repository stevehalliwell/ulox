using System.Collections.Generic;
using System.Text;

namespace ULox
{
    public sealed class VmStatisticsReporter
    {
        public sealed class ChunkStatistics
        {
            public int[] OpCodeOccurances = new int[OpCodeUtil.NumberOfOpCodes];
        }

        private readonly Dictionary<Chunk, ChunkStatistics> _chunkLookUp = new Dictionary<Chunk, ChunkStatistics>();

        public void ProcessingOpCode(Chunk chunk, OpCode opCode)
        {
            if (!_chunkLookUp.TryGetValue(chunk, out var stats))
            {
                stats = new ChunkStatistics();
                _chunkLookUp.Add(chunk, stats);
            }

            stats.OpCodeOccurances[(byte)opCode]++;
        }

        public VmStatisticsReport GetReport()
        {
            return VmStatisticsReport.Create(_chunkLookUp);
        }
    }

    public sealed class VmStatisticsReport
    {
        public sealed class ChunkStatistics 
        {
            public string name;
            public int[] OpCodeOccurances = new int[OpCodeUtil.NumberOfOpCodes];
        }

        private readonly List<ChunkStatistics> _chunkStatistics = new List<ChunkStatistics>();

        public IReadOnlyList<ChunkStatistics> ChunksStats => _chunkStatistics;

        public static VmStatisticsReport Create(IReadOnlyDictionary<Chunk, VmStatisticsReporter.ChunkStatistics> chunkLookUp)
        {
            var report = new VmStatisticsReport();

            foreach (var item in chunkLookUp)
            {
                var chunkStats = new ChunkStatistics()
                {
                    name = item.Key.Name,
                    OpCodeOccurances = item.Value.OpCodeOccurances,
                };
                report._chunkStatistics.Add(chunkStats);
            }

            return report;
        }

        public string GenerateStringReport()
        {
            var sb = new StringBuilder();

            foreach (var chunk in ChunksStats)
            {
                sb.AppendLine($"Chunk: {chunk.name}");
                var occurances = chunk.OpCodeOccurances;
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
