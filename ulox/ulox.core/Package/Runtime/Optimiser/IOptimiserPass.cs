using static ULox.Optimiser;

namespace ULox
{
    public interface IOptimiserPass
    {
        void Prepare(Optimiser optimiser, Chunk chunk);
        void ProcessPacket(Optimiser optimiser, Chunk chunk, int inst, ByteCodePacket packet);
        PassCompleteRequest Complete(Optimiser optimiser, Chunk chunk);
    }
}
