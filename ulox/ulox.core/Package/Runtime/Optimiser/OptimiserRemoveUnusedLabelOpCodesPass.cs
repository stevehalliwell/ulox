using System.Linq;
using static ULox.Optimiser;

namespace ULox
{
    public sealed class OptimiserRemoveUnusedLabelOpCodesPass : IOptimiserPass
    {
        private readonly OptimiserLabelUsageAccumulator _optimiserLabelUsageAccumulator = new OptimiserLabelUsageAccumulator();

        public void Prepare(Optimiser optimiser, Chunk chunk)
        {
            _optimiserLabelUsageAccumulator.Clear();
        }

        public void ProcessPacket(Optimiser optimiser, Chunk chunk, int inst, ByteCodePacket packet)
        {
            _optimiserLabelUsageAccumulator.ProcessPacket(inst, packet);
        }

        public PassCompleteRequest Complete(Optimiser optimiser, Chunk chunk)
        {
            foreach (var label in chunk.Labels.ToList())
            {
                if (!_optimiserLabelUsageAccumulator.LabelUsage.Any(x => x.label == label.Key))
                    chunk.RemoveLabel(label.Key);
            }

            return PassCompleteRequest.None;
        }
    }
}
