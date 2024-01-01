using System.Linq;

namespace ULox
{
    public sealed class OptimiserZeroJumpRemovalPass : CompiledScriptIterator, IOptimiserPass
    {
        private Optimiser _optimiser;

        public void Run(Optimiser optimiser, CompiledScript compiledScript)
        {
            _optimiser = optimiser;
            Iterate(compiledScript);
        }

        protected override void PreChunkInterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        protected override void ProcessPacket(ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.GOTO:
                var labelId = packet.b1;
                var labelLoc = CurrentChunk.Labels[labelId];
                var from = CurrentInstructionIndex + 1;
                var dist = labelLoc - from;
                if (dist == 0)
                    _optimiser.AddToRemove(CurrentChunk, CurrentInstructionIndex);
                else if (dist < 0)
                    break;
                else if (CurrentChunk.Instructions.GetRange(from, dist).All(x => x.OpCode == OpCode.LABEL))
                    _optimiser.AddToRemove(CurrentChunk, CurrentInstructionIndex);
                break;
            }
        }

        protected override void PostChunkIterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        public void Reset()
        {
        }
    }
}
