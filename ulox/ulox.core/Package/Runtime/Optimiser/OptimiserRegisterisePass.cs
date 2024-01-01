using System.Collections.Generic;

namespace ULox
{
    public sealed class OptimiserRegisterisePass : CompiledScriptIterator, IOptimiserPass
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

        private Optimiser _optimiser;
        private List<(Chunk chunk, int inst, RegisteriseType regType)> _potentialRegisterise = new List<(Chunk, int, RegisteriseType)>();

        public void Run(Optimiser optimiser, CompiledScript compiledScript)
        {
            _optimiser = optimiser;
            Iterate(compiledScript);
            ProcessRegisterise();
        }

        protected override void PreChunkInterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        protected override void ProcessPacket(ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
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

        protected override void PostChunkIterate(CompiledScript compiledScript, Chunk chunk)
        {
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

        public void Reset()
        {
            _potentialRegisterise.Clear();
        }
    }
}
