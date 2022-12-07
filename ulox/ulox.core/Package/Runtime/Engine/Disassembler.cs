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

        protected override void ProcessTestOpAndStringConstantAndByte(OpCode opCode, TestOpType testOpType, byte stringConstant, Value value, byte b)
        {
            stringBuilder.Append($"({stringConstant}){value}");
            AppendSpace();
            stringBuilder.Append($"({b})");
            AppendSpace();
        }

        protected override void ProcessTestOpAndStringConstantAndTestCountAndTestIndexAndTestLocation(OpCode opCode, byte sc, Value value, byte testCount, int it, ushort ushortValue)
        {
            stringBuilder.Append($"({ushortValue})");
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

        protected override void ProcessTestOpAndStringConstantAndTestCount(OpCode opCode, byte stringConstantID, Value value, byte testCount)
        {
            stringBuilder.Append($"({stringConstantID}){value}");
            Append("  [");
            AppendSpace();
        }

        protected override void ProcessTestOpAndUShort(OpCode opCode, TestOpType testOpType, ushort ushortValue)
        {
            stringBuilder.Append($"({ushortValue})");
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

        protected override void ProcessOpAndStringConstantAndByteAndUShort(OpCode opCode, byte stringConstant, Value value, byte b, ushort ushortValue)
        {
            stringBuilder.Append($"({stringConstant}){value}");
            AppendSpace();
            stringBuilder.Append($"({b})");
            AppendSpace();
            stringBuilder.Append($"({ushortValue})");
            AppendSpace();
        }

        protected override void ProcessOpAndStringConstantAndByte(OpCode opCode, byte stringConstant, Value value, byte b)
        {
            stringBuilder.Append($"({stringConstant}){value}");
            AppendSpace();
            stringBuilder.Append($"({b})");
            AppendSpace();
        }

        protected override void ProcessOpAndUShort(OpCode opCode, ushort ushortValue)
        {
            stringBuilder.Append($"({ushortValue})");
        }

        protected override void ProcessOpAndStringConstant(OpCode opCode, byte b, Value value)
        {
            stringBuilder.Append($"({b}){value}");
        }

        protected override void ProcessOpAndByte(OpCode opCode, byte b)
        {
            stringBuilder.Append($"({b})");
        }

        protected override void ProcessOp(OpCode opCode)
        {
        }

        protected override void DefaultOpCode(Chunk chunk, int i, OpCode opCode)
        {
            stringBuilder.Append(i.ToString("00000"));
            DoLineNumber(chunk);
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
    }
}
