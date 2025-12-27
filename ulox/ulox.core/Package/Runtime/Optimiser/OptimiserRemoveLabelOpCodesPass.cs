using static ULox.Optimiser;

namespace ULox
{
    public sealed class OptimiserRemoveLabelOpCodesPass : IOptimiserPass
    {
        public void Prepare(Optimiser optimiser, Chunk chunk)
        {
        }

        public void ProcessPacket(Optimiser optimiser, Chunk chunk, int inst, ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.LABEL:
                optimiser.AddToRemove(chunk, inst);
                break;
            }
        }

        public PassCompleteRequest Complete(Optimiser optimiser, Chunk chunk)
        {
            return PassCompleteRequest.None;
        }
    }
}
