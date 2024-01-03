using System.Collections.Generic;

namespace ULox
{
    public sealed class OptimiserLabelUsageAccumulator
    {
        private readonly List<(int from, byte label)> _labelUsage = new List<(int, byte)>();
        public IReadOnlyList<(int from, byte label)> LabelUsage => _labelUsage;

        public void Clear()
        {
            _labelUsage.Clear();
        }

        public void AddLabelUsage(int inst, byte labelId)
        {
            _labelUsage.Add((inst, labelId));
        }

        public void ProcessPacket(int currentInstructionIndex, ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.TEST:
                if (packet.testOpDetails.TestOpType == TestOpType.TestFixtureBodyInstruction
                    || packet.testOpDetails.TestOpType == TestOpType.TestCase)
                    AddLabelUsage(currentInstructionIndex, packet.testOpDetails.b1);
                break;
            case OpCode.GOTO:
            case OpCode.GOTO_IF_FALSE:
                AddLabelUsage(currentInstructionIndex, packet.b1);
                break;
            }
        }
    }
}
