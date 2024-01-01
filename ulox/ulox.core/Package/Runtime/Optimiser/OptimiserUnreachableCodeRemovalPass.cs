using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public sealed class OptimiserUnreachableCodeRemovalPass : CompiledScriptIterator, IOptimiserPass
    {
        private readonly List<(Chunk chunk, int inst)> _unreachableFollowingGoto = new List<(Chunk, int)>();
        private readonly List<(Chunk chunk, int inst)> _unreachableFollowingReturns = new List<(Chunk, int)>();
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
            case OpCode.GOTO://todo if this was smarter it could go after labels are removed
                if (CurrentChunk.Instructions[CurrentInstructionIndex+1].OpCode != OpCode.LABEL)
                    _unreachableFollowingGoto.Add((CurrentChunk, CurrentInstructionIndex));
                break;
            case OpCode.RETURN:
                if(!CurrentChunk.Labels.Any(x => x.Value > CurrentInstructionIndex))
                    _unreachableFollowingReturns.Add((CurrentChunk, CurrentInstructionIndex));
                break;
            }
        }

        protected override void PostChunkIterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        public void Reset()
        {
            _unreachableFollowingGoto.Clear();
            _unreachableFollowingReturns.Clear();
        }

        private void ProcessUnreachable()
        {
            foreach (var (chunk, inst) in _unreachableFollowingGoto)
            {
                for(var next = inst + 1; next < chunk.Instructions.Count; next++)
                {
                    if (chunk.Instructions[next].OpCode == OpCode.LABEL)
                    {
                        break;
                    }
                    _optimiser.AddToRemove(chunk, next);
                }
            }

            foreach (var (chunk, inst) in _unreachableFollowingReturns)
            {
                for (var next = inst + 1; next < chunk.Instructions.Count; next++)
                {
                    _optimiser.AddToRemove(chunk, next);
                }
            }
        }
    }
}
