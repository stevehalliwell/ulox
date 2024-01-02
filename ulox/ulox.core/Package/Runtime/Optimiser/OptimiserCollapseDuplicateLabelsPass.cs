using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public sealed class OptimiserCollapseDuplicateLabelsPass : CompiledScriptIterator, IOptimiserPass
    {
        private Optimiser _optimiser;
        private readonly OptimiserLabelUsageAccumulator _optimiserLabelUsageAccumulator = new OptimiserLabelUsageAccumulator();

        public void Run(Optimiser optimiser, CompiledScript compiledScript)
        {
            _optimiser = optimiser;
            Iterate(compiledScript);
            CollapseDuplicates(compiledScript);
        }

        private void CollapseDuplicates(CompiledScript compiledScript)
        {
            foreach(var chunk in compiledScript.AllChunks)
            {
                var allLabelLocs = chunk.Labels.Select(x => x.Value).OrderBy(x => x).ToArray();

                foreach (var label in chunk.Labels.ToArray())
                {
                    var labelsAtLoc = chunk.Labels.Where(x => x.Value == label.Value).ToArray();

                    if(labelsAtLoc.Length > 1)
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
        }

        public static void RedirectLabels(
            IReadOnlyList<(Chunk chunk, int from, byte label)> labelUsage,
            Chunk chunkOfConcern,
            byte from,
            byte to)
        {
            var labelUsageToRedirect = labelUsage.Where(x => x.chunk == chunkOfConcern && x.label == from);
            foreach (var (chunk, fromInst, label) in labelUsageToRedirect)
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
