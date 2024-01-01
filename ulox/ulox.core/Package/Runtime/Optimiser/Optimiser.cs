﻿using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public sealed class Optimiser
    {
        public const byte NOT_LOCAL_BYTE = byte.MaxValue;

        public bool Enabled { get; set; } = true;
        public List<IOptimiserPass> OptimiserPasses { get; } = new List<IOptimiserPass>() 
        {
            new OptimiserUnreachableCodeRemovalPass(),
            new OptimiserRegisterisePass(),
            new OptimiserSingleLabelReorderPass(),
            new OptimiserUnreachableCodeRemovalPass(),
            new OptimiserZeroJumpRemovalPass(),
            new OptimiserCollapsePopsPass(),
            new OptimiserRemoveLabelsPass(),
        };
        public OptimisationReporter OptimisationReporter { get; set; }

        private List<(Chunk chunk, int inst)> _toRemove = new List<(Chunk, int)>();

        public void Optimise(CompiledScript compiledScript)
        {
            if (!Enabled)
                return;

            OptimisationReporter?.PreOptimise(compiledScript);
            foreach (var pass in OptimiserPasses)
            {
                pass.Run(this, compiledScript);
                RemoveMarkedInstructions();
            }
            OptimisationReporter?.PostOptimise(compiledScript);
        }

        public void Reset()
        {
            _toRemove.Clear();
            foreach (var pass in OptimiserPasses)
                pass.Reset();
        }

        public void AddToRemove(Chunk chunk, int inst)
        {
            _toRemove.Add((chunk, inst));
        }

        public bool IsMarkedForRemoval(Chunk chunk, int inst)
        {
            return _toRemove.Any(x => x.chunk == chunk && x.inst == inst);
        }

        public void FixUpToRemoves(Chunk chunk, int from, int numberMoved)
        {
            for (int i = _toRemove.Count - 1; i >= 0; i--)
            {
                var (c, inst) = _toRemove[i];
                if (c == chunk && inst >= from)
                {
                    _toRemove[i] = (c, inst + numberMoved);
                }
            }
        }

        private void RemoveMarkedInstructions()
        {
            _toRemove.Sort((x, y) => x.inst.CompareTo(y.inst));
            _toRemove = _toRemove.Distinct().ToList();

            for (int i = _toRemove.Count - 1; i >= 0; i--)
            {
                var (chunk, b) = _toRemove[i];
                chunk.RemoveInstructionAt(b);
            }
            _toRemove.Clear();
        }
    }
}
