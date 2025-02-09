using System.Collections.Generic;
using System.Linq;
using static ULox.Optimiser;

namespace ULox
{
    public sealed class OptimiserCollapseOpsPass : IOptimiserPass
    {
        private readonly List<int> _byteToProcess = new();
        private readonly List<int> _getLocals = new();
        private readonly List<int> _popsToInspect = new();

        public void Prepare(Optimiser optimiser, Chunk chunk)
        {
            _byteToProcess.Clear();
            _getLocals.Clear();
            _popsToInspect.Clear();
        }

        public void ProcessPacket(Optimiser optimiser, Chunk chunk, int inst, ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.GET_LOCAL:
                if (packet.b2 == Optimiser.NOT_LOCAL_BYTE)   //for now only optimise single byte locals
                    _getLocals.Add(inst);
                break;
            case OpCode.POP:
                if (inst < chunk.Instructions.Count - 1
                    && chunk.Instructions[inst + 1].OpCode == OpCode.POP)
                    _popsToInspect.Add(inst);
                break;
            }
        }

        public PassCompleteRequest Complete(Optimiser optimiser, Chunk chunk)
        {
            for (int i = 0; i < _getLocals.Count - 1; i++)
            {
                var inst1 = _getLocals[i];
                var inst2 = _getLocals[i + 1];
                if (inst2 - inst1 != 1
                    || chunk.Labels.Any(x => x.Value == inst1))
                    continue;

                var packet2 = chunk.Instructions[inst2];
                if (packet2.b2 != Optimiser.NOT_LOCAL_BYTE)   //for now only optimise single byte locals
                    continue;
                
                var packet1 = chunk.Instructions[inst1];
                var b1 = packet1.b1;
                var b2 = packet2.b1;
                var b3 = NOT_LOCAL_BYTE;
                optimiser.AddToRemove(chunk, inst2);

                //check that 3rd too
                if (i < _getLocals.Count - 2)
                {
                    var inst3 = _getLocals[i + 2];
                    if (inst3 - inst2 == 1
                        || chunk.Labels.Any(x => x.Value == inst2))
                    {
                        b3 = chunk.Instructions[inst3].b1;
                        optimiser.AddToRemove(chunk, inst3);
                        i++;
                    }
                }

                var newPacket = new ByteCodePacket(OpCode.GET_LOCAL, b1, b2, b3);
                chunk.Instructions[inst1] = newPacket;
                i++;
            }

            foreach (var inst in _popsToInspect)
            {
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
