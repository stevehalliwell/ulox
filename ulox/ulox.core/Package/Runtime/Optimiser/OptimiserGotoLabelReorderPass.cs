using System.Linq;
using static ULox.Optimiser;

namespace ULox
{
    public sealed class OptimiserGotoLabelReorderPass : IOptimiserPass
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

            foreach (var (from, label, opCode, isWeaved) in labelUsage)
            {
                if (opCode == OpCode.GOTO && chunk.Labels[label] == from)
                {
                    optimiser.AddToRemove(chunk, from);
                    retval = PassCompleteRequest.Repeat;
                }
            }

            return retval;
        }

        private PassCompleteRequest ProcessSingleUsageLabels(Optimiser optimiser, Chunk chunk, PassCompleteRequest retval)
        {
            var labelUsage = _optimiserLabelUsageAccumulator.LabelUsage;

            foreach (var (from, label, opCode, isWeaved) in labelUsage)
            {
                if (opCode == OpCode.GOTO
                    && isWeaved)
                {
                    if (labelUsage.Count(x => x.label == label) != 1)
                        continue;

                    var (startAt, end) = IsolatedLabelBound(chunk, label);
                    if (startAt != -1)
                    {
                        var spanLen = end - startAt + 1;
                        var spanPackets = chunk.Instructions.GetRange(startAt, spanLen);
                        chunk.InsertInstructionsAt(from, spanPackets);
                        optimiser.AddToRemove(chunk, from + spanLen);
                        var removedShift = startAt < from ? 0 : spanLen;
                        for (int i = 0; i < spanLen; i++)
                        {
                            optimiser.AddToRemove(chunk, startAt + i + removedShift);
                        }
                        //that label ws the only way to get here and we are gone now
                        chunk.RemoveLabel(label);
                        return PassCompleteRequest.Repeat;
                    }

                }
            }

            return retval;
        }

        private static (int startAt, int end) IsolatedLabelBound(Chunk chunk, Label labelId)
        {
            var startAt = chunk.GetLabelPosition(labelId);
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
