using System;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using static ULox.CompilerState;

namespace ULox
{
    public sealed class Disassembler : CompiledScriptIterator
    {
        private Func<Chunk, int, int>[] OpCodeHandlers { get; set; } = new Func<Chunk, int, int>[Enum.GetValues(typeof(OpCode)).Length];

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
            stringBuilder.Append(CurrentInstructionIndex.ToString("00000"));
            DoLineNumber(CurrentChunk);
            stringBuilder.Append(packet.OpCode);
            AppendSpace();

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
                DoConstant(packet);
                break;
            case OpCode.ASSIGN_GLOBAL:
                DoConstant(packet);
                break;
            case OpCode.GET_LOCAL:
                stringBuilder.Append($"({packet.b1})");
                break;
            case OpCode.SET_LOCAL:
                stringBuilder.Append($"({packet.b1})");
                break;
            case OpCode.GET_UPVALUE:
                stringBuilder.Append($"({packet.b1})");
                break;
            case OpCode.SET_UPVALUE:
                stringBuilder.Append($"({packet.b1})");
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
            {
                //todo
                stringBuilder.Append($"({packet.closureDetails.ClosureType}) ");
                switch (packet.closureDetails.ClosureType)
                {
                case ClosureType.Closure:
                    var funcID = packet.closureDetails.b1;
                    var asChunk = CurrentChunk.Constants[funcID].val.asChunk;
                    stringBuilder.Append($"({funcID}) {asChunk}  upvals:{packet.closureDetails.b2})");
                    break;
                case ClosureType.UpValueInfo:
                    stringBuilder.Append($"{(packet.closureDetails.b1 == 1 ? "local" : "upvalue")} {packet.closureDetails.b2}");
                    break;
                default:
                    break;
                }
            }
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
            {
                var sc = packet.typeDetails.stringConstantId;
                stringBuilder.Append($"({sc}){CurrentChunk.Constants[sc]}");
                AppendSpace();
                stringBuilder.Append($"({packet.typeDetails.UserType})");
                AppendSpace();
                PrintLabel(packet.typeDetails.initLabelId);
                AppendSpace();
            }
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
            {
                var testOpType = packet.testOpDetails.TestOpType;
                Append(testOpType.ToString());
                AppendSpace();

                switch (testOpType)
                {
                case TestOpType.TestFixtureBodyInstruction:
                    PrintLabel(packet.testOpDetails.b1);
                    AppendSpace();
                    break;
                case TestOpType.TestCase:
                {
                    var label = packet.testOpDetails.b1;
                    var stringConstant = packet.testOpDetails.b2;
                    stringBuilder.Append($"({stringConstant}){CurrentChunk.Constants[stringConstant]}");
                    AppendSpace();
                    PrintLabel(label);
                }
                break;
                case TestOpType.CaseStart:
                case TestOpType.CaseEnd:
                {
                    var stringConstant = packet.testOpDetails.b1;
                    var b = packet.testOpDetails.b2;
                    stringBuilder.Append($"({stringConstant}){CurrentChunk.Constants[stringConstant]}");
                    AppendSpace();
                    stringBuilder.Append($"({b})");
                    AppendSpace();
                }
                break;
                case TestOpType.TestSetEnd:
                    break;
                default:
                    break;
                }
            }
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
                throw new UloxException($"Unhandled OpCode '{packet.OpCode}'.");
            }


            stringBuilder.AppendLine();
            _currentInstructionCount++;
        }

        private void DoConstant(ByteCodePacket packet)
        {
            var sc = packet.b1;
            stringBuilder.Append($"({sc}){CurrentChunk.Constants[sc]}");
        }
    }
}
