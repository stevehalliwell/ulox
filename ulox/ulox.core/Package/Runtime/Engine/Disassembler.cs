using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace ULox
{
    public sealed class Disassembler : ByteCodeIterator
    {
        protected Func<Chunk, int, int>[] OpCodeHandlers { get; set; } = new Func<Chunk, int, int>[Enum.GetValues(typeof(OpCode)).Length];

        private readonly StringBuilder stringBuilder = new StringBuilder();
        private int _currentInstructionCount;
        private int _prevLine;

        public string GetString() => stringBuilder.ToString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoLabels(Chunk chunk)
        {
            if (chunk.Labels.Count > 0)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("--labels--");

                foreach (var label in chunk.Labels)
                {
                    stringBuilder.AppendLine($"{chunk.ReadConstant(label.Key)} = {label.Value}");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoArgs(Chunk chunk)
        {
            if (chunk.Arity == 0)
                return;

            stringBuilder.AppendLine();
            stringBuilder.Append("arguments: ");

            var argsConstants = chunk.ArgumentConstantIds;

            var constantStrings = argsConstants.Select(x => chunk.Constants[x].val.asString);
            var joinedArgStringConstants = string.Join(", ", constantStrings);

            stringBuilder.AppendLine(joinedArgStringConstants);
            stringBuilder.AppendLine();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoLineNumber(Chunk chunk)
        {
            var lineForInst = chunk.GetLineForInstruction(_currentInstructionCount);

            if (lineForInst != _prevLine)
            {
                stringBuilder.Append($" {lineForInst} ");
                _prevLine = lineForInst;
            }
            else
            {
                stringBuilder.Append($" | ");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AppendSpace()
        {
            stringBuilder.Append(" ");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Append(string s)
        {
            stringBuilder.Append(s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoConstants(Chunk chunk)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("--constants--");

            var constants = chunk.Constants;

            for (int i = 0; i < constants.Count; i++)
            {
                var v = constants[i];

                stringBuilder.Append(i.ToString("000"));
                stringBuilder.Append("  ");
                stringBuilder.Append(v.ToString());
                stringBuilder.AppendLine();
            }
        }

        protected override void ProcessTestOp(OpCode opCode, TestOpType testOpType)
        {
            Append(testOpType.ToString());
            AppendSpace();
        }

        protected override void ProcessTestOpAndStringConstantAndByte(OpCode opCode, TestOpType testOpType, byte stringConstant, byte b)
        {
            stringBuilder.Append($"({stringConstant}){CurrentChunk.Constants[stringConstant]}");
            AppendSpace();
            stringBuilder.Append($"({b})");
            AppendSpace();
        }

        protected override void ProcessTestOpAndStringConstantAndTestCountAndTestIndexAndTestLabel(OpCode opCode, byte sc, byte testCount, int it, byte label)
        {
            PrintLabel(label);
            if (it < testCount - 1)
                Append(", ");
            else
                Append(" ] ");
        }

        protected override void ProcessTestOpAndByteAndByte(OpCode opCode, TestOpType testOpType, byte b1, byte b2)
        {
            stringBuilder.Append($"({b1})");
            AppendSpace();
            stringBuilder.Append($"({b2})");
            AppendSpace();
        }

        protected override void ProcessTestOpAndStringConstantAndTestCount(OpCode opCode, byte stringConstantID, byte testCount)
        {
            stringBuilder.Append($"({stringConstantID}){CurrentChunk.Constants[stringConstantID]}");
            Append("  [");
            AppendSpace();
        }

        protected override void ProcessTestOpAndLabel(OpCode opCode, TestOpType testOpType, byte labelId)
        {
            PrintLabel(labelId);
            AppendSpace();
        }

        protected override void ProcessOpClosure(OpCode opCode, byte funcID, Chunk asChunk, int upValueCount)
        {
            stringBuilder.Append($"({funcID})" + asChunk.ToString());
            if (upValueCount > 0)
                stringBuilder.AppendLine();
        }

        protected override void ProcessOpClosureUpValue(
            OpCode opCode,
            byte fundID,
            int count,
            int upVal,
            byte isLocal,
            byte upvalIndex)
        {
            stringBuilder.Append($"     {(isLocal == 1 ? "local" : "upvalue")} {upvalIndex}");
            if (upVal < count - 1)
                stringBuilder.AppendLine();
        }

        protected override void ProcessTypeOp(OpCode opCode, byte stringConstant, byte b, byte initLabel)
        {
            stringBuilder.Append($"({stringConstant}){CurrentChunk.Constants[stringConstant]}");
            AppendSpace();
            stringBuilder.Append($"({b})");
            AppendSpace();
            PrintLabel(initLabel);
            AppendSpace();
        }

        protected override void ProcessOpAndStringConstant(OpCode opCode, byte sc)
        {
            stringBuilder.Append($"({sc}){CurrentChunk.Constants[sc]}");
        }

        protected override void ProcessOpAndByte(OpCode opCode, byte b)
        {
            stringBuilder.Append($"({b})");
        }

        protected override void ProcessOp(OpCode opCode)
        {
        }

        protected override void DefaultOpCode(OpCode opCode)
        {
            stringBuilder.Append(CurrentInstructionIndex.ToString("00000"));
            DoLineNumber(CurrentChunk);
            stringBuilder.Append(opCode);
            AppendSpace();
        }

        protected override void DefaultPostOpCode()
        {
            stringBuilder.AppendLine();
            _currentInstructionCount++;
        }

        protected override void PostChunkIterate(CompiledScript compiledScript, Chunk chunk)
        {
            DoLabels(chunk);
            DoConstants(chunk);

            if (chunk == compiledScript.TopLevelChunk)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("####");
                stringBuilder.AppendLine();
            }
        }

        protected override void PreChunkInterate(CompiledScript compiledScript, Chunk chunk)
        {
            stringBuilder.AppendLine(chunk.Name);

            DoArgs(chunk);

            _currentInstructionCount = 0;
            _prevLine = -1;
        }

        private void PrintLabel(byte labelID)
        {
            stringBuilder.Append($"({labelID}){CurrentChunk.Constants[labelID]}@{CurrentChunk.Labels[labelID]}");
        }

        protected override void ProcessPacket(ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.NONE:
                break;
            case OpCode.CONSTANT:
                DoConstant(packet);
            break;
            case OpCode.NULL:
                break;
            case OpCode.PUSH_BOOL:
                stringBuilder.Append($"({packet.BoolValue})");
                break;
            case OpCode.PUSH_BYTE:
                stringBuilder.Append($"({packet.b1})");
                break;
            case OpCode.POP:
                break;
            case OpCode.SWAP:
                break;
            case OpCode.DUPLICATE:
                break;
            case OpCode.DEFINE_GLOBAL:
                DoConstant(packet);
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
                stringBuilder.Append($"({packet.u1})");
                break;
            case OpCode.JUMP:
                stringBuilder.Append($"({packet.u1})");
                break;
            case OpCode.LOOP:
                stringBuilder.Append($"({packet.u1})");
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
                stringBuilder.Append($"({packet.b1})");
                break;
            case OpCode.CLOSURE:
                break;
            case OpCode.NATIVE_CALL:
                break;
            case OpCode.RETURN:
                stringBuilder.Append($"({packet.ReturnMode})");
                break;
            case OpCode.YIELD:
                break;
            case OpCode.THROW:
                break;
            case OpCode.VALIDATE:
                stringBuilder.Append($"({packet.ValidateOp})");
                break;
            case OpCode.TYPE:
                break;
            case OpCode.GET_PROPERTY:
                DoConstant(packet);
                break;
            case OpCode.SET_PROPERTY:
                DoConstant(packet);
                break;
            case OpCode.METHOD:
                DoConstant(packet);
                break;
            case OpCode.FIELD:
                DoConstant(packet);
                break;
            case OpCode.INVOKE:
            {
                var sc = packet.b1;
                var b = packet.b2;
                stringBuilder.Append($"({sc}){CurrentChunk.Constants[sc]}");
                AppendSpace();
                stringBuilder.Append($"({b})");
                AppendSpace();
            }
            break;
            case OpCode.FREEZE:
                break;
            case OpCode.MIXIN:
                break;
            case OpCode.TEST:
                break;
            case OpCode.BUILD:
                break;
            case OpCode.REGISTER:
                DoConstant(packet);
                break;
            case OpCode.INJECT:
                DoConstant(packet);
                break;
            case OpCode.NATIVE_TYPE:
                stringBuilder.Append($"({packet.NativeType})");
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
                PrintLabel(packet.b1);
                break;
            case OpCode.GOTO_IF_FALSE:
                PrintLabel(packet.b1);
                break;
            case OpCode.LABEL:
                PrintLabel(packet.b1);
                break;
            case OpCode.ENUM_VALUE:
                break;
            case OpCode.READ_ONLY:
                break;
            default:
                break;
            }
        }

        private void DoConstant(ByteCodePacket packet)
        {
            var sc = packet.b1;
            stringBuilder.Append($"({sc}){CurrentChunk.Constants[sc]}");
        }
    }
}
