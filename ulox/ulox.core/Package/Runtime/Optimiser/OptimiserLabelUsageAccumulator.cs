using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class OptimiserLabelUsageAccumulator
    {
        private readonly List<(int from, Label label, OpCode opCode, bool isWeaved)> _labelUsage = new();
        public IReadOnlyList<(int from, Label label, OpCode opCode, bool isWeaved)> LabelUsage => _labelUsage;

        public void Clear()
        {
            _labelUsage.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddLabelUsage(Chunk chunk, int inst, Label labelId)
        {
            _labelUsage.Add((inst, labelId, chunk.Instructions[inst].OpCode, Optimiser.IsIndexWeaved(chunk, inst)));
        }

        public void ProcessPacket(Chunk chunk, int currentInstructionIndex, ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.TEST:
                if (packet.testOpDetails.TestOpType == TestOpType.TestSetBodyLabel
                    || packet.testOpDetails.TestOpType == TestOpType.TestCase)
                    AddLabelUsage(chunk, currentInstructionIndex, packet.testOpDetails.LabelId);
                break;
            case OpCode.GOTO:
            case OpCode.GOTO_IF_FALSE:
                AddLabelUsage(chunk, currentInstructionIndex, packet.labelDetails.LabelId);
                break;
            }
        }
    }
}
