using System.Collections.Generic;
using System.Linq;
using static ULox.Optimiser;

namespace ULox
{
    public sealed class OptimiserGotoLabelReorderPass : IOptimiserPass
    {
        private readonly OptimiserLabelUsageAccumulator _optimiserLabelUsageAccumulator = new OptimiserLabelUsageAccumulator();
        private readonly List<int> _gotos = new List<int>();

        public void Prepare(Optimiser optimiser, Chunk chunk)
        {
            _optimiserLabelUsageAccumulator.Clear();
            _gotos.Clear();
        }

        public void ProcessPacket(Optimiser optimiser, Chunk chunk, int inst, ByteCodePacket packet)
        {
            _optimiserLabelUsageAccumulator.ProcessPacket(inst, packet);
            switch (packet.OpCode)
            {
            case OpCode.GOTO:
                _gotos.Add(inst);
                break;
            }
        }

        public PassCompleteRequest Complete(Optimiser optimiser, Chunk chunk)
        {
            var retval = PassCompleteRequest.None;
            var labelUsage = _optimiserLabelUsageAccumulator.LabelUsage;
            //any zero jumps just nuke them all and go next
            var zeroJumps = labelUsage
                .Where(x => chunk.Labels[x.label] == x.from)
                .ToArray();

            foreach (var (from, labelId) in zeroJumps)
            {
                optimiser.AddToRemove(chunk, from);
                retval = PassCompleteRequest.Repeat;
            }

            return retval;
        }

        //todo we would like to do this but it's not playing nice
        //public void ProcessSingleUsageLabels()
        //{
        //    var labelUsage = _optimiserLabelUsageAccumulator.LabelUsage;
        //    //find labels with a single use
        //    var singleUseLabelsFromGotos = _gotos
        //        .Where(x => labelUsage.Count(y => y.chunk == x.chunk && y.from == x.loc) == 1)
        //        .Select(x => (x.chunk, from: x.loc, labelId: x.chunk.Instructions[x.loc].b1))
        //        .Select(x => (x.chunk, x.from, x.labelId, labelDest: x.chunk.Labels[x.labelId]))
        //        .Where(x => x.chunk.Instructions[x.labelDest].OpCode == OpCode.GOTO)
        //        .ToArray();

        //    var attempts = singleUseLabelsFromGotos.OrderBy(x => x.from).ToList();

        //    for (int attempt = attempts.Count - 1; attempt >= 0; attempt--)
        //    {
        //        var (chunk, from, labelId, labelDest) = attempts[attempt];
        //        attempts.RemoveAt(attempt);

        //        if (_optimiser.IsMarkedForRemoval(chunk, from))
        //            continue;

        //        //find end of this section
        //        var startLoc = labelDest + 1;
        //        var canMove = true;
        //        int endLoc = startLoc;
        //        for (; endLoc < chunk.Instructions.Count && canMove; endLoc++)
        //        {
        //            var op = chunk.Instructions[endLoc].OpCode;

        //            //have we found the end
        //            if (op == OpCode.GOTO
        //                || op == OpCode.RETURN)
        //                break;

        //            //have we found something we cannot move
        //            if (op == OpCode.GOTO_IF_FALSE
        //                || op == OpCode.TEST)
        //                canMove = false;
        //        }

        //        if (!canMove)
        //            continue;

        //        if (chunk.Labels.Any(x => x.Value >= startLoc && x.Value < endLoc))
        //            continue;

        //        var moveSpan = endLoc - startLoc + 1;

        //        var toMove = chunk.Instructions.GetRange(startLoc, moveSpan);
        //        chunk.InsertInstructionsAt(from, toMove);

        //        _optimiser.AddToRemove(chunk, from + moveSpan);
        //        var shiftedBy = startLoc < from ? 0 : moveSpan;
        //        for (var i = startLoc; i < startLoc + moveSpan; i++)
        //        {
        //            _optimiser.AddToRemove(chunk, i + shiftedBy);
        //        }

        //        _madeChanges = true;
        //        return;
        //    }
        //}
    }
}
