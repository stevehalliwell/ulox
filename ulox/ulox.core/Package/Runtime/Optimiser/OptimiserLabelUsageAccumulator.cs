using System.Collections.Generic;

namespace ULox
{
    public sealed class OptimiserLabelUsageAccumulator
    {
        private readonly List<(int from, byte label, OpCode opCode, bool isWeaved)> _labelUsage = new();
        public IReadOnlyList<(int from, byte label, OpCode opCode, bool isWeaved)> LabelUsage => _labelUsage;

        public void Clear()
        {
            _labelUsage.Clear();
        }

        public void AddLabelUsage(Chunk chunk, int inst, byte labelId)
        {
            _labelUsage.Add((inst, labelId, chunk.Instructions[inst].OpCode, Optimiser.IsIndexWeaved(chunk, inst)));
        }

        public void ProcessPacket(Chunk chunk, int currentInstructionIndex, ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.TEST:
                if (packet.testOpDetails.TestOpType == TestOpType.TestFixtureBodyInstruction
                    || packet.testOpDetails.TestOpType == TestOpType.TestCase)
                    AddLabelUsage(chunk, currentInstructionIndex, packet.testOpDetails.b1);
                break;
            case OpCode.GOTO:
            case OpCode.GOTO_IF_FALSE:
                AddLabelUsage(chunk, currentInstructionIndex, packet.b1);
                break;
            }
        }
    }
}
