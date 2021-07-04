using System.Collections.Generic;
using System.Text;

namespace ULox
{
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
                case OpCode.INIT_CHAIN_START:
                case OpCode.TEST_CHAIN_START:
                    stringBuilder.Append(" ");
                    i++;
                    var bhi = chunk.Instructions[i];
                    i++;
                    var blo = chunk.Instructions[i];
                    stringBuilder.Append((ushort)((bhi << 8) | blo));
                    break;

                case OpCode.CONSTANT:
                case OpCode.DEFINE_GLOBAL:
                case OpCode.FETCH_GLOBAL_UNCACHED:
                case OpCode.ASSIGN_GLOBAL_UNCACHED:
                case OpCode.GET_PROPERTY_UNCACHED:
                case OpCode.SET_PROPERTY_UNCACHED:
                case OpCode.GET_SUPER:
                case OpCode.CLASS:
                case OpCode.METHOD:
                case OpCode.TEST_START:
                case OpCode.TEST_END:
                    {
                        stringBuilder.Append(" ");
                        i++;
                        var ind = chunk.Instructions[i];
                        stringBuilder.Append($"({ind})" + chunk.ReadConstant(ind).ToString());
                    }
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
                    //case OpCode.GET_PROPERTY_CACHED:
                    //case OpCode.SET_PROPERTY_CACHED:
                    {
                        stringBuilder.Append(" ");
                        i++;
                        var ind = chunk.Instructions[i];
                        stringBuilder.Append($"({ind})");
                    }
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

                case OpCode.RETURN:
            case OpCode.NEGATE:
            case OpCode.ADD:
            case OpCode.SUBTRACT:
            case OpCode.MULTIPLY:
            case OpCode.DIVIDE:
            case OpCode.NONE:
            case OpCode.NULL:
            case OpCode.NOT:
            case OpCode.GREATER:
            case OpCode.LESS:
            case OpCode.EQUAL:
            case OpCode.POP:
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

                if (v.type == Value.Type.Chunk)
                {
                    subChunks.Add(v.val.asChunk);
                }
            }
        }
    }
}
