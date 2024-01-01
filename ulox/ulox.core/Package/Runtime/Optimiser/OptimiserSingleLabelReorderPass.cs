using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public sealed class OptimiserSingleLabelReorderPass : CompiledScriptIterator, IOptimiserPass
    {
        private readonly List<(Chunk chunk, int from, byte label)> _labelUsage = new List<(Chunk, int, byte)>();
        private Optimiser _optimiser;

        public void Run(Optimiser optimiser, CompiledScript compiledScript)
        {
            _optimiser = optimiser;
            Iterate(compiledScript);
            ProcessReordering();
        }

        private void ProcessReordering()
        {
            //find labels with a single use
            var singleUseLabelsFromGotos = _labelUsage
                .GroupBy(x => x.label)
                .Where(x => x.Count() == 1)
                .SelectMany(x => x)
                .Where(x => x.chunk.Instructions[x.from].OpCode == OpCode.GOTO);

            var descAttempts = singleUseLabelsFromGotos.OrderByDescending(x => x.from).ToArray();

            for (var descI = 0; descI < descAttempts.Length; descI++)
            {
                var (chunk, from, label) = descAttempts[descI];

                if (_optimiser.IsMarkedForRemoval(chunk, from))
                    continue;

                //find end of this section
                var startLoc = chunk.GetLabelPosition(label);
                var canMove = true;
                int endLoc = startLoc;
                for (; endLoc < chunk.Instructions.Count && canMove; endLoc++)
                {
                    var op = chunk.Instructions[endLoc].OpCode;

                    //have we found the end
                    if (op == OpCode.GOTO
                        || op == OpCode.RETURN)
                        break;

                    //have we found something we cannot move
                    if (op == OpCode.GOTO_IF_FALSE
                        || op == OpCode.LABEL
                        || op == OpCode.TEST)
                        canMove = false;
                }

                if (!canMove)
                    continue;

                if (startLoc >= endLoc)
                    continue;

                var moveSpan = endLoc - startLoc + 1;

                var toMove = GetGotoReorgSpan(chunk, startLoc, startLoc + moveSpan).ToArray();    //todo temp
                _optimiser.AddToRemove(chunk, from);
                for (var i = 0; i < moveSpan; i++)
                {
                    _optimiser.AddToRemove(chunk, startLoc + i);
                }
                chunk.InsertInstructionsAt(from + 1, toMove);
                var insertedCount = toMove.Length;
                _optimiser.FixUpToRemoves(chunk, from + 1, insertedCount);
                FixUpSingleUseLables(descAttempts, from + 1, startLoc, insertedCount);
            }
        }

        private void FixUpSingleUseLables(
            (Chunk chunk, int from, byte label)[] descAttempts,
            int at,
            ushort startLoc,
            int count)
        {
            var movedDist = startLoc - at;
            for (int i = 0; i < descAttempts.Length; i++)
            {
                var (chunk, from, label) = descAttempts[i];
                if (chunk == CurrentChunk)
                {
                    if (from < at) //unaffected
                        continue;
                    else if (from >= startLoc && from < startLoc + count)   //just moved them
                        descAttempts[i] = (chunk, (ushort)(from - movedDist), label);
                    else //pushed them back
                        descAttempts[i] = (chunk, from + count, label);
                }
            }
        }

        private IEnumerable<ByteCodePacket> GetGotoReorgSpan(Chunk chunk, int startLoc, int endLoc)
        {
            for (int i = startLoc; i < endLoc; i++)
            {
                var bytePacket = chunk.Instructions[i];
                if (bytePacket.OpCode == OpCode.GOTO)
                    yield return bytePacket;
                else if (_optimiser.IsMarkedForRemoval(chunk, i))
                    continue;
                else
                    yield return chunk.Instructions[i];
            }
        }

        protected override void PreChunkInterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        protected override void ProcessPacket(ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.TEST:
                if (packet.testOpDetails.TestOpType == TestOpType.TestFixtureBodyInstruction)
                    AddLabelUsage(packet.testOpDetails.b1);
                else if (packet.testOpDetails.TestOpType == TestOpType.TestCase)
                    AddLabelUsage(packet.testOpDetails.b2);
                break;
            case OpCode.GOTO:
            case OpCode.GOTO_IF_FALSE:
                AddLabelUsage(packet.b1);
                break;
            }
        }

        protected override void PostChunkIterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        private void AddLabelUsage(byte labelId)
        {
            _labelUsage.Add((CurrentChunk, CurrentInstructionIndex, labelId));
        }

        public void Reset()
        {
            _labelUsage.Clear();
        }
    }
}
