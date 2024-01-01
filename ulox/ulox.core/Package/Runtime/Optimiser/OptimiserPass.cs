using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public sealed class OptimiserPass : CompiledScriptIterator
    {
        private enum RegisteriseType
        {
            Unknown,
            Uniary,
            Binary,
            SetIndex,
            GetProp,
            SetProp,
        }

        private List<(Chunk chunk, int inst, RegisteriseType regType)> _potentialRegisterise = new List<(Chunk, int, RegisteriseType)>();
        private readonly List<(Chunk chunk, int from, byte label)> _labelUsage = new List<(Chunk, int, byte)>();
        private List<(Chunk chunk, int inst)> _gotos = new List<(Chunk chunk, int inst)>();
        private List<(Chunk chunk, int inst)> _pops = new List<(Chunk chunk, int inst)>();
        private OpCode _prevOoCode;
        private int _deadCodeStart = -1;
        private Optimiser _optimiser;

        public bool EnableLocalizing { get; set; } = true;
        public bool EnableDeadCodeRemoval { get; set; } = true;
        public bool EnableZeroJumpGotoRemoval { get; set; } = true;
        public bool EnablePopCollapse { get; set; } = true;
        public bool EnableByteCodeReordering { get; set; } = true;

        public void Run(Optimiser optimiser, CompiledScript compiledScript, TypeInfo typeInfo)
        {
            _optimiser = optimiser;
            Iterate(compiledScript);
            if (EnableLocalizing) ProcessRegisterise();
            if (EnableZeroJumpGotoRemoval) MarkZeroJumpGotoForRemoval();
            if (EnablePopCollapse) AdjustAndMarkCollapsePops();
            if (EnableByteCodeReordering) ProcessReordering();
        }

        private void MarkZeroJumpGotoForRemoval()
        {
            //want to do this in reverse order so we can mark things as to remove in longer forward stretches
            _gotos = _gotos.OrderByDescending(x => x.inst).ToList();

            //todo check that gotos have alive code to actually skip, if not add them to the toremove
            foreach (var (chunk, inst) in _gotos)
            {
                //if behind us skip
                var labelId = chunk.Instructions[inst].b1;
                var labelLoc = chunk.Labels[labelId];
                if (inst > labelLoc)
                    continue;

                //if already skipped
                if (_optimiser.IsMarkedForRemoval(chunk, inst))
                    continue;

                //are all ops between us and the label marked as toremove?
                var found = false;
                for (int i = inst + 1; i < labelLoc; i++)
                {
                    if (!_optimiser.IsMarkedForRemoval(chunk, i))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _optimiser.AddToRemove(chunk, inst);
                }
            }
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

        private void AdjustAndMarkCollapsePops()
        {
            for (int i = _pops.Count - 1; i >= 0; i--)
            {
                var (chunk, inst) = _pops[i];

                //if already skipped
                if (_optimiser.IsMarkedForRemoval(chunk, inst))
                    continue;

                //read back until not skipped
                var back = 1;
                while (_optimiser.IsMarkedForRemoval(chunk, inst - back))
                {
                    back++;
                }

                var locToCheck = inst - back;
                if (locToCheck < 0)
                    continue;

                var instructionAtLoc = chunk.Instructions[locToCheck];
                //if it's a pop, remove us and adjust it to include us
                if (instructionAtLoc.OpCode == OpCode.POP)
                {
                    //if there's a label between us we can't
                    if (chunk.Labels.Values.Any(x => x >= locToCheck && x < inst))
                        continue;

                    var ourInstruction = chunk.Instructions[inst];
                    _optimiser.AddToRemove(chunk, inst);
                    chunk.Instructions[locToCheck] = new ByteCodePacket(OpCode.POP, (byte)(instructionAtLoc.b1 + ourInstruction.b1));
                }
            }
        }

        protected override void PreChunkInterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        protected override void ProcessPacket(ByteCodePacket packet)
        {
            var opCode = packet.OpCode;
            if (EnableDeadCodeRemoval)
            {
                if (_deadCodeStart == -1
                    && _prevOoCode == OpCode.GOTO
                    && opCode != OpCode.LABEL)
                {
                    _deadCodeStart = CurrentInstructionIndex;
                }

                if (opCode == OpCode.LABEL
                    && _deadCodeStart != -1)
                {
                    foreach (var i in Enumerable.Range(_deadCodeStart, CurrentInstructionIndex - _deadCodeStart))
                    {
                        _optimiser.AddToRemove(CurrentChunk, i);
                    }
                    _deadCodeStart = -1;
                }
            }
            _prevOoCode = opCode;

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
                AddGotoLabel((CurrentChunk, CurrentInstructionIndex));
                AddLabelUsage(packet.b1);
                break;
            case OpCode.ADD:
            case OpCode.SUBTRACT:
            case OpCode.MULTIPLY:
            case OpCode.DIVIDE:
            case OpCode.MODULUS:
            case OpCode.EQUAL:
            case OpCode.LESS:
            case OpCode.GREATER:
            case OpCode.GET_INDEX:
                _potentialRegisterise.Add((CurrentChunk, CurrentInstructionIndex, RegisteriseType.Binary));
                break;
            case OpCode.SET_INDEX:
                _potentialRegisterise.Add((CurrentChunk, CurrentInstructionIndex, RegisteriseType.SetIndex));
                break;
            case OpCode.NEGATE:
            case OpCode.NOT:
            case OpCode.COUNT_OF:
            case OpCode.DUPLICATE:
                _potentialRegisterise.Add((CurrentChunk, CurrentInstructionIndex, RegisteriseType.Uniary));
                break;
            case OpCode.GET_PROPERTY:
                _potentialRegisterise.Add((CurrentChunk, CurrentInstructionIndex, RegisteriseType.GetProp));
                break;
            case OpCode.SET_PROPERTY:
                _potentialRegisterise.Add((CurrentChunk, CurrentInstructionIndex, RegisteriseType.SetProp));
                break;
            case OpCode.LABEL:
                _optimiser.AddToRemove(CurrentChunk, CurrentInstructionIndex);
                break;
            case OpCode.POP:
                _pops.Add((CurrentChunk, CurrentInstructionIndex));
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

        private void AddGotoLabel((Chunk chunk, int instruction) value)
        {
            _gotos.Add(value);
        }

        private void ProcessRegisterise()
        {
            foreach (var (chunk, inst, regType) in _potentialRegisterise)
            {
                var original = chunk.Instructions[inst];
                var nb1 = original.b1;
                var nb2 = original.b2;
                var nb3 = original.b3;

                var prev = chunk.Instructions[inst - 1];

                switch (regType)
                {
                case RegisteriseType.Binary:
                    //TODO: would like to but it conflicts with add overload internals at the moment
                    // either way we would need to have all the binary ops check the set byte for local or stack
                    //if the following is a set local we can just do that
                    //if (chunk.Instructions.Count > inst)
                    //{
                    //    var next = chunk.Instructions[inst + 1];
                    //    if (next.OpCode == OpCode.SET_LOCAL)
                    //    {
                    //        _toRemove.Add((chunk, inst + 1));
                    //        nb3 = next.b1;
                    //    }
                    //}

                    //if the prevous is a getlocal take it's byte and put it as the second byte in the add
                    //  and mark it as for removal
                    if (prev.OpCode == OpCode.GET_LOCAL)
                    {
                        _optimiser.AddToRemove(chunk, inst - 1);
                        nb2 = prev.b1;
                        var prevprev = chunk.Instructions[inst - 2];
                        // if the previous previous is getlocal take its byte and make first byte, mark for removal
                        if (prevprev.OpCode == OpCode.GET_LOCAL)
                        {
                            _optimiser.AddToRemove(chunk, inst - 2);
                            nb1 = prevprev.b1;
                        }
                    }
                    break;
                case RegisteriseType.SetIndex:
                {
                    if (prev.OpCode == OpCode.GET_LOCAL)
                    {
                        _optimiser.AddToRemove(chunk, inst - 1);
                        nb3 = prev.b1;  //newval

                        var prevprev = chunk.Instructions[inst - 2];
                        if (prevprev.OpCode == OpCode.GET_LOCAL)
                        {
                            _optimiser.AddToRemove(chunk, inst - 2);
                            nb2 = prevprev.b1; // index

                            var prevprevprev = chunk.Instructions[inst - 3];
                            if (prevprevprev.OpCode == OpCode.GET_LOCAL)
                            {
                                _optimiser.AddToRemove(chunk, inst - 3);
                                nb1 = prevprevprev.b1;  // target
                            }
                        }
                    }
                }
                break;
                case RegisteriseType.Uniary:
                {
                    if (prev.OpCode == OpCode.GET_LOCAL)
                    {
                        _optimiser.AddToRemove(chunk, inst - 1);
                        nb1 = prev.b1;
                    }
                }
                break;
                case RegisteriseType.GetProp:
                    if (prev.OpCode == OpCode.GET_LOCAL)
                    {
                        _optimiser.AddToRemove(chunk, inst - 1);
                        nb3 = prev.b1;
                    }
                    break;
                case RegisteriseType.SetProp:
                    if (prev.OpCode == OpCode.GET_LOCAL)
                    {
                        _optimiser.AddToRemove(chunk, inst - 1);
                        nb3 = prev.b1;  //target

                        var prevprev = chunk.Instructions[inst - 2];
                        if (prevprev.OpCode == OpCode.GET_LOCAL)
                        {
                            _optimiser.AddToRemove(chunk, inst - 2);
                            nb2 = prevprev.b1; // newval
                        }
                    }
                    break;
                case RegisteriseType.Unknown:
                default:
                    throw new UloxException($"Unknown registerise type {regType}");
                }

                chunk.Instructions[inst] = new ByteCodePacket(original.OpCode, nb1, nb2, nb3);
            }
        }

        public void Clear()
        {
            _potentialRegisterise.Clear();
            _labelUsage.Clear();
            _gotos.Clear();
            _pops.Clear();
            _prevOoCode = OpCode.NONE;
            _deadCodeStart = -1;
        }
    }
}
