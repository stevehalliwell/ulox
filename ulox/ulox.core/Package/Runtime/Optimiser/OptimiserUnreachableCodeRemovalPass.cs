﻿using System.Collections.Generic;
using System.Linq;
using static ULox.Optimiser;

namespace ULox
{
    public sealed class OptimiserUnreachableCodeRemovalPass : IOptimiserPass
    {
        private readonly List<int> _unreachablePoint = new();

        public void Prepare(Optimiser optimiser, Chunk chunk)
        {
            _unreachablePoint.Clear();
        }

        public void ProcessPacket(Optimiser optimiser, Chunk chunk, int inst, ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.GOTO:
            case OpCode.RETURN:
                if (!chunk.Labels.Any(x => x.Value == inst))
                    _unreachablePoint.Add(inst + 1);
                break;
            }
        }

        public PassCompleteRequest Complete(Optimiser optimiser, Chunk chunk)
        {
            for (int i = _unreachablePoint.Count - 1; i >= 0; i--)
            {
                var inst = _unreachablePoint[i];
                var nextLabelLocOrEnd = chunk.Labels.Values.Where(x => x >= inst)
                    .DefaultIfEmpty(chunk.Instructions.Count - 1)
                    .Min();

                for (int j = inst; j <= nextLabelLocOrEnd; j++)
                    optimiser.AddToRemove(chunk, j);
            }

            return PassCompleteRequest.None;
        }
    }
}
