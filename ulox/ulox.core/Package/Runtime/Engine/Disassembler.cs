using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace ULox
{
    public sealed class Disassembler : CompiledScriptIterator
    {
        private readonly StringBuilder stringBuilder = new();
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
                    stringBuilder.AppendLine($"{chunk.ReadConstant(label.Key)} = {chunk.GetLabelPosition(label.Key)}");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoArgs(Chunk chunk)
        {
            if (chunk.ArgumentConstantIds.Count == 0)
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
            stringBuilder.Append(' ');
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
            else
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine();
            }
        }

        protected override void PreChunkInterate(CompiledScript compiledScript, Chunk chunk)
        {
            stringBuilder.AppendLine(chunk.FullName);

            DoArgs(chunk);

            _currentInstructionCount = 0;
            _prevLine = -1;
        }

        private void PrintLabel(byte labelID)
        {
            stringBuilder.Append($"({labelID}){CurrentChunk.Constants[labelID]}@{CurrentChunk.GetLabelPosition(labelID)}");
        }

        protected override void ProcessPacket(ByteCodePacket packet)
        {
            stringBuilder.Append(CurrentInstructionIndex.ToString("00000"));
            DoLineNumber(CurrentChunk);
            stringBuilder.Append(packet.OpCode);
            AppendSpace();

            switch (packet.OpCode)
            {
            case OpCode.PUSH_CONSTANT:
            case OpCode.DEFINE_GLOBAL:
            case OpCode.FETCH_GLOBAL:
            case OpCode.ASSIGN_GLOBAL:
                DoConstant(packet);
                break;
            case OpCode.SET_PROPERTY:
                DoConstant(packet);
                AppendOptionalTwoLocals(packet.b2, packet.b3);
                break;
            case OpCode.GET_PROPERTY:
                DoConstant(packet);
                AppendSingleLocalByte(packet.b3);
                break;
            case OpCode.POP:
            case OpCode.SET_LOCAL:
            case OpCode.GET_UPVALUE:
            case OpCode.SET_UPVALUE:
            case OpCode.CALL:
                stringBuilder.Append($"({packet.b1})");
                break;
            case OpCode.CLOSURE:
            {
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
                }
            }
            break;
            case OpCode.VALIDATE:
                stringBuilder.Append($"({packet.ValidateOp})");
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
                }
            }
            break;
            case OpCode.NATIVE_TYPE:
                stringBuilder.Append($"({packet.NativeType})");
                break;
            case OpCode.GOTO:
            case OpCode.GOTO_IF_FALSE:
                PrintLabel(packet.b1);
                break;
            case OpCode.ADD:
            case OpCode.SUBTRACT:
            case OpCode.MULTIPLY:
            case OpCode.DIVIDE:
            case OpCode.MODULUS:
            case OpCode.EQUAL:
            case OpCode.LESS:
            case OpCode.GREATER:
            case OpCode.GET_INDEX:
                AppendOptionalTwoLocalsAndOptionalLocalSet(packet.b1, packet.b2, packet.b3);
                break;
            case OpCode.SET_INDEX:
                AppendOptionalRegistersSetIndex(packet.b1, packet.b2, packet.b3);
                break;
            case OpCode.NEGATE:
            case OpCode.NOT:
            case OpCode.COUNT_OF:
            case OpCode.DUPLICATE:
                AppendSingleLocalByte(packet.b1);
                break;
            case OpCode.PUSH_VALUE:
            {
                var pushValueOpType = (PushValueOpType)packet.b1;
                stringBuilder.Append($"({pushValueOpType})");
                switch (pushValueOpType)
                {
                case PushValueOpType.Null:
                    break;
                case PushValueOpType.Bool:
                    stringBuilder.Append($"({packet.b2 == 1})");
                    break;
                case PushValueOpType.Byte:
                    stringBuilder.Append($"({packet.b2})");
                    break;
                case PushValueOpType.Bytes:
                    stringBuilder.Append($"({packet.b2},{packet.b3})");
                    break;
                }
            }
            break;

            case OpCode.GET_LOCAL:
            {
                var b1 = packet.b1;
                var b2 = packet.b2;
                var b3 = packet.b3;
                if (b3 != Optimiser.NOT_LOCAL_BYTE)
                {
                    stringBuilder.Append($"({b1},{b2},{b3})");
                }
                else if (b2 != Optimiser.NOT_LOCAL_BYTE)
                {
                    stringBuilder.Append($"({b1},{b2})");
                }
                else
                {
                    stringBuilder.Append($"({b1})");
                }
            }
            break;
            case OpCode.MULTI_VAR:
            {
                var b1 = packet.b1;
                var b2 = packet.b2;
                if (b1 == 1)
                    stringBuilder.Append($"start");
                else if (b1 == 0)
                    stringBuilder.Append($"end ({b2})");
                break;
            }
            }


            stringBuilder.AppendLine();

            AppendAnyLabels();

            _currentInstructionCount++;
        }

        private void AppendAnyLabels()
        {
            var labelIds = CurrentChunk.Labels
                .Where(x => x.Value == CurrentInstructionIndex)
                .Select(x => x.Key);

            if (labelIds.Any())
            {
                stringBuilder.Append(" <-- Label: ");
                stringBuilder.AppendLine(string.Join(", ", labelIds.Select(x => CurrentChunk.Constants[x])));
            }
        }

        private void AppendSingleLocalByte(byte b)
        {
            if (b != Optimiser.NOT_LOCAL_BYTE)
                stringBuilder.Append($" ({b})");
        }

        private void AppendOptionalTwoLocals(byte b1, byte b2)
        {
            if (b1 == Optimiser.NOT_LOCAL_BYTE && b2 == Optimiser.NOT_LOCAL_BYTE)
                return;

            stringBuilder.Append($" ({((b1 == Optimiser.NOT_LOCAL_BYTE) ? "_" : b1.ToString())}, " +
                $"{((b2 == Optimiser.NOT_LOCAL_BYTE) ? "_" : b2.ToString())})");
        }

        private void AppendOptionalTwoLocalsAndOptionalLocalSet(byte b1, byte b2, byte b3)
        {
            if (b1 == Optimiser.NOT_LOCAL_BYTE && b2 == Optimiser.NOT_LOCAL_BYTE)
                return;

            stringBuilder.Append($" ({((b1 == Optimiser.NOT_LOCAL_BYTE) ? "_" : b1.ToString())}, " +
                $"{((b2 == Optimiser.NOT_LOCAL_BYTE) ? "_" : b2.ToString())})" +
                $"{((b3 != Optimiser.NOT_LOCAL_BYTE) ? $" -> {b3}" : "")}");
        }

        private void AppendOptionalRegistersSetIndex(byte b1, byte b2, byte b3)
        {
            if (b1 == Optimiser.NOT_LOCAL_BYTE && b2 == Optimiser.NOT_LOCAL_BYTE && b3 == Optimiser.NOT_LOCAL_BYTE)
                return;

            stringBuilder.Append($" ({((b1 == Optimiser.NOT_LOCAL_BYTE) ? "_" : b1.ToString())}, " +
                $"{((b2 == Optimiser.NOT_LOCAL_BYTE) ? "_" : b2.ToString())}, " +
                $"{((b3 == Optimiser.NOT_LOCAL_BYTE) ? "_" : b3.ToString())})");
        }

        private void DoConstant(ByteCodePacket packet)
        {
            var sc = packet.b1;
            stringBuilder.Append($"'{CurrentChunk.Constants[sc]}'");
        }
    }
}
