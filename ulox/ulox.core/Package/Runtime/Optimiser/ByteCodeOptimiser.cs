﻿using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public sealed class ByteCodeOptimiser : CompiledScriptIterator
    {
        public bool Enabled { get; set; } = true;
        public bool EnableRegisterisation { get; set; } = true;
        public bool EnableRemoveUnreachableLabels { get; set; } = false;
        private List<(Chunk chunk, int inst)> _toRemove = new List<(Chunk, int)>();
        private List<(Chunk chunk, int inst)> _potentialRegisterise = new List<(Chunk, int)>();
        private readonly List<(Chunk chunk, int from, byte label)> _labelUsage = new List<(Chunk, int, byte)>();
        private OpCode _prevOoCode;
        private int _deadCodeStart = -1;

        public void Optimise(CompiledScript compiledScript)
        {
            if (!Enabled)
                return;

            Iterate(compiledScript);
            if (EnableRegisterisation) AttemptRegisterise();
            if (EnableRemoveUnreachableLabels)
            {
                MarkNoJumpGotoLabelAsDead();
                MarkUnsedLabelsAsDead(compiledScript);
            }
            RemoveMarkedInstructions();
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
                AddRegisterOptimisableInstruction(CurrentChunk, CurrentInstructionIndex);
                break;
            }
        }

        private void AddRegisterOptimisableInstruction(Chunk currentChunk, int currentInstructionIndex)
        {
            _potentialRegisterise.Add((currentChunk, currentInstructionIndex));
        }

        private void AttemptRegisterise()
        {
            foreach (var (chunk, inst) in _potentialRegisterise)
            {
                var original = chunk.Instructions[inst];
                var nb1 = original.b1;
                var nb2 = original.b2;
                var nb3 = original.b3;

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

                var prev = chunk.Instructions[inst - 1];

                //if the prevous is a getlocal take it's byte and put it as the second byte in the add
                //  and mark it as for removal
                if (prev.OpCode == OpCode.GET_LOCAL)
                {
                    _toRemove.Add((chunk, inst - 1));
                    nb2 = prev.b1;
                    // if the previous previous is getlocal take its byte and make first byte, mark for removal
                    var prevprev = chunk.Instructions[inst - 2];
                    if (prevprev.OpCode == OpCode.GET_LOCAL)
                    {
                        _toRemove.Add((chunk, inst - 2));
                        nb1 = prevprev.b1;
                    }
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
