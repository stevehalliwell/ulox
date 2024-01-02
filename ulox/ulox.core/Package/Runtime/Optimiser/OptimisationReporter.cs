using System.Collections.Generic;
using System.Text;

namespace ULox
{
    public sealed class OptimisationReporter
    {
        public sealed class CompiledScriptStatistics : CompiledScriptIterator
        {
            public sealed class ChunkStatistics
            {
                public int LabelCount;
                public int InstructionCount;
                public int[] OpCodeOccurances = new int[OpCodeUtil.NumberOfOpCodes];
            }

            public Dictionary<Chunk, ChunkStatistics> ChunkLookUp = new Dictionary<Chunk, ChunkStatistics>();
            private ChunkStatistics _current;

            protected override void PreChunkInterate(CompiledScript compiledScript, Chunk chunk)
            {
                _current = new ChunkStatistics();
                ChunkLookUp.Add(chunk, _current);
            }

            protected override void PostChunkIterate(CompiledScript compiledScript, Chunk chunk)
            {
                _current.InstructionCount = chunk.Instructions.Count;
                _current.LabelCount = chunk.Labels.Count;
            }

            protected override void ProcessPacket(ByteCodePacket packet)
            {
                _current.OpCodeOccurances[(byte)packet.OpCode]++;
            }
        }

        private CompiledScriptStatistics _pre = new CompiledScriptStatistics();
        private CompiledScriptStatistics _post = new CompiledScriptStatistics();
        
        public void PreOptimise(CompiledScript compiledScript)
        {
            _pre.Iterate(compiledScript);
        }

        public void PostOptimise(CompiledScript compiledScript)
        {
            _post.Iterate(compiledScript);
        }

        public OptimisationReport GetReport()
        {
            return OptimisationReport.Create(_pre, _post);
        }
    }

    public sealed class OptimisationReport
    {
        public sealed class ChunkOptimisationReport
        {
            public string Name;
            public int InstructionCountBefore;
            public int InstructionCountAfter;
            public int LabelCountBefore;
            public int LabelCountAfter;
            public int[] OpCodeOccurancesBefore = new int[OpCodeUtil.NumberOfOpCodes];
            public int[] OpCodeOccurancesAfter = new int[OpCodeUtil.NumberOfOpCodes];
        }

        private readonly List<ChunkOptimisationReport> _chunkOptimisationReports = new List<ChunkOptimisationReport>();

        public IReadOnlyList<ChunkOptimisationReport> ChunkOptimisationReports => _chunkOptimisationReports;

        public string GenerateStringReport()
        {
            var sb = new StringBuilder();
            foreach (var item in ChunkOptimisationReports)
            {
                sb.AppendLine($"Chunk: {item.Name}");
                sb.AppendLine($"  Instructions: {item.InstructionCountBefore} -> {item.InstructionCountAfter}");
                sb.AppendLine($"  Labels: {item.LabelCountBefore} -> {item.LabelCountAfter}");
                var hasHeader = false;
                for (int i = 0; i < item.OpCodeOccurancesBefore.Length; i++)
                {
                    if (item.OpCodeOccurancesBefore[i] != item.OpCodeOccurancesAfter[i])
                    {
                        if (!hasHeader)
                        {
                            sb.AppendLine($"  OpCode Occurances:");
                            hasHeader = true;
                        }
                        sb.AppendLine($"    {(OpCode)i}: {item.OpCodeOccurancesBefore[i]} -> {item.OpCodeOccurancesAfter[i]}");
                    }
                }
            }

            return sb.ToString();
        }

        public static OptimisationReport Create(OptimisationReporter.CompiledScriptStatistics pre, OptimisationReporter.CompiledScriptStatistics post)
        {
            var report = new OptimisationReport();
            foreach (var item in post.ChunkLookUp)
            {
                var preChunk = pre.ChunkLookUp[item.Key];
                var chunkReport = new ChunkOptimisationReport()
                {
                    Name = item.Key.FullName,
                    InstructionCountBefore = preChunk.InstructionCount,
                    InstructionCountAfter = item.Value.InstructionCount,
                    LabelCountBefore = preChunk.LabelCount,
                    LabelCountAfter = item.Value.LabelCount,
                };
                for (int i = 0; i < preChunk.OpCodeOccurances.Length; i++)
                {
                    chunkReport.OpCodeOccurancesBefore[i] = preChunk.OpCodeOccurances[i];
                    chunkReport.OpCodeOccurancesAfter[i] = item.Value.OpCodeOccurances[i];
                }
                report._chunkOptimisationReports.Add(chunkReport);
            }
            return report;
        }
    }
}
