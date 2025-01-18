using System.Collections.Generic;
using System.Linq;
using static ULox.ByteCodePacket;
using static ULox.Optimiser;

namespace ULox
{
    public sealed class OptimiserCollapseDuplicateLabelsPass : IOptimiserPass
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
            var allOptimisableLabels = chunk.Labels
                .Where(x => !chunk.IsInternalLabel(x.Key))
                .OrderBy(x => x.Value)
                .ToArray(); //NOTE: this to array is going to show up in profiler. But it's slower without it.

            var len = allOptimisableLabels.Length;
            var outterLen = len - 1;
            for (int i = 0; i < outterLen; i++)
            {
                var label = allOptimisableLabels[i];
                var labelValue = label.Value;
                //find the last label at this location
                int j = i;
                for (; j < outterLen; j++)
                {
                    if (allOptimisableLabels[j + 1].Value != labelValue)
                    {
                        break;
                    }
                }

                if (j == i)
                    continue;

                //add a new label that is the combined name of all the labels at this location
                //PERF: would love to use span, but we don't have access to it...
                var labelsAtLoc = allOptimisableLabels.Skip(i).Take(j - i + 1).ToArray();
                //perf: not a great way to do this
                var labelsThatStillExist = labelsAtLoc
                    .Where(x => chunk.Labels.ContainsKey(x.Key))
                    .ToArray();
                if (!labelsThatStillExist.Any())
                    continue;

                var newLabelName = string.Join("_", labelsAtLoc
                    .Select(x => chunk.LabelNames[x.Key].String));

                var newLabelId = chunk.AddLabel(new HashedString(newLabelName), labelValue);

                //remove all the old labels
                foreach (var labelToRemove in labelsAtLoc)
                {
                    chunk.RemoveLabel(labelToRemove.Key);
                    //redirect all the jumps to the new label
                    RedirectLabels(
                        _optimiserLabelUsageAccumulator.LabelUsage,
                        chunk,
                        labelToRemove.Key,
                        newLabelId);
                }
            }

            return PassCompleteRequest.None;
        }

        public static void RedirectLabels(
            IReadOnlyList<(int from, Label label, OpCode opCode, bool isWeaved)> labelUsage,
            Chunk chunk,
            Label from,
            Label to)
        {
            var labelUsageToRedirect = labelUsage.Where(x => x.label == from);
            foreach (var (fromInst, label, opCode, isWeaved) in labelUsageToRedirect)
            {
                var packet = chunk.Instructions[fromInst];
                switch (packet.OpCode)
                {
                case OpCode.GOTO:
                    chunk.Instructions[fromInst] = new ByteCodePacket(OpCode.GOTO, new LabelDetails(to));
                    break;
                case OpCode.GOTO_IF_FALSE:
                    chunk.Instructions[fromInst] = new ByteCodePacket(OpCode.GOTO_IF_FALSE, new LabelDetails(to));
                    break;
                case OpCode.TEST:
                    if (packet.testOpDetails.TestOpType == TestOpType.TestSetBodyLabel
                        || packet.testOpDetails.TestOpType == TestOpType.TestCase)
                    {
                        var type = packet.testOpDetails.TestOpType;
                        chunk.Instructions[fromInst] = new ByteCodePacket(OpCode.TEST, new ByteCodePacket.TestOpDetails(type, label));
                    }
                    break;
                }
            }
        }
    }
}
