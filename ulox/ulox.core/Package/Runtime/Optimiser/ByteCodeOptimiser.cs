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

        private void AddLabelUsage(byte labelId)
        {
            _labelUsage.Add((CurrentChunk, CurrentInstructionIndex, labelId));
        }

        protected override void ProcessPacket(ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.NONE:
                break;
            case OpCode.CONSTANT:
                break;
            case OpCode.NULL:
                break;
            case OpCode.PUSH_BOOL:
                break;
            case OpCode.PUSH_BYTE:
                break;
            case OpCode.POP:
                break;
            case OpCode.SWAP:
                break;
            case OpCode.DUPLICATE:
                break;
            case OpCode.DEFINE_GLOBAL:
                break;
            case OpCode.FETCH_GLOBAL:
                break;
            case OpCode.ASSIGN_GLOBAL:
                break;
            case OpCode.GET_LOCAL:
                break;
            case OpCode.SET_LOCAL:
                break;
            case OpCode.GET_UPVALUE:
                break;
            case OpCode.SET_UPVALUE:
                break;
            case OpCode.CLOSE_UPVALUE:
                break;
            case OpCode.JUMP_IF_FALSE:
                break;
            case OpCode.JUMP:
                break;
            case OpCode.LOOP:
                break;
            case OpCode.NOT:
                break;
            case OpCode.EQUAL:
                break;
            case OpCode.LESS:
                break;
            case OpCode.GREATER:
                break;
            case OpCode.NEGATE:
                break;
            case OpCode.ADD:
                break;
            case OpCode.SUBTRACT:
                break;
            case OpCode.MULTIPLY:
                break;
            case OpCode.DIVIDE:
                break;
            case OpCode.MODULUS:
                break;
            case OpCode.CALL:
                break;
            case OpCode.CLOSURE:
                break;
            case OpCode.NATIVE_CALL:
                break;
            case OpCode.RETURN:
                break;
            case OpCode.YIELD:
                break;
            case OpCode.THROW:
                break;
            case OpCode.VALIDATE:
                break;
            case OpCode.TYPE:
                AddLabelUsage(packet.typeDetails.initLabelId);
                break;
            case OpCode.GET_PROPERTY:
                break;
            case OpCode.SET_PROPERTY:
                break;
            case OpCode.METHOD:
                break;
            case OpCode.FIELD:
                break;
            case OpCode.INVOKE:
                break;
            case OpCode.FREEZE:
                break;
            case OpCode.MIXIN:
                break;
            case OpCode.TEST:
                if(packet.testOpDetails.TestOpType == TestOpType.TestFixtureBodyInstruction)
                    AddLabelUsage(packet.testOpDetails.b1);
                else if (packet.testOpDetails.TestOpType == TestOpType.TestCase)
                    AddLabelUsage(packet.testOpDetails.b2);
                break;
            case OpCode.BUILD:
                break;
            case OpCode.REGISTER:
                break;
            case OpCode.INJECT:
                break;
            case OpCode.NATIVE_TYPE:
                break;
            case OpCode.GET_INDEX:
                break;
            case OpCode.SET_INDEX:
                break;
            case OpCode.EXPAND_COPY_TO_STACK:
                break;
            case OpCode.TYPEOF:
                break;
            case OpCode.MEETS:
                break;
            case OpCode.SIGNS:
                break;
            case OpCode.COUNT_OF:
                break;
            case OpCode.EXPECT:
                break;
            case OpCode.GOTO:
                AddLabelUsage(packet.b1);
                break;
            case OpCode.GOTO_IF_FALSE:
                AddLabelUsage(packet.b1);
                break;
            case OpCode.LABEL:
                break;
            case OpCode.ENUM_VALUE:
                break;
            case OpCode.READ_ONLY:
                break;
            default:
                break;
            }
        }
    }
}
