using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public sealed class ByteCodeOptimiser : CompiledScriptIterator
    {
        public const byte NOT_LOCAL_BYTE = byte.MaxValue;
        private enum RegisteriseType
        {
            Unknown,
            Uniary,
            Binary,
            SetIndex,
            GetProp,
            SetProp,
        }

        public bool Enabled { get; set; } = true;
        public bool EnableLocalizing { get; set; } = true;
        public bool EnableRemoveUnreachableLabels { get; set; } = false;

        public OptimisationReporter OptimisationReporter { get; set; }
        private List<(Chunk chunk, int inst)> _toRemove = new List<(Chunk, int)>();
        private List<(Chunk chunk, int inst, RegisteriseType regType)> _potentialRegisterise = new List<(Chunk, int, RegisteriseType)>();
        private readonly List<(Chunk chunk, int from, byte label)> _labelUsage = new List<(Chunk, int, byte)>();
        private OpCode _prevOoCode;
        private int _deadCodeStart = -1;

        public void Optimise(CompiledScript compiledScript)
        {
            if (!Enabled)
                return;

            OptimisationReporter?.PreOptimise(compiledScript);
            Iterate(compiledScript);
            if (EnableLocalizing) AttemptRegisterise();
            if (EnableRemoveUnreachableLabels)
            {
                MarkNoJumpGotoLabelAsDead();
                MarkUnsedLabelsAsDead(compiledScript);
            }
            RemoveMarkedInstructions();
            OptimisationReporter?.PostOptimise(compiledScript);
        }

        private void MarkNoJumpGotoLabelAsDead()
        {
            foreach (var (chunk, from, label) in _labelUsage)
            {
                var labelLoc = chunk.Labels[label];

                if (from - 1 >= labelLoc)
                    continue;

                var found = false;
                for (int i = from + 1; i < labelLoc; i++)
                {
                    if (!_toRemove.Any(d => d.chunk == chunk && d.inst == i))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _toRemove.Add((chunk, from));
                }
            }
        }

        private void MarkUnsedLabelsAsDead(CompiledScript compiledScript)
        {
            foreach (var chunk in compiledScript.AllChunks)
            {
                foreach (var label in chunk.Labels)
                {
                    var matches = _labelUsage.Where(x => x.chunk == chunk && x.label == label.Key);
                    var used = matches.Any(x => !_toRemove.Any(y => y.chunk == chunk && y.inst == x.from));
                    if (!used)
                    {
                        _toRemove.Add((chunk, label.Value));
                    }
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
        }

        public void Reset()
        {
            _toRemove.Clear();
            _potentialRegisterise.Clear();
            _labelUsage.Clear();
            _deadCodeStart = -1;
        }

        protected override void PostChunkIterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        protected override void PreChunkInterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        private void AddLabelUsage(byte labelId)
        {
            _labelUsage.Add((CurrentChunk, CurrentInstructionIndex, labelId));
        }

        protected override void ProcessPacket(ByteCodePacket packet)
        {
            var opCode = packet.OpCode;
            if (_deadCodeStart == -1
                && _prevOoCode == OpCode.GOTO
                && opCode != OpCode.LABEL)
            {
                _deadCodeStart = CurrentInstructionIndex;
            }

            if (opCode == OpCode.LABEL
                && _deadCodeStart != -1)
            {
                _toRemove.AddRange(Enumerable.Range(_deadCodeStart, CurrentInstructionIndex - _deadCodeStart).Select(b => (CurrentChunk, b)));
                _deadCodeStart = -1;
            }
            _prevOoCode = opCode;

            switch (packet.OpCode)
            {
            case OpCode.TYPE:
                AddLabelUsage(packet.typeDetails.initLabelId);
                break;
            case OpCode.TEST:
                if (packet.testOpDetails.TestOpType == TestOpType.TestFixtureBodyInstruction)
                    AddLabelUsage(packet.testOpDetails.b1);
                else if (packet.testOpDetails.TestOpType == TestOpType.TestCase)
                    AddLabelUsage(packet.testOpDetails.b2);
                break;
            case OpCode.GOTO:
            case OpCode.GOTO_IF_FALSE:
                ProcessGoto(packet);
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
            }
        }

        private void AttemptRegisterise()
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
                        _toRemove.Add((chunk, inst - 1));
                        nb2 = prev.b1;
                        var prevprev = chunk.Instructions[inst - 2];
                        // if the previous previous is getlocal take its byte and make first byte, mark for removal
                        if (prevprev.OpCode == OpCode.GET_LOCAL)
                        {
                            _toRemove.Add((chunk, inst - 2));
                            nb1 = prevprev.b1;
                        }
                    }
                    break;
                case RegisteriseType.SetIndex:
                {
                    if (prev.OpCode == OpCode.GET_LOCAL)
                    {
                        _toRemove.Add((chunk, inst - 1));
                        nb3 = prev.b1;  //newval

                        var prevprev = chunk.Instructions[inst - 2];
                        if (prevprev.OpCode == OpCode.GET_LOCAL)
                        {
                            _toRemove.Add((chunk, inst - 2));
                            nb2 = prevprev.b1; // index

                            var prevprevprev = chunk.Instructions[inst - 3];
                            if (prevprevprev.OpCode == OpCode.GET_LOCAL)
                            {
                                _toRemove.Add((chunk, inst - 3));
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
                        _toRemove.Add((chunk, inst - 1));
                        nb1 = prev.b1;
                    }
                }
                break;
                case RegisteriseType.GetProp:
                    if (prev.OpCode == OpCode.GET_LOCAL)
                    {
                        _toRemove.Add((chunk, inst - 1));
                        nb3 = prev.b1;
                    }
                    break;
                case RegisteriseType.SetProp:
                    if (prev.OpCode == OpCode.GET_LOCAL)
                    {
                        _toRemove.Add((chunk, inst - 1));
                        nb3 = prev.b1;  //target

                        var prevprev = chunk.Instructions[inst - 2];
                        if (prevprev.OpCode == OpCode.GET_LOCAL)
                        {
                            _toRemove.Add((chunk, inst - 2));
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

        private void ProcessGoto(ByteCodePacket packet)
        {
            var endingLocation = CurrentChunk.Labels[packet.b1];
            if (endingLocation != CurrentInstructionIndex + 1)
                AddLabelUsage(packet.b1);
            else
                _toRemove.Add((CurrentChunk, CurrentInstructionIndex));
        }
    }
}
