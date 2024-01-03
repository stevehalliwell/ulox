using System.Collections.Generic;
using System.Linq;
using static ULox.Optimiser;

namespace ULox
{
    //todo this is mostly future proofing, we don't have any pops to collapse yet
    public sealed class OptimiserCollapsePopsPass : IOptimiserPass
    {
        private readonly List<int> _popsToInspect = new List<int>();

        public void Prepare(Optimiser optimiser, Chunk chunk)
        {
            _popsToInspect.Clear();
        }

        public void ProcessPacket(Optimiser optimiser, Chunk chunk, int inst, ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.POP:
                if (inst < chunk.Instructions.Count - 1
                    && chunk.Instructions[inst + 1].OpCode == OpCode.POP)
                    _popsToInspect.Add(inst);
                break;
            }
        }

        public PassCompleteRequest Complete(Optimiser optimiser, Chunk chunk)
        {
            foreach (var inst in _popsToInspect)
            {
                //doing this incase labels are already removed
                if (chunk.Labels.Any(x => x.Value == inst))
                    continue;

                optimiser.AddToRemove(chunk, inst);
                var packCur = chunk.Instructions[inst];
                var packNext = chunk.Instructions[inst + 1];
                chunk.Instructions[inst + 1] = new ByteCodePacket(OpCode.POP, (byte)(packCur.b1 + packNext.b1));
            }

            return PassCompleteRequest.None;
        }
    }
}
