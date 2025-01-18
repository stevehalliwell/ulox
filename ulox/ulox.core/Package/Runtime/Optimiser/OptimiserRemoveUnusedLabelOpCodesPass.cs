using System.Linq;
using static ULox.Optimiser;

namespace ULox
{
    public sealed class OptimiserRemoveUnusedLabelOpCodesPass : IOptimiserPass
    {
        private readonly OptimiserLabelUsageAccumulator _optimiserLabelUsageAccumulator = new();

        public void Prepare(Optimiser optimiser, Chunk chunk)
        {
            _optimiserLabelUsageAccumulator.Clear();
        }

        public void ProcessPacket(Optimiser optimiser, Chunk chunk, int inst, ByteCodePacket packet)
        {
            _optimiserLabelUsageAccumulator.ProcessPacket(chunk, inst, packet);
        }

        public PassCompleteRequest Complete(Optimiser optimiser, Chunk chunk)
        {
            var labelsToCheck = chunk.Labels.Keys.Where(x => !chunk.IsInternalLabel(x)).ToArray();

            foreach (var label in labelsToCheck)
            {
                if (!_optimiserLabelUsageAccumulator.LabelUsage.Any(x => x.label == label))
                    chunk.RemoveLabel(label);
            }

            return PassCompleteRequest.None;
        }
    }
}
