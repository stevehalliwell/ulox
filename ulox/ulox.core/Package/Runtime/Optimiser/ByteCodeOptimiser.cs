using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public sealed class ByteCodeOptimiser : CompiledScriptIterator
    {
        public bool Enabled { get; set; } = false;
        private List<(Chunk chunk, int inst)> _toRemove = new List<(Chunk, int)>();
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
            RemoveMarkedInstructions();
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
                    if (!_toRemove.Any(d => d.chunk == labelUsage.chunk && d.inst == i))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _toRemove.Add((labelUsage.chunk, labelUsage.from));
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
                ProcessGoto(packet);
                break;
            case OpCode.GOTO_IF_FALSE:
                ProcessGoto(packet);
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
