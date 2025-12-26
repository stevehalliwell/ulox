using System.Collections.Generic;

namespace ULox
{
    public interface IOptimiserPass
    {
        void Prepare(Optimiser optimiser, Chunk chunk);
        void ProcessPacket(Optimiser optimiser, Chunk chunk, int inst, ByteCodePacket packet);
        Optimiser.PassCompleteRequest Complete(Optimiser optimiser, Chunk chunk);
    }

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
            new OptimiserPreenGetLocalsPass(),
            new OptimiserSimpleRegisterisePass(),
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
            new OptimiserUnreachableCodeRemovalPass(),
        };
        public OptimisationReporter OptimisationReporter { get; set; }

        private List<(Chunk chunk, int inst)> _toRemove = new();

        public void Optimise(CompiledScript compiledScript)
        {
            if (!Enabled)
                return;

            OptimisationReporter?.PreOptimise(compiledScript);
            foreach (var chunk in compiledScript.AllChunks)
            {
                for (var passIndex = 0; passIndex < OptimiserPasses.Count; passIndex++)
                {
                    var pass = OptimiserPasses[passIndex];
                    var len = chunk.Instructions.Count;
                    pass.Prepare(this, chunk);
                    for (var i = 0; i < len; i++)
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
            var prevItem = default((Chunk chunk,int inst));

            //PERF: doing this at instruction at a time is slow
            //  ordering by chunk and marching from front to back 
            //  with a running count would be faster (split read and write heads)
            for (int i = _toRemove.Count - 1; i >= 0; i--)
            {
                var item = _toRemove[i];
                if (prevItem.chunk == item.chunk && prevItem.inst == item.inst)
                    continue;
                var (chunk, b) = item;
                chunk.Instructions.RemoveAt(b);
                chunk.AdjustLabelIndicies(b, -1);
                chunk.AdjustLineNumbers(b, -1); ;
                prevItem = item;
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
