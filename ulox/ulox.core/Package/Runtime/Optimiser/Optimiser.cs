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

        private readonly List<(Chunk chunk, int inst)> _toRemove = new();
        private readonly List<int> _distinctRemovals = new();

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
            if (_toRemove.Count == 0)
                return;

            _toRemove.Sort((x, y) => x.inst.CompareTo(y.inst));
            _distinctRemovals.Clear();

            var chunk = _toRemove[0].chunk;
            var previousInst = -1;
            foreach (var item in _toRemove)
            {
                if (item.inst == previousInst)
                    continue;
                _distinctRemovals.Add(item.inst);
                previousInst = item.inst;
            }

            var write = 0;
            var nextRemovalIndex = 0;
            var nextRemoval = _distinctRemovals[nextRemovalIndex];
            for (var read = 0; read < chunk.Instructions.Count; read++)
            {
                if (read == nextRemoval)
                {
                    nextRemovalIndex++;
                    nextRemoval = nextRemovalIndex < _distinctRemovals.Count ? _distinctRemovals[nextRemovalIndex] : -1;
                    continue;
                }

                if (write != read)
                    chunk.Instructions[write] = chunk.Instructions[read];
                write++;
            }
            chunk.Instructions.RemoveRange(write, chunk.Instructions.Count - write);

            foreach (var label in new List<Label>(chunk.Labels.Keys))
            {
                var pos = chunk.Labels[label];
                chunk.Labels[label] = pos - CountRemovalsAtOrBefore(pos);
            }

            for (var i = 0; i < chunk.RunLengthLineNumbers.Count; i++)
            {
                var item = chunk.RunLengthLineNumbers[i];
                chunk.RunLengthLineNumbers[i] = new Chunk.RunLengthLineNumber()
                {
                    line = item.line,
                    startingInstruction = item.startingInstruction - CountRemovalsAtOrBefore(item.startingInstruction)
                };
            }

            _toRemove.Clear();
        }

        private int CountRemovalsAtOrBefore(int instruction)
        {
            var count = 0;
            for (; count < _distinctRemovals.Count; count++)
            {
                if (_distinctRemovals[count] > instruction)
                    break;
            }
            return count;
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
