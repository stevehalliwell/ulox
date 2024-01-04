using System.Linq;
using static ULox.Optimiser;

namespace ULox
{
    public sealed class OptimiserGotoLabelReorderPass : IOptimiserPass
    {
        private readonly OptimiserLabelUsageAccumulator _optimiserLabelUsageAccumulator = new OptimiserLabelUsageAccumulator();

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
            var retval = PassCompleteRequest.None;
            retval = ProcessZeroJumpSquash(optimiser, chunk, retval);

            if (retval != PassCompleteRequest.None)
                return retval;

            retval = ProcessSingleUsageLabels(optimiser, chunk, retval);

            return retval;
        }

        private PassCompleteRequest ProcessZeroJumpSquash(Optimiser optimiser, Chunk chunk, PassCompleteRequest retval)
        {
            var labelUsage = _optimiserLabelUsageAccumulator.LabelUsage;
            //any zero jumps just nuke them all and go next
            var zeroJumps = labelUsage
                .Where(x => x.opCode == OpCode.GOTO)
                .Where(x => chunk.Labels[x.label] == x.from)
                .ToArray();

            foreach (var (from, labelId, opCode, isWeaved) in zeroJumps)
            {
                optimiser.AddToRemove(chunk, from);
                if (labelUsage.Count(x => x.label == labelId) <= 1)
                {
                    chunk.RemoveLabel(labelId);
                }
                retval = PassCompleteRequest.Repeat;
            }

            return retval;
        }

        private PassCompleteRequest ProcessSingleUsageLabels(Optimiser optimiser, Chunk chunk, PassCompleteRequest retval)
        {
            var labelUsage = _optimiserLabelUsageAccumulator.LabelUsage;
            //find labels with a single use
            var singleUsageLabels = labelUsage
                .Where(x => x.opCode == OpCode.GOTO && x.isWeaved)
                .Where(x => labelUsage.Count(y => x.label == y.label) <= 1)
                .Select(x => (loc: x.from, labelId: x.label, indicies: IsolatedLabelBound(chunk, x.label)))
                .Where(x => x.indicies.startAt != -1)
                .ToArray();

            foreach (var (loc, labelId, indicies) in singleUsageLabels)
            {
                var spanLen = indicies.end - indicies.startAt + 1;
                var spanPackets = chunk.Instructions.GetRange(indicies.startAt, spanLen);
                chunk.InsertInstructionsAt(loc, spanPackets);
                optimiser.AddToRemove(chunk, loc + spanLen);
                var removedShift = indicies.startAt < loc ? 0 : spanLen;
                for (int i = 0; i < spanLen; i++)
                {
                    optimiser.AddToRemove(chunk, indicies.startAt + i + removedShift);
                }
                //that label ws the only way to get here and we are gone now
                chunk.RemoveLabel(labelId);
                return PassCompleteRequest.Repeat;
            }

            return retval;
        }

        private static (int startAt, int end) IsolatedLabelBound(Chunk chunk, byte labelId)
        {
            var startAt = chunk.GetLabelPosition(labelId);
            if(!IsIndexWeaved(chunk, startAt))
                return (-1, -1);

            var endAt = -1;
            for (int i = startAt; i < chunk.Instructions.Count; i++)
            {
                var op = chunk.Instructions[i].OpCode;
                if (op == OpCode.GOTO
                    || op == OpCode.RETURN)
                {
                    endAt = i;
                    break;
                }

                if (op == OpCode.GOTO_IF_FALSE
                    || op == OpCode.TEST)
                {
                    return (-1, -1);
                }
            }

            //ensure there are no labels between us
            if (chunk.Labels.Any(x => x.Value >= startAt && x.Value < endAt))
                return (-1, -1);

            return (startAt, endAt);
        }
    }
}
