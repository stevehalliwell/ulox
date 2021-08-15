using System;
using System.Collections.Generic;
using System.Text;

namespace ULox
{
    public class DisassemblerBase
    {
        private readonly StringBuilder stringBuilder = new StringBuilder();
        protected Func<Chunk, int, int>[] opCodeHandlers;

        public DisassemblerBase()
        {
            opCodeHandlers = new Func<Chunk, int, int>[Enum.GetValues(typeof(OpCode)).Length];

            opCodeHandlers[(int)OpCode.RETURN] = AppendNothing;
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

            opCodeHandlers[(int)OpCode.GET_UPVALUE] = AppendByte;
            opCodeHandlers[(int)OpCode.SET_UPVALUE] = AppendByte;
            opCodeHandlers[(int)OpCode.GET_LOCAL] = AppendByte;
            opCodeHandlers[(int)OpCode.SET_LOCAL] = AppendByte;
            opCodeHandlers[(int)OpCode.CALL] = AppendByte;
            opCodeHandlers[(int)OpCode.PUSH_BOOL] = AppendByte;
            opCodeHandlers[(int)OpCode.PUSH_BYTE] = AppendByte;

            opCodeHandlers[(int)OpCode.CONSTANT] = AppendStringConstant;
            opCodeHandlers[(int)OpCode.DEFINE_GLOBAL] = AppendStringConstant;
            opCodeHandlers[(int)OpCode.FETCH_GLOBAL] = AppendStringConstant;
            opCodeHandlers[(int)OpCode.ASSIGN_GLOBAL] = AppendStringConstant;

            opCodeHandlers[(int)OpCode.CLOSURE] = AppendClosure;
        }

        public string GetString() => stringBuilder.ToString();

        public void DoChunk(Chunk chunk)
        {
            stringBuilder.AppendLine(chunk.Name);
            var instructionCount = 0;
            var prevLine = -1;

            for (int i = 0; i < chunk.Instructions.Count; i++, instructionCount++)
            {
                stringBuilder.Append(i.ToString("0000"));

                var opCode = (OpCode)chunk.Instructions[i];
                prevLine = DoLineNumber(chunk, instructionCount, prevLine);

                i = DoOpCode(chunk, i, opCode);
            }

            var subChunks = new List<Chunk>();

            DoConstants(chunk, subChunks);
            DoSubChunks(subChunks);
        }

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

        private int DoOpCode(Chunk chunk, int i, OpCode opCode)
        {
            stringBuilder.Append(opCode.ToString());

            var opAction = opCodeHandlers[(int)opCode];

            if (opAction != null)
            {
                i = opAction?.Invoke(chunk, i) ?? i;

                stringBuilder.AppendLine();
                return i;
            }

            throw new LoxException($"'{opCode}' is unhandled by the disassembler.");
        }

        protected void AppendSpace()
        {
            stringBuilder.Append(" ");
        }

        protected void Append(string s)
        {
            stringBuilder.Append(s);
        }

        protected int AppendNothing(Chunk chunk, int i)
        {
            return i;
        }

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

        protected int AppendUShort(Chunk chunk, int i)
        {
            AppendSpace();
            i = ReadUShort(chunk, i, out var ushortValue);
            stringBuilder.Append($"({ushortValue})");
            return i;
        }

        protected int AppendByte(Chunk chunk, int i)
        {
            AppendSpace();
            i++;
            var byteValue = chunk.Instructions[i];
            stringBuilder.Append($"({byteValue})");
            return i;
        }

        protected int AppendStringConstant(Chunk chunk, int i)
        {
            AppendSpace();
            i++;
            var ind = chunk.Instructions[i];
            stringBuilder.Append($"({ind})" + chunk.ReadConstant(ind).ToString());
            return i;
        }

        private int ReadUShort(Chunk chunk, int i, out ushort ushortValue)
        {
            i++;
            var bhi = chunk.Instructions[i];
            i++;
            var blo = chunk.Instructions[i];
            ushortValue = (ushort)((bhi << 8) | blo);
            return i;
        }

        private void DoSubChunks(List<Chunk> subChunks)
        {
            stringBuilder.AppendLine("####");
            stringBuilder.AppendLine();
            foreach (var c in subChunks)
            {
                DoChunk(c);
            }
        }

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
    }
}
