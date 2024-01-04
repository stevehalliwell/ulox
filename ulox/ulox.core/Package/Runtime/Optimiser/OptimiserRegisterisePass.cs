﻿using System.Collections.Generic;
using static ULox.Optimiser;

namespace ULox
{
    public sealed class OptimiserRegisterisePass : IOptimiserPass
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

        private List<(int inst, RegisteriseType regType)> _potentialRegisterise = new List<(int, RegisteriseType)>();

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
            case OpCode.NEGATE:
            case OpCode.NOT:
            case OpCode.COUNT_OF:
            case OpCode.DUPLICATE:
                _potentialRegisterise.Add((inst, RegisteriseType.Uniary));
                break;
            case OpCode.GET_PROPERTY:
                _potentialRegisterise.Add((inst, RegisteriseType.GetProp));
                break;
            case OpCode.SET_PROPERTY:
                _potentialRegisterise.Add((inst, RegisteriseType.SetProp));
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
                        optimiser.AddToRemove(chunk, inst - 1);
                        nb2 = prev.b1;
                        var prevprev = chunk.Instructions[inst - 2];
                        // if the previous previous is getlocal take its byte and make first byte, mark for removal
                        if (prevprev.OpCode == OpCode.GET_LOCAL)
                        {
                            optimiser.AddToRemove(chunk, inst - 2);
                            nb1 = prevprev.b1;
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
                case RegisteriseType.Uniary:
                {
                    if (prev.OpCode == OpCode.GET_LOCAL)
                    {
                        optimiser.AddToRemove(chunk, inst - 1);
                        nb1 = prev.b1;
                    }
                }
                break;
                case RegisteriseType.GetProp:
                    if (prev.OpCode == OpCode.GET_LOCAL)
                    {
                        optimiser.AddToRemove(chunk, inst - 1);
                        nb3 = prev.b1;
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
