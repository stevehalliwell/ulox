using System.Collections.Generic;
using static ULox.Optimiser;

namespace ULox
{
    //todo if we did all get set props first we would remove all the get local 0, afterwhich we could do lhs or rhs registerise on binary ops
    //todo if a get prop is followed by a set local we can hold the setlocal loc in the getprop
    public sealed class OptimiserRegisterisePass : IOptimiserPass
    {
        private enum RegisteriseType
        {
            Unknown,
            Binary,
            SetIndex,
            SetProp,
            GetProp,
        }

        private List<(int inst, RegisteriseType regType)> _potentialRegisterise = new();

        public void Prepare(Optimiser optimiser, Chunk chunk)
        {
            _potentialRegisterise.Clear();
        }

        public void ProcessPacket(Optimiser optimiser, Chunk chunk, int inst, ByteCodePacket packet)
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
                _potentialRegisterise.Add((inst, RegisteriseType.Binary));
                break;
            case OpCode.SET_INDEX:
                _potentialRegisterise.Add((inst, RegisteriseType.SetIndex));
                break;
            case OpCode.SET_PROPERTY:
                _potentialRegisterise.Add((inst, RegisteriseType.SetProp));
                break;
                case OpCode.GET_PROPERTY:
                _potentialRegisterise.Add((inst, RegisteriseType.GetProp));
                break;
            }
        }

        public PassCompleteRequest Complete(Optimiser optimiser, Chunk chunk)
        {
            foreach (var (inst, regType) in _potentialRegisterise)
            {
                var original = chunk.Instructions[inst];
                var nb1 = original.b1;
                var nb2 = original.b2;
                var nb3 = original.b3;

                if (inst == 0)
                    continue;

                var prev = chunk.Instructions[inst - 1];

                switch (regType)
                {
                case RegisteriseType.Binary:
                    //TODO: would like to but it conflicts with add overload internals at the moment
                    // either way we would need to have all the binary ops check the set byte for local or stack
                    //if the following is a set local we can just do that
                    if (chunk.Instructions.Count > inst)
                    {
                        var next = chunk.Instructions[inst + 1];
                        if (next.OpCode == OpCode.SET_LOCAL && next.b1 != Optimiser.NOT_LOCAL_BYTE)
                        {
                            optimiser.AddToRemove(chunk, inst + 1);
                            nb3 = next.b1;
                        }
                    }

                    //if the prevous is a getlocal take it's byte and put it as the second byte in the add
                    //  and mark it as for removal
                    if (prev.OpCode == OpCode.GET_LOCAL)
                    {
                        optimiser.AddToRemove(chunk, inst - 1);
                        nb2 = prev.b1;
                        if (inst > 1)
                        {
                            var prevprev = chunk.Instructions[inst - 2];
                            // if the previous previous is getlocal take its byte and make first byte, mark for removal
                            if (prevprev.OpCode == OpCode.GET_LOCAL)
                            {
                                optimiser.AddToRemove(chunk, inst - 2);
                                nb1 = prevprev.b1;
                            }
                        }
                    }

                    break;
                case RegisteriseType.SetIndex:
                {
                    if (prev.OpCode == OpCode.GET_LOCAL)
                    {
                        optimiser.AddToRemove(chunk, inst - 1);
                        nb3 = prev.b1;  //newval

                        var prevprev = chunk.Instructions[inst - 2];
                        if (prevprev.OpCode == OpCode.GET_LOCAL)
                        {
                            optimiser.AddToRemove(chunk, inst - 2);
                            nb2 = prevprev.b1; // index

                            var prevprevprev = chunk.Instructions[inst - 3];
                            if (prevprevprev.OpCode == OpCode.GET_LOCAL)
                            {
                                optimiser.AddToRemove(chunk, inst - 3);
                                nb1 = prevprevprev.b1;  // target
                            }
                        }
                    }
                }
                break;
                case RegisteriseType.SetProp:
                    if (prev.OpCode == OpCode.GET_LOCAL)
                    {
                        optimiser.AddToRemove(chunk, inst - 1);
                        nb3 = prev.b1;  //target

                        var prevprev = chunk.Instructions[inst - 2];
                        if (prevprev.OpCode == OpCode.GET_LOCAL)
                        {
                            optimiser.AddToRemove(chunk, inst - 2);
                            nb2 = prevprev.b1; // newval
                        }
                    }
                    break;
                case RegisteriseType.GetProp:
                        {
                            //detecting a var a = obj.prop; pattern to registerise the getprop
                            //we put the setlocal byte into the getprop and remove the setlocal and pop
                            var nextInst = inst + 1;
                            if (chunk.Instructions.Count > nextInst)
                            {
                                var next = chunk.Instructions[nextInst];
                                if (next.OpCode == OpCode.SET_LOCAL)
                                {
                                    var nextNextInst = nextInst + 1;
                                    if (chunk.Instructions.Count > nextNextInst)
                                    {
                                        var nextNext = chunk.Instructions[nextNextInst];
                                        if (nextNext.OpCode == OpCode.POP && nextNext.b1 == 1)
                                        {
                                            optimiser.AddToRemove(chunk, nextNextInst);
                                            optimiser.AddToRemove(chunk, nextInst);
                                            nb2 = next.b1; // setlocal byte
                                        }
                                    }
                                }
                            }
                        }
                    break;
                case RegisteriseType.Unknown:
                default:
                    throw new UloxException($"Unknown registerise type {regType}");
                }

                chunk.Instructions[inst] = new ByteCodePacket(original.OpCode, nb1, nb2, nb3);
            }

            return PassCompleteRequest.None;
        }
    }
}
