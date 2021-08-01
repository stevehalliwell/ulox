using System.Collections.Generic;
using System.Text;

namespace ULox
{
    //TODO create standard parsers for number of bytes and their types, map op code to disassemblette
    public class Disassembler
    {
        private readonly StringBuilder stringBuilder = new StringBuilder();

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

            switch (opCode)
            {
            case OpCode.JUMP_IF_FALSE:
            case OpCode.JUMP:
            case OpCode.LOOP:
                stringBuilder.Append(" ");
                i = ReadUShort(chunk, i);
                break;

            case OpCode.CONSTANT:
                case OpCode.DEFINE_GLOBAL:
                case OpCode.FETCH_GLOBAL:
                case OpCode.ASSIGN_GLOBAL:
                case OpCode.GET_PROPERTY:
                case OpCode.SET_PROPERTY:
                case OpCode.GET_SUPER:
                case OpCode.METHOD:
                    i = ReadStringConstant(chunk, i);
                break;

                case OpCode.SUPER_INVOKE:
                case OpCode.INVOKE:
                    {
                        stringBuilder.Append(" ");
                        i++;
                        var constant = chunk.Instructions[i];
                        stringBuilder.Append($"({constant})" + chunk.ReadConstant(constant).ToString());

                        stringBuilder.Append(" ");
                        i++;
                        var argCount = chunk.Instructions[i];
                        stringBuilder.Append(argCount);
                    }
                    break;

                case OpCode.GET_UPVALUE:
                case OpCode.SET_UPVALUE:
                case OpCode.GET_LOCAL:
                case OpCode.SET_LOCAL:
                case OpCode.CALL:
                case OpCode.PUSH_BOOL:
                case OpCode.PUSH_BYTE:
                    i = ReadByte(chunk, i);
                break;

                case OpCode.CLOSURE:
                    {
                        stringBuilder.Append(" ");
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
                    }
                    break;

            case OpCode.CLASS:
                {
                    stringBuilder.Append(" ");
                    i++;
                    var ind = chunk.Instructions[i];
                    stringBuilder.Append($"({ind})" + chunk.ReadConstant(ind).ToString());
                    stringBuilder.Append(" ");
                    i = ReadUShort(chunk, i);
                }
                break;
            case OpCode.TEST:
                {
                    stringBuilder.Append(" ");
                    i++;
                    var testOpType = (TestOpType)chunk.Instructions[i];
                    stringBuilder.Append(testOpType.ToString());
                    stringBuilder.Append(" ");
                    switch (testOpType)
                    {
                    case TestOpType.CaseStart:
                    case TestOpType.CaseEnd:
                        i = ReadStringConstant(chunk, i);
                        i = ReadByte(chunk, i);
                        break;
                    case TestOpType.TestSetStart:
                        i = ReadStringConstant(chunk, i);
                        stringBuilder.Append(" ");
                        i++;
                        var testCount = chunk.Instructions[i];
                        stringBuilder.Append(" [");
                        for (int it = 0; it < testCount; it++)
                        {
                            i = ReadUShort(chunk, i);
                            if(it < testCount-1)
                                stringBuilder.Append(", ");
                        }
                        stringBuilder.Append("] ");
                        break;
                    case TestOpType.TestSetEnd:
                        i = ReadByte(chunk, i);
                        i = ReadByte(chunk, i);
                        break;
                    default:
                        break;
                    }
                    break;
                }
                break;

            case OpCode.RETURN:
            case OpCode.NEGATE:
            case OpCode.ADD:
            case OpCode.SUBTRACT:
            case OpCode.MULTIPLY:
            case OpCode.DIVIDE:
            case OpCode.MODULUS:
            case OpCode.NONE:
            case OpCode.NULL:
            case OpCode.NOT:
            case OpCode.GREATER:
            case OpCode.LESS:
            case OpCode.EQUAL:
            case OpCode.POP:
            case OpCode.SWAP:
            case OpCode.CLOSE_UPVALUE:
            case OpCode.INHERIT:
            case OpCode.THROW:
            case OpCode.YIELD:
            default:
                break;
            }
            stringBuilder.AppendLine();
            return i;
        }

        private int ReadByte(Chunk chunk, int i)
        {
            stringBuilder.Append(" ");
            i++;
            var ind = chunk.Instructions[i];
            stringBuilder.Append($"({ind})");
            return i;
        }

        private int ReadStringConstant(Chunk chunk, int i)
        {
            stringBuilder.Append(" ");
            i++;
            var ind = chunk.Instructions[i];
            stringBuilder.Append($"({ind})" + chunk.ReadConstant(ind).ToString());
            return i;
        }

        private int ReadUShort(Chunk chunk, int i)
        {
            i++;
            var bhi = chunk.Instructions[i];
            i++;
            var blo = chunk.Instructions[i];
            stringBuilder.Append((ushort)((bhi << 8) | blo));
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
