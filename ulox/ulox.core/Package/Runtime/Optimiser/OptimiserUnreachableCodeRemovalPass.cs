using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public sealed class OptimiserUnreachableCodeRemovalPass : CompiledScriptIterator, IOptimiserPass
    {
        private readonly List<(Chunk chunk, int inst)> _unreachablePoint = new List<(Chunk, int)>();
        private Optimiser _optimiser;

        public void Run(Optimiser optimiser, CompiledScript compiledScript)
        {
            _optimiser = optimiser;
            Iterate(compiledScript);
            ProcessUnreachable();
        }

        protected override void PreChunkInterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        protected override void ProcessPacket(ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.GOTO:
            case OpCode.RETURN:
                if (!CurrentChunk.Labels.Any(x => x.Value == CurrentInstructionIndex))
                    _unreachablePoint.Add((CurrentChunk, CurrentInstructionIndex + 1));
                break;
            }
        }

        protected override void PostChunkIterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        public void Reset()
        {
            _unreachablePoint.Clear();
        }

        private void ProcessUnreachable()
        {
            foreach (var (chunk, inst) in _unreachablePoint.OrderByDescending(x => x.inst))
            {
                var nextLabelLocOrEnd = chunk.Labels.Values.Where(x => x >= inst)
                    .DefaultIfEmpty(chunk.Instructions.Count-1)
                    .Min();

                for (int i = inst; i <= nextLabelLocOrEnd; i++)
                    _optimiser.AddToRemove(chunk, i);
            }
        }
    }
}
