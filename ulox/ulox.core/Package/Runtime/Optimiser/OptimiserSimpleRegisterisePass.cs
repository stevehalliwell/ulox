using static ULox.Optimiser;

namespace ULox
{
    public sealed class OptimiserSimpleRegisterisePass : IOptimiserPass
    {
        private ByteCodePacket _previousPacket;

        public void Prepare(Optimiser optimiser, Chunk chunk)
        {
            _previousPacket = default;
        }

        public void ProcessPacket(Optimiser optimiser, Chunk chunk, int inst, ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.NEGATE:
            case OpCode.NOT:
            case OpCode.COUNT_OF:
            case OpCode.DUPLICATE:
                if (_previousPacket.OpCode == OpCode.GET_LOCAL)
                {
                    optimiser.AddToRemove(chunk, inst - 1);
                    chunk.Instructions[inst] = new ByteCodePacket(packet.OpCode, _previousPacket.b1);
                }
                break;
            case OpCode.GET_PROPERTY:
                if (_previousPacket.OpCode == OpCode.GET_LOCAL)
                {
                    optimiser.AddToRemove(chunk, inst - 1);
                    chunk.Instructions[inst] = new ByteCodePacket(packet.OpCode, packet.b1, packet.b2, _previousPacket.b1);
                }
                break;
            }

            _previousPacket = packet;
        }

        public PassCompleteRequest Complete(Optimiser optimiser, Chunk chunk)
        {
            return PassCompleteRequest.None;
        }
    }
}
