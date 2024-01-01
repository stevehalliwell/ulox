using System.Collections.Generic;

namespace ULox
{
    public sealed class OptimiserCollapsePopsPass : CompiledScriptIterator, IOptimiserPass
    {
        private List<(Chunk chunk, int inst)> _pops = new List<(Chunk chunk, int inst)>();
        private Optimiser _optimiser;

        public void Run(Optimiser optimiser, CompiledScript compiledScript)
        {
            _optimiser = optimiser;
            Iterate(compiledScript);
            AdjustAndMarkCollapsePops();
        }

        private void AdjustAndMarkCollapsePops()
        {
            var pairEnd = _pops.Count - 1;
            for (var i = 0; i < pairEnd; i++)
            {
                var (chunkCur, instCur) = _pops[i];
                var (chunkNext, instNext) = _pops[i+1];

                if (chunkCur != chunkNext)
                    continue;

                if (instCur != instNext - 1)
                    continue;

                _optimiser.AddToRemove(chunkCur, instCur);
                var packCur = chunkCur.Instructions[instCur];
                var packNext = chunkNext.Instructions[instNext];
                chunkNext.Instructions[instNext] = new ByteCodePacket(OpCode.POP, (byte)(packCur.b1 + packNext.b1));
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
                _pops.Add((CurrentChunk, CurrentInstructionIndex));
                break;
            }
        }

        protected override void PostChunkIterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        public void Reset()
        {
            _pops.Clear();
        }
    }
}
