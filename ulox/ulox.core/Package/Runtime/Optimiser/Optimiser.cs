using System.Collections.Generic;
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
            new OptimiserCollapseOpsPass(),
            new OptimiserRemoveLabelOpCodesPass(),
            new OptimiserCollapseDuplicateLabelsPass(),
            new OptimiserUnreachableCodeRemovalPass(),
            new OptimiserGotoLabelReorderPass(),
            new OptimiserUnreachableCodeRemovalPass(),
            new OptimiserCollapseDuplicateLabelsPass(),
            new OptimiserCollapseOpsPass(),
            new OptimiserRemoveUnusedLabelOpCodesPass(),
            //new OptimiserUnreachableCodeRemovalPass(),
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

        public static bool IsIndexWeaved(Chunk chunk, int from)
        {
            if(from == 0)
                return true;
            //ensures only way to get here was via weave not fallthrough
            var op = chunk.Instructions[from-1].OpCode;
            return op == OpCode.GOTO || op == OpCode.RETURN;
        }
    }
}
