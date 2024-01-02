using System.Collections.Generic;

namespace ULox
{
    public sealed class OptimiserLabelUsageAccumulator
    {
        private readonly List<(Chunk chunk, int from, byte label)> _labelUsage = new List<(Chunk, int, byte)>();
        public IReadOnlyList<(Chunk chunk, int from, byte label)> LabelUsage => _labelUsage;

        public void Clear()
        {
            _labelUsage.Clear();
        }

        public void AddLabelUsage(Chunk chunk, int inst, byte labelId)
        {
            _labelUsage.Add((chunk, inst, labelId));
        }

        public void ProcessPacket(Chunk currentChunk, int currentInstructionIndex, ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.TEST:
                if (packet.testOpDetails.TestOpType == TestOpType.TestFixtureBodyInstruction
                    || packet.testOpDetails.TestOpType == TestOpType.TestCase)
                    AddLabelUsage(currentChunk, currentInstructionIndex, packet.testOpDetails.b1);
                break;
            case OpCode.GOTO:
            case OpCode.GOTO_IF_FALSE:
                AddLabelUsage(currentChunk, currentInstructionIndex, packet.b1);
                break;
            }
        }
    }
}
