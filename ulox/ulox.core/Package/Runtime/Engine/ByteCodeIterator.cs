using System.Runtime.CompilerServices;

namespace ULox
{
    public abstract class ByteCodeIterator
    {
        public int CurrentInstructionIndex { get; private set; }
        public Chunk CurrentChunk { get; private set; }

        public void Iterate(CompiledScript compiledScript)
        {
            Iterate(compiledScript, compiledScript.TopLevelChunk);

            foreach (var c in compiledScript.AllChunks)
            {
                if (compiledScript.TopLevelChunk == c) continue;
                Iterate(compiledScript, c);
            }
        }

        public void Iterate(CompiledScript compiledScript, Chunk chunk)
        {
            PreChunkInterate(compiledScript, chunk);

            ChunkIterate(chunk);

            PostChunkIterate(compiledScript, chunk);
        }

        private void ChunkIterate(Chunk chunk)
        {
            CurrentChunk = chunk;
            for (CurrentInstructionIndex = 0; CurrentInstructionIndex < chunk.Instructions.Count; CurrentInstructionIndex++)
            {
                var opCode = (OpCode)chunk.Instructions[CurrentInstructionIndex];
                DefaultOpCode(opCode);

                switch (opCode)
                {
                case OpCode.NONE:
                case OpCode.NEGATE:
                case OpCode.ADD:
                case OpCode.SUBTRACT:
                case OpCode.MULTIPLY:
                case OpCode.DIVIDE:
                case OpCode.NOT:
                case OpCode.NULL:
                case OpCode.LESS:
                case OpCode.GREATER:
                case OpCode.MODULUS:
                case OpCode.POP:
                case OpCode.SWAP:
                case OpCode.FREEZE:
                case OpCode.MIXIN:
                case OpCode.GET_INDEX:
                case OpCode.SET_INDEX:
                case OpCode.EXPAND_COPY_TO_STACK:
                case OpCode.MEETS:
                case OpCode.SIGNS:
                case OpCode.COUNT_OF:
                case OpCode.EXPECT:
                case OpCode.DUPLICATE:
                case OpCode.EQUAL:
                case OpCode.CLOSE_UPVALUE:
                case OpCode.YIELD:
                case OpCode.TYPEOF:
                case OpCode.THROW:
                case OpCode.ENUM_VALUE:
                    ProcessOp(opCode);
                    break;

                case OpCode.RETURN:
                case OpCode.PUSH_BOOL:
                case OpCode.PUSH_BYTE:
                case OpCode.GET_LOCAL:
                case OpCode.SET_LOCAL:
                case OpCode.GET_UPVALUE:
                case OpCode.SET_UPVALUE:
                case OpCode.BUILD:
                case OpCode.CALL:
                case OpCode.NATIVE_TYPE:
                case OpCode.VALIDATE:
                {
                    CurrentInstructionIndex++;
                    var b = chunk.Instructions[CurrentInstructionIndex];
                    ProcessOpAndByte(opCode, b);
                }
                break;

                case OpCode.CONSTANT:
                case OpCode.DEFINE_GLOBAL:
                case OpCode.FETCH_GLOBAL:
                case OpCode.ASSIGN_GLOBAL:
                case OpCode.GET_PROPERTY:
                case OpCode.SET_PROPERTY:
                case OpCode.METHOD:
                case OpCode.FIELD:
                case OpCode.REGISTER:
                case OpCode.INJECT:
                {
                    CurrentInstructionIndex++;
                    var sc = chunk.Instructions[CurrentInstructionIndex];
                    ProcessOpAndStringConstant(opCode, sc);
                }
                break;

                case OpCode.GOTO:
                case OpCode.GOTO_IF_FALSE:
                case OpCode.LABEL:
                {
                    CurrentInstructionIndex++;
                    var labelID = chunk.Instructions[CurrentInstructionIndex];
                    ProcessOpAndLabel(opCode, labelID);
                }
                break;

                case OpCode.JUMP_IF_FALSE:
                case OpCode.JUMP:
                case OpCode.LOOP:
                {
                    CurrentInstructionIndex = ReadUShort(chunk, CurrentInstructionIndex, out var ushortValue);
                    ProcessOpAndUShort(opCode, ushortValue);
                }
                break;

                case OpCode.INVOKE:
                {
                    CurrentInstructionIndex++;
                    var sc = chunk.Instructions[CurrentInstructionIndex];
                    CurrentInstructionIndex++;
                    var b = chunk.Instructions[CurrentInstructionIndex];
                    ProcessOpAndStringConstantAndByte(opCode, sc, b);
                }
                break;

                case OpCode.TYPE:
                {
                    CurrentInstructionIndex++;
                    var sc = chunk.Instructions[CurrentInstructionIndex];
                    CurrentInstructionIndex++;
                    var b = chunk.Instructions[CurrentInstructionIndex];
                    CurrentInstructionIndex++;
                    var label = chunk.Instructions[CurrentInstructionIndex];
                    ProcessTypeOp(opCode, sc, b, label);
                }
                break;

                case OpCode.CLOSURE:
                {
                    CurrentInstructionIndex++;
                    var ind = chunk.Instructions[CurrentInstructionIndex];
                    var func = chunk.ReadConstant(ind);

                    var count = func.val.asChunk.UpvalueCount;

                    ProcessOpClosure(opCode, ind, func.val.asChunk, count);

                    for (int upVal = 0; upVal < count; upVal++)
                    {
                        CurrentInstructionIndex++;
                        var isLocal = chunk.Instructions[CurrentInstructionIndex];
                        CurrentInstructionIndex++;
                        var upvalIndex = chunk.Instructions[CurrentInstructionIndex];

                        ProcessOpClosureUpValue(opCode, ind, count, upVal, isLocal, upvalIndex);
                    }
                }
                break;

                case OpCode.TEST:
                {
                    CurrentInstructionIndex++;
                    var testOpType = (TestOpType)chunk.Instructions[CurrentInstructionIndex];

                    ProcessTestOp(opCode, testOpType);

                    switch (testOpType)
                    {
                    case TestOpType.CaseStart:
                    case TestOpType.CaseEnd:
                    {
                        CurrentInstructionIndex++;
                        var sc = chunk.Instructions[CurrentInstructionIndex];
                        CurrentInstructionIndex++;
                        var b = chunk.Instructions[CurrentInstructionIndex];
                        ProcessTestOpAndStringConstantAndByte(opCode, testOpType, sc, b);
                    }
                    break;

                    case TestOpType.TestSetStart:
                    {
                        CurrentInstructionIndex++;
                        var sc = chunk.Instructions[CurrentInstructionIndex];
                        CurrentInstructionIndex++;
                        var testCount = chunk.Instructions[CurrentInstructionIndex];
                        ProcessTestOpAndStringConstantAndTestCount(opCode, sc, testCount);
                        for (int it = 0; it < testCount; it++)
                        {
                            CurrentInstructionIndex++;
                            var label = chunk.Instructions[CurrentInstructionIndex];
                            ProcessTestOpAndStringConstantAndTestCountAndTestIndexAndTestLabel(opCode, sc, testCount, it, label);
                        }
                    }
                    break;

                    case TestOpType.TestSetEnd:
                    {
                        CurrentInstructionIndex++;
                        var b1 = chunk.Instructions[CurrentInstructionIndex];
                        CurrentInstructionIndex++;
                        var b2 = chunk.Instructions[CurrentInstructionIndex];
                        ProcessTestOpAndByteAndByte(opCode, testOpType, b1, b2);
                    }
                    break;

                    case TestOpType.TestFixtureBodyInstruction:
                    {
                        CurrentInstructionIndex++;
                        var label = chunk.Instructions[CurrentInstructionIndex];
                        ProcessTestOpAndLabel(opCode, testOpType, label);
                    }
                    break;
                    }
                }
                break;

                case OpCode.NATIVE_CALL:
                    break;

                default:
                    throw new UloxException($"Unhandled OpCode '{opCode}'.");
                }

                DefaultPostOpCode();
            }
        }

        protected abstract void ProcessOpAndStringConstantAndByte(OpCode opCode, byte sc, byte b);
        protected abstract void ProcessOpAndStringConstant(OpCode opCode, byte sc);
        protected abstract void ProcessOpAndLabel(OpCode opCode, byte labelId);
        protected abstract void PostChunkIterate(CompiledScript compiledScript, Chunk chunk);
        protected abstract void PreChunkInterate(CompiledScript compiledScript, Chunk chunk);
        protected abstract void DefaultOpCode(OpCode opCode);
        protected abstract void DefaultPostOpCode();
        protected abstract void ProcessTestOpAndStringConstantAndTestCountAndTestIndexAndTestLabel(OpCode opCode, byte sc, byte testCount, int it, byte label);
        protected abstract void ProcessTestOpAndStringConstantAndTestCount(OpCode opCode, byte stringConstantID, byte testCount);
        protected abstract void ProcessTestOp(OpCode opCode, TestOpType testOpType);
        protected abstract void ProcessTestOpAndLabel(OpCode opCode, TestOpType testOpType, byte label);
        protected abstract void ProcessTestOpAndByteAndByte(OpCode opCode, TestOpType testOpType, byte b1, byte b2);
        protected abstract void ProcessTestOpAndStringConstantAndByte(OpCode opCode, TestOpType testOpType, byte stringConstant, byte b);
        protected abstract void ProcessOpClosure(OpCode opCode, byte funcID, Chunk asChunk, int upValueCount);
        protected abstract void ProcessOpClosureUpValue(OpCode opCode, byte fundID, int count, int upVal, byte isLocal, byte upvalIndex);
        protected abstract void ProcessTypeOp(OpCode opCode, byte stringConstant, byte b, byte initLabel);
        protected abstract void ProcessOpAndUShort(OpCode opCode, ushort ushortValue);
        protected abstract void ProcessOpAndByte(OpCode opCode, byte b);
        protected abstract void ProcessOp(OpCode opCode);


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
    }
}
