using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace ULox
{
    public sealed class Disassembler : IDisassembler
    {
        private readonly StringBuilder stringBuilder = new StringBuilder();
        private Func<Chunk, int, int>[] opCodeHandlers;

        public Disassembler()
        {
            opCodeHandlers = new Func<Chunk, int, int>[Enum.GetValues(typeof(OpCode)).Length];

            opCodeHandlers[(int)OpCode.NEGATE] = AppendNothing;
            opCodeHandlers[(int)OpCode.ADD] = AppendNothing;
            opCodeHandlers[(int)OpCode.SUBTRACT] = AppendNothing;
            opCodeHandlers[(int)OpCode.MULTIPLY] = AppendNothing;
            opCodeHandlers[(int)OpCode.DIVIDE] = AppendNothing;
            opCodeHandlers[(int)OpCode.MODULUS] = AppendNothing;
            opCodeHandlers[(int)OpCode.NONE] = AppendNothing;
            opCodeHandlers[(int)OpCode.NULL] = AppendNothing;
            opCodeHandlers[(int)OpCode.NOT] = AppendNothing;
            opCodeHandlers[(int)OpCode.GREATER] = AppendNothing;
            opCodeHandlers[(int)OpCode.LESS] = AppendNothing;
            opCodeHandlers[(int)OpCode.EQUAL] = AppendNothing;
            opCodeHandlers[(int)OpCode.POP] = AppendNothing;
            opCodeHandlers[(int)OpCode.SWAP] = AppendNothing;
            opCodeHandlers[(int)OpCode.CLOSE_UPVALUE] = AppendNothing;
            opCodeHandlers[(int)OpCode.THROW] = AppendNothing;
            opCodeHandlers[(int)OpCode.YIELD] = AppendNothing;

            opCodeHandlers[(int)OpCode.JUMP_IF_FALSE] = AppendUShort;
            opCodeHandlers[(int)OpCode.JUMP] = AppendUShort;
            opCodeHandlers[(int)OpCode.LOOP] = AppendUShort;

            opCodeHandlers[(int)OpCode.RETURN] = AppendByte;
            opCodeHandlers[(int)OpCode.GET_UPVALUE] = AppendByte;
            opCodeHandlers[(int)OpCode.SET_UPVALUE] = AppendByte;
            opCodeHandlers[(int)OpCode.GET_LOCAL] = AppendByte;
            opCodeHandlers[(int)OpCode.SET_LOCAL] = AppendByte;
            opCodeHandlers[(int)OpCode.CALL] = AppendByte;
            opCodeHandlers[(int)OpCode.PUSH_BOOL] = AppendByte;
            opCodeHandlers[(int)OpCode.PUSH_BYTE] = AppendByte;
            opCodeHandlers[(int)OpCode.VALIDATE] = AppendByte;

            opCodeHandlers[(int)OpCode.CONSTANT] = AppendStringConstant;
            opCodeHandlers[(int)OpCode.DEFINE_GLOBAL] = AppendStringConstant;
            opCodeHandlers[(int)OpCode.FETCH_GLOBAL] = AppendStringConstant;
            opCodeHandlers[(int)OpCode.ASSIGN_GLOBAL] = AppendStringConstant;

            opCodeHandlers[(int)OpCode.CLOSURE] = AppendClosure;

            opCodeHandlers[(int)OpCode.CLASS] = AppendStringConstantThenSpaceThenUshort;
            opCodeHandlers[(int)OpCode.GET_PROPERTY] = AppendStringConstant;
            opCodeHandlers[(int)OpCode.SET_PROPERTY] = AppendStringConstant;
            opCodeHandlers[(int)OpCode.METHOD] = AppendStringConstant;
            opCodeHandlers[(int)OpCode.FIELD] = AppendStringConstant;
            opCodeHandlers[(int)OpCode.MIXIN] = AppendNothing;

            opCodeHandlers[(int)OpCode.FREEZE] = AppendNothing;

            opCodeHandlers[(int)OpCode.INVOKE] = AppendStringConstantThenByte;

            opCodeHandlers[(int)OpCode.TEST] = HandleTestOpCode;

            opCodeHandlers[(int)OpCode.BUILD] = AppendByte;

            opCodeHandlers[(int)OpCode.REGISTER] = AppendStringConstant;
            opCodeHandlers[(int)OpCode.INJECT] = AppendStringConstant;

            opCodeHandlers[(int)OpCode.NATIVE_TYPE] = AppendByte;
            opCodeHandlers[(int)OpCode.GET_INDEX] = AppendNothing;
            opCodeHandlers[(int)OpCode.SET_INDEX] = AppendNothing;
            opCodeHandlers[(int)OpCode.EXPAND_COPY_TO_STACK] = AppendNothing;

            opCodeHandlers[(int)OpCode.TYPEOF] = AppendNothing;

            opCodeHandlers[(int)OpCode.MEETS] = AppendNothing;
            opCodeHandlers[(int)OpCode.SIGNS] = AppendNothing;

            opCodeHandlers[(int)OpCode.COUNT_OF] = AppendNothing;
        }

        public string GetString() => stringBuilder.ToString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoChunk(Chunk chunk)
        {
            stringBuilder.AppendLine(chunk.Name);
            var instructionCount = 0;
            var prevLine = -1;

            DoArgs(chunk);

            for (int i = 0; i < chunk.Instructions.Count; i++, instructionCount++)
            {
                stringBuilder.Append(i.ToString("00000"));

                var opCode = (OpCode)chunk.Instructions[i];
                prevLine = DoLineNumber(chunk, instructionCount, prevLine);

                i = DoOpCode(chunk, i, opCode);
            }

            var subChunks = new List<Chunk>();

            DoConstants(chunk, subChunks);
            DoSubChunks(subChunks);
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
        private int DoLineNumber(Chunk chunk, int instructionCount, int prevLine)
        {
            var lineForInst = chunk.GetLineForInstruction(instructionCount);

            if (lineForInst != prevLine)
            {
                stringBuilder.Append($" {lineForInst} ");
                prevLine = lineForInst;
            }
            else
            {
                stringBuilder.Append($" | ");
            }

            return prevLine;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int DoOpCode(Chunk chunk, int i, OpCode opCode)
        {
            stringBuilder.Append(opCode.ToString());

            var opAction = opCodeHandlers[(int)opCode];

            if (opAction == null)
                throw new UloxException($"'{opCode}' is unhandled by the disassembler.");
            
            i = opAction?.Invoke(chunk, i) ?? i;

            stringBuilder.AppendLine();
            return i;
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
        private int AppendNothing(Chunk chunk, int i)
        {
            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AppendClosure(Chunk chunk, int i)
        {
            AppendSpace();
            i++;
            var ind = chunk.Instructions[i];
            var func = chunk.ReadConstant(ind);
            stringBuilder.Append($"({ind})" + func.ToString());

            if (func.val.asChunk.UpvalueCount > 0)
                stringBuilder.AppendLine();

            var count = func.val.asChunk.UpvalueCount;
            for (int upVal = 0; upVal < count; upVal++)
            {
                i++;
                var isLocal = chunk.Instructions[i];
                i++;
                var upvalIndex = chunk.Instructions[i];

                stringBuilder.Append($"     {(isLocal == 1 ? "local" : "upvalue")} {upvalIndex}");
                if (upVal < count - 1)
                    stringBuilder.AppendLine();
            }

            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AppendUShort(Chunk chunk, int i)
        {
            AppendSpace();
            i = ReadUShort(chunk, i, out var ushortValue);
            stringBuilder.Append($"({ushortValue})");
            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AppendByte(Chunk chunk, int i)
        {
            AppendSpace();
            i++;
            var byteValue = chunk.Instructions[i];
            stringBuilder.Append($"({byteValue})");
            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AppendStringConstant(Chunk chunk, int i)
        {
            AppendSpace();
            i++;
            var ind = chunk.Instructions[i];
            stringBuilder.Append($"({ind})" + chunk.ReadConstant(ind).ToString());
            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ReadUShort(Chunk chunk, int i, out ushort ushortValue)
        {
            i++;
            var bhi = chunk.Instructions[i];
            i++;
            var blo = chunk.Instructions[i];
            ushortValue = (ushort)((bhi << 8) | blo);
            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoSubChunks(List<Chunk> subChunks)
        {
            stringBuilder.AppendLine("####");
            stringBuilder.AppendLine();
            foreach (var c in subChunks)
            {
                DoChunk(c);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoConstants(Chunk chunk, List<Chunk> subChunks)
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

                if (v.type == ValueType.Chunk)
                {
                    subChunks.Add(v.val.asChunk);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int HandleTestOpCode(Chunk chunk, int i)
        {
            AppendSpace();
            i++;
            var testOpType = (TestOpType)chunk.Instructions[i];
            Append(testOpType.ToString());
            AppendSpace();
            switch (testOpType)
            {
            case TestOpType.CaseStart:
            case TestOpType.CaseEnd:
                i = AppendStringConstant(chunk, i);
                i = AppendByte(chunk, i);
                break;

            case TestOpType.TestSetStart:
                i = AppendStringConstant(chunk, i);
                Append(" ");
                i++;
                var testCount = chunk.Instructions[i];
                Append(" [");
                for (int it = 0; it < testCount; it++)
                {
                    i = AppendUShort(chunk, i);
                    if (it < testCount - 1)
                        Append(", ");
                }
                Append("] ");
                break;

            case TestOpType.TestSetEnd:
                i = AppendByte(chunk, i);
                i = AppendByte(chunk, i);
                break;
            case TestOpType.TestFixtureBodyInstruction:
                i = AppendUShort(chunk, i);
                break;
            }

            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AppendStringConstantThenSpaceThenUshort(Chunk chunk, int i)
        {
            i = AppendStringConstant(chunk, i);
            AppendSpace();
            return AppendUShort(chunk, i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AppendStringConstantThenByte(Chunk chunk, int i)
        {
            i = AppendStringConstant(chunk, i);
            return AppendByte(chunk, i);
        }
    }
}
