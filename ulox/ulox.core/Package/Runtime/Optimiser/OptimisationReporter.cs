using System.Text;

namespace ULox
{
    public sealed class OptimisationReporter
    {
        public CompiledScriptStatistics Pre = new CompiledScriptStatistics();
        public CompiledScriptStatistics Post = new CompiledScriptStatistics();
        
        public void PreOptimise(CompiledScript compiledScript)
        {
            Pre.Iterate(compiledScript);
        }

        public void PostOptimise(CompiledScript compiledScript)
        {
            Post.Iterate(compiledScript);
        }

        public string GetReport()
        {
            var sb = new StringBuilder();
            foreach (var item in Post.ChunkLookUp)
            {
                var pre = Pre.ChunkLookUp[item.Key];
                sb.AppendLine($"Chunk: {item.Key.Name}");
                sb.AppendLine($"  Instructions: {pre.InstructionCount} -> {item.Value.InstructionCount}");
                var hasHeader = false;
                for (int i = 0; i < pre.OpCodeOccurances.Length; i++)
                {
                    if (pre.OpCodeOccurances[i] != item.Value.OpCodeOccurances[i])
                    {
                        if(!hasHeader)
                        {
                            sb.AppendLine($"  OpCode Occurances:");
                            hasHeader = true;
                        }
                        sb.AppendLine($"    {(OpCode)i}: {pre.OpCodeOccurances[i]} -> {item.Value.OpCodeOccurances[i]}");
                    }
                }
            }

            return sb.ToString();
        }
    }
}
