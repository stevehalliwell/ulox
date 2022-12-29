using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public sealed class ByteCodeOptimiser : ByteCodeIterator
    {
        public bool Enabled { get; set; } = false;
        private List<(Chunk chunk, int b)> _deadBytes = new List<(Chunk, int)>();
        private List<(Chunk chunk, int from, byte label)> _labelUsage = new List<(Chunk, int, byte)>();
        private OpCode _prevOoCode;
        private int _deadCodeStart = -1;

        public void Optimise(CompiledScript compiledScript)
        {
            if (!Enabled)
                return;

            Iterate(compiledScript);

            MarkNoJumpGotoLabelAsDead(compiledScript);
            MarkUnsedLabelsAsDead(compiledScript);
            RemoveDeadBytes();
        }

        private void MarkNoJumpGotoLabelAsDead(CompiledScript compiledScript)
        {
            foreach (var labelUsage in _labelUsage)
            {
                var labelLoc = labelUsage.chunk.Labels[labelUsage.label];

                if (labelUsage.from - 1 >= labelLoc)
                    continue;

                var found = false;
                for (int i = labelUsage.from + 1; i < labelLoc; i++)
                {
                    if (!_deadBytes.Any(d => d.chunk == labelUsage.chunk && d.b == i))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _deadBytes.Add((labelUsage.chunk, labelUsage.from - 1));
                    _deadBytes.Add((labelUsage.chunk, labelUsage.from));
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
                    var used = matches.Any(x => !_deadBytes.Any(y => y.chunk == chunk && y.b == x.from));
                    if (!used)
                    {
                        _deadBytes.Add((chunk, label.Value));
                        _deadBytes.Add((chunk, label.Value + 1));
                    }
                }
            }
        }

        private void RemoveDeadBytes()
        {
            _deadBytes.Sort((x, y) => x.b.CompareTo(y.b));
            _deadBytes = _deadBytes.Distinct().ToList();

            for (int i = _deadBytes.Count - 1; i >= 0; i--)
            {
                var (chunk, b) = _deadBytes[i];
                chunk.RemoveByteAt(b);
            }
        }

        public void Reset()
        {
            _deadBytes.Clear();
            _deadCodeStart = -1;
        }

        protected override void DefaultOpCode(OpCode opCode)
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
                _deadBytes.AddRange(Enumerable.Range(_deadCodeStart, CurrentInstructionIndex - _deadCodeStart).Select(b => (CurrentChunk, b)));
                _deadCodeStart = -1;
            }
            _prevOoCode = opCode;
        }

        protected override void DefaultPostOpCode()
        {
        }

        protected override void PostChunkIterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        protected override void PreChunkInterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        protected override void ProcessOp(OpCode opCode)
        {
        }

        protected override void ProcessOpAndByte(OpCode opCode, byte b)
        {
        }

        protected override void ProcessOpAndStringConstant(OpCode opCode, byte sc)
        {
        }

        protected override void ProcessOpAndStringConstantAndByte(OpCode opCode, byte stringConstant, byte b)
        {
        }

        protected override void ProcessTypeOp(OpCode opCode, byte stringConstant, byte b, byte labelId)
        {
            AddLabelUsage(labelId);
        }

        protected override void ProcessOpAndUShort(OpCode opCode, ushort ushortValue)
        {
        }

        protected override void ProcessOpClosure(OpCode opCode, byte funcID, Chunk asChunk, int upValueCount)
        {
        }

        protected override void ProcessOpClosureUpValue(OpCode opCode, byte fundID, int count, int upVal, byte isLocal, byte upvalIndex)
        {
        }

        protected override void ProcessTestOp(OpCode opCode, TestOpType testOpType)
        {
        }

        protected override void ProcessTestOpAndByteAndByte(OpCode opCode, TestOpType testOpType, byte b1, byte b2)
        {
        }

        protected override void ProcessTestOpAndStringConstantAndByte(OpCode opCode, TestOpType testOpType, byte stringConstant, byte b)
        {
        }

        protected override void ProcessTestOpAndStringConstantAndTestCount(OpCode opCode, byte stringConstantID, byte testCount)
        {
        }

        protected override void ProcessTestOpAndStringConstantAndTestCountAndTestIndexAndTestLabel(OpCode opCode, byte sc, byte testCount, int it, byte label)
        {
            AddLabelUsage(label);
        }

        private void AddLabelUsage(byte labelId)
        {
            _labelUsage.Add((CurrentChunk, CurrentInstructionIndex, labelId));
        }

        protected override void ProcessTestOpAndLabel(OpCode opCode, TestOpType testOpType, byte label)
        {
            AddLabelUsage(label);
        }

        protected override void ProcessOpAndLabel(OpCode opCode, byte labelId)
        {
            if (opCode == OpCode.GOTO
                || opCode == OpCode.GOTO_IF_FALSE)
            {
                AddLabelUsage(labelId);
            }
        }

        protected override void ProcessPacket(ByteCodePacket packet)
        {
            //nothing for now
        }
    }
}
