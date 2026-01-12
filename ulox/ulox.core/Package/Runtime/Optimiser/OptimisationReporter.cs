using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ULox
{
    public sealed class OptimisationReporter
    {
        public sealed class CompiledScriptStatistics
        {
            public sealed class ChunkStatistics
            {
                public int LabelCount;
                public int InstructionCount;
                public int[] OpCodeOccurrences = new int[OpCodeUtil.NumberOfOpCodes];
                public int Constants;
            }

            public Dictionary<Chunk, ChunkStatistics> ChunkLookUp = new();
            private ChunkStatistics _current;

            internal void Iterate(CompiledScript compiledScript)
            {
                foreach (var chunk in compiledScript.AllChunks)
                {
                    _current = new ChunkStatistics();
                    ChunkLookUp.Add(chunk, _current);
                    foreach (var packet in chunk.Instructions)
                    {
                        _current.OpCodeOccurrences[(byte)packet.OpCode]++;
                    }
                    _current.InstructionCount = chunk.Instructions.Count;
                    _current.LabelCount = chunk.Labels.Count;
                    _current.Constants = chunk.Constants.Count;
                }
            }
        }

        private CompiledScriptStatistics _pre = new();
        private CompiledScriptStatistics _post = new();
        
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
            public int[] OpCodeOccurrencesBefore = new int[OpCodeUtil.NumberOfOpCodes];
            public int[] OpCodeOccurrencesAfter = new int[OpCodeUtil.NumberOfOpCodes];
            public int ConstantsBefore;
            public int ConstantsAfter;
        }

        private readonly List<ChunkOptimisationReport> _chunkOptimisationReports = new();

        public IReadOnlyList<ChunkOptimisationReport> ChunkOptimisationReports => _chunkOptimisationReports;

        public string GenerateStringReport()
        {
            var sb = new StringBuilder();
            foreach (var item in ChunkOptimisationReports)
            {
                sb.AppendLine($"Chunk: {item.Name}");
                sb.AppendLine($"  Instructions: {item.InstructionCountBefore} -> {item.InstructionCountAfter}");
                sb.AppendLine($"  Constants: {item.ConstantsBefore} -> {item.ConstantsAfter}");
                sb.AppendLine($"  Labels: {item.LabelCountBefore} -> {item.LabelCountAfter}");
                var hasHeader = false;
                for (int i = 0; i < item.OpCodeOccurrencesBefore.Length; i++)
                {
                    if (item.OpCodeOccurrencesBefore[i] != item.OpCodeOccurrencesAfter[i])
                    {
                        if (!hasHeader)
                        {
                            sb.AppendLine($"  OpCode Occurrences:");
                            hasHeader = true;
                        }
                        sb.AppendLine($"    {(OpCode)i}: {item.OpCodeOccurrencesBefore[i]} -> {item.OpCodeOccurrencesAfter[i]}");
                    }
                }
            }

            //and a quick summary
            sb.AppendLine("Summary:");
            var totalInstructionsBefore = ChunkOptimisationReports.Sum(c => c.InstructionCountBefore);
            var totalInstructionsAfter = ChunkOptimisationReports.Sum(c => c.InstructionCountAfter);
            sb.AppendLine($"  Total Instructions: {totalInstructionsBefore} -> {totalInstructionsAfter}");
            var totalConstantsBefore = ChunkOptimisationReports.Sum(c => c.ConstantsBefore);
            var totalConstantsAfter = ChunkOptimisationReports.Sum(c => c.ConstantsAfter);
            sb.AppendLine($"  Total Constants: {totalConstantsBefore} -> {totalConstantsAfter}");
            var totalLabelsBefore = ChunkOptimisationReports.Sum(c => c.LabelCountBefore);
            var totalLabelsAfter = ChunkOptimisationReports.Sum(c => c.LabelCountAfter);
            sb.AppendLine($"  Total Labels: {totalLabelsBefore} -> {totalLabelsAfter}");


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
                    ConstantsBefore = preChunk.Constants,
                    ConstantsAfter = item.Value.Constants,
                };
                for (int i = 0; i < preChunk.OpCodeOccurrences.Length; i++)
                {
                    chunkReport.OpCodeOccurrencesBefore[i] = preChunk.OpCodeOccurrences[i];
                    chunkReport.OpCodeOccurrencesAfter[i] = item.Value.OpCodeOccurrences[i];
                }
                report._chunkOptimisationReports.Add(chunkReport);
            }
            return report;
        }
    }
}
