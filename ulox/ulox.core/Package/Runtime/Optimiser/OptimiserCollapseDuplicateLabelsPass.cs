using System.Collections.Generic;
using System.Linq;
using static ULox.Optimiser;

namespace ULox
{
    public sealed class OptimiserCollapseDuplicateLabelsPass : IOptimiserPass
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
            var allLabelLocs = chunk.Labels.Select(x => x.Value).OrderBy(x => x).ToArray();

            foreach (var label in chunk.Labels.ToArray())
            {
                var labelsAtLoc = chunk.Labels.Where(x => x.Value == label.Value).ToArray();

                if (labelsAtLoc.Length > 1)
                {
                    //add a new label that is the combined name of all the labels at this location
                    var newLabelName = string.Join("_", labelsAtLoc
                        .Select(x => chunk.Constants[x.Key].val.asString.String));

                    var newLabelId = chunk.AddConstant(Value.New(newLabelName));
                    chunk.AddLabel(newLabelId, label.Value);

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
            }

            return PassCompleteRequest.None;
        }

        public static void RedirectLabels(
            IReadOnlyList<(int from, byte label)> labelUsage,
            Chunk chunk,
            byte from,
            byte to)
        {
            var labelUsageToRedirect = labelUsage.Where(x => x.label == from);
            foreach (var (fromInst, label) in labelUsageToRedirect)
            {
                var packet = chunk.Instructions[fromInst];
                switch (packet.OpCode)
                {
                case OpCode.GOTO:
                    chunk.Instructions[fromInst] = new ByteCodePacket(OpCode.GOTO, to);
                    break;
                case OpCode.GOTO_IF_FALSE:
                    chunk.Instructions[fromInst] = new ByteCodePacket(OpCode.GOTO_IF_FALSE, to);
                    break;
                case OpCode.TEST:
                    if (packet.testOpDetails.TestOpType == TestOpType.TestFixtureBodyInstruction
                        || packet.testOpDetails.TestOpType == TestOpType.TestCase)
                    {
                        var type = packet.testOpDetails.TestOpType;
                        var b2 = packet.testOpDetails.b2;
                        chunk.Instructions[fromInst] = new ByteCodePacket(OpCode.TEST, new ByteCodePacket.TestOpDetails(type, to, b2));
                    }
                    break;
                }
            }
        }
    }
}
