using System.Linq;

namespace ULox
{
    public sealed class OptimiserRemoveUnusedLabelOpCodesPass : CompiledScriptIterator, IOptimiserPass
    {
        private Optimiser _optimiser;
        private readonly OptimiserLabelUsageAccumulator _optimiserLabelUsageAccumulator = new OptimiserLabelUsageAccumulator();

        public void Run(Optimiser optimiser, CompiledScript compiledScript)
        {
            _optimiser = optimiser;
            Iterate(compiledScript);
            ProcessUnusedLabels(compiledScript);
        }

        private void ProcessUnusedLabels(CompiledScript compiledScript)
        {
            foreach (var chunk in compiledScript.AllChunks)
            {
                foreach (var label in chunk.Labels.ToList())
                {
                    if (!_optimiserLabelUsageAccumulator.LabelUsage.Any(x => x.label == label.Key))
                        chunk.RemoveLabel(label.Key);
                }
            }
        }

        protected override void PreChunkInterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        protected override void ProcessPacket(ByteCodePacket packet)
        {
            _optimiserLabelUsageAccumulator.ProcessPacket(CurrentChunk, CurrentInstructionIndex, packet);
        }

        protected override void PostChunkIterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        public void Reset()
        {
            _optimiserLabelUsageAccumulator.Clear();
        }
    }
}
