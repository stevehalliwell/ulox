using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    //todo this is mostly future proofing, we don't have any pops to collapse yet
    public sealed class OptimiserCollapsePopsPass : CompiledScriptIterator, IOptimiserPass
    {
        private readonly List<(Chunk chunk, int inst)> _popsToInspect = new List<(Chunk chunk, int inst)>();
        private Optimiser _optimiser;

        public void Run(Optimiser optimiser, CompiledScript compiledScript)
        {
            _optimiser = optimiser;
            Iterate(compiledScript);
            AdjustAndMarkCollapsePops();
        }

        private void AdjustAndMarkCollapsePops()
        {
            foreach (var (chunk, inst) in _popsToInspect)
            {
                //doing this incase labels are already removed
                if (chunk.Labels.Any(x => x.Value == inst))
                    continue;

                _optimiser.AddToRemove(chunk, inst);
                var packCur = chunk.Instructions[inst];
                var packNext = chunk.Instructions[inst+1];
                chunk.Instructions[inst+1] = new ByteCodePacket(OpCode.POP, (byte)(packCur.b1 + packNext.b1));
            }
        }

        protected override void PreChunkInterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        protected override void ProcessPacket(ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.POP:
                if (CurrentInstructionIndex < CurrentChunk.Instructions.Count - 1 
                    && CurrentChunk.Instructions[CurrentInstructionIndex+1].OpCode == OpCode.POP)
                    _popsToInspect.Add((CurrentChunk, CurrentInstructionIndex));
                break;
            }
        }

        protected override void PostChunkIterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        public void Reset()
        {
            _popsToInspect.Clear();
        }
    }
}
