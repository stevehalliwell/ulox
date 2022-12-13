using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public sealed class ByteCodeOptimiser : ByteCodeIterator, IByteCodeOptimiser
    {
        public bool Enabled { get; set; } = true;
        private List<(Chunk chunk, int b)> _deadBytes = new List<(Chunk, int)>();
        private List<(Chunk chunk, int from, byte label)> _labelUsage = new List<(Chunk, int, byte)>();
        private OpCode _prevOoCode;
        private int _deadCodeStart = -1;

        public void Optimise(CompiledScript compiledScript)
        {
            if (!Enabled)
                return;

            Iterate(compiledScript);

            MarkUnsedLabelsAsDead(compiledScript);
            RemoveDeadBytes();
        }

        private void MarkUnsedLabelsAsDead(CompiledScript compiledScript)
        {
            foreach (var chunk in compiledScript.AllChunks)
            {
                foreach (var label in chunk.Labels)
                {
                    var used = _labelUsage.Any(x => x.chunk == chunk && x.label == label.Key);
                    if(!used)
                    {
                        _deadBytes.Add((chunk, label.Value));
                        _deadBytes.Add((chunk, label.Value+1));
                    }
                }
            }
        }

        private void RemoveDeadBytes()
        {
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

        protected override void DefaultOpCode(Chunk chunk, int i, OpCode opCode)
        {
            if (_prevOoCode == OpCode.GOTO
                && opCode != OpCode.LABEL)
            {
                _deadCodeStart = i;
            }

            if (opCode == OpCode.LABEL
                && _deadCodeStart != -1)
            {
                _deadBytes.AddRange(Enumerable.Range(_deadCodeStart, i - _deadCodeStart).Select(b => (chunk, b)));
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

        protected override void ProcessOpAndByte(Chunk chunk, OpCode opCode, byte b)
        {
        }

        protected override void ProcessOpAndStringConstant(Chunk chunk, OpCode opCode, byte sc, Value value)
        {
            if (opCode == OpCode.GOTO
                || opCode == OpCode.GOTO_IF_FALSE)
            {
                if (chunk.Labels[sc] - 1 != CurrentInstructionIndex)
                    _labelUsage.Add((chunk, CurrentInstructionIndex, sc));
                else
                {
                    _deadBytes.Add((chunk, CurrentInstructionIndex - 1));
                    _deadBytes.Add((chunk, CurrentInstructionIndex));
                }
            }
        }

        protected override void ProcessOpAndStringConstantAndByte(OpCode opCode, byte stringConstant, Value value, byte b)
        {

        }

        protected override void ProcessTypeOp(Chunk chunk, OpCode opCode, byte stringConstant, Value value, byte b, byte labelId)
        {
            _labelUsage.Add((chunk, CurrentInstructionIndex, labelId));
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

        protected override void ProcessTestOpAndStringConstantAndByte(OpCode opCode, TestOpType testOpType, byte stringConstant, Value value, byte b)
        {

        }

        protected override void ProcessTestOpAndStringConstantAndTestCount(OpCode opCode, byte stringConstantID, Value value, byte testCount)
        {

        }

        protected override void ProcessTestOpAndStringConstantAndTestCountAndTestIndexAndTestLabel(Chunk chunk, OpCode opCode, byte sc, Value value, byte testCount, int it, byte label)
        {
            _labelUsage.Add((chunk, CurrentInstructionIndex, label));
        }

        protected override void ProcessTestOpAndLabel(Chunk chunk, OpCode opCode, TestOpType testOpType, byte label)
        {
            _labelUsage.Add((chunk, CurrentInstructionIndex, label));
        }
    }
}
