﻿using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public sealed class Optimiser
    {
        public enum PassCompleteRequest
        {
            None,
            Repeat,
        }

        public const byte NOT_LOCAL_BYTE = byte.MaxValue;

        public bool Enabled { get; set; } = true;
        public List<IOptimiserPass> OptimiserPasses { get; } = new List<IOptimiserPass>() 
        {
            new OptimiserRegisterisePass(),
            new OptimiserRemoveLabelOpCodesPass(),
            new OptimiserCollapseDuplicateLabelsPass(),
            new OptimiserUnreachableCodeRemovalPass(),
            new OptimiserGotoLabelReorderPass(),
            new OptimiserUnreachableCodeRemovalPass(),
            new OptimiserCollapseDuplicateLabelsPass(),
            new OptimiserCollapsePopsPass(),
            new OptimiserRemoveUnusedLabelOpCodesPass(),
        };
        public OptimisationReporter OptimisationReporter { get; set; }

        private List<(Chunk chunk, int inst)> _toRemove = new List<(Chunk, int)>();

        public void Optimise(CompiledScript compiledScript)
        {
            if (!Enabled)
                return;

            OptimisationReporter?.PreOptimise(compiledScript);
            foreach (var chunk in compiledScript.AllChunks)
            {
                for (int passIndex = 0; passIndex < OptimiserPasses.Count; passIndex++)
                {
                    var pass = OptimiserPasses[passIndex];
                    var len = chunk.Instructions.Count;
                    pass.Prepare(this, chunk);
                    for (int i = 0; i < len; i++)
                    {
                        pass.ProcessPacket(this, chunk, i, chunk.Instructions[i]);
                    }
                    var request = pass.Complete(this, chunk);
                    RemoveMarkedInstructions();
                    if(request == PassCompleteRequest.Repeat)
                    {
                        passIndex--;
                    }
                }
            }
            OptimisationReporter?.PostOptimise(compiledScript);
        }

        public void Reset()
        {
            _toRemove.Clear();
        }

        public void AddToRemove(Chunk chunk, int inst)
        {
            _toRemove.Add((chunk, inst));
        }

        public bool IsMarkedForRemoval(Chunk chunk, int inst)
        {
            return _toRemove.Any(x => x.chunk == chunk && x.inst == inst);
        }

        public void RemoveMarkedInstructions()
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
