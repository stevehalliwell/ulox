using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ULox
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ByteCodePacket
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct TestOpDetails
        {
            public TestOpType TestOpType => (TestOpType)_testOp;

            [FieldOffset(0)]
            public readonly byte _testOp;
            [FieldOffset(1)]
            public readonly byte b1;
            [FieldOffset(2)]
            public readonly byte b2;

            public TestOpDetails(TestOpType testOpType, byte b1, byte b2) : this()
            {
                _testOp = (byte)testOpType;
                this.b1 = b1;
                this.b2 = b2;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct ClosureDetails
        {
            public ClosureType ClosureType => (ClosureType)_type;

            [FieldOffset(0)]
            public readonly byte _type;
            [FieldOffset(1)]
            public readonly byte b1;
            [FieldOffset(2)]
            public readonly byte b2;

            public ClosureDetails(ClosureType testOpType, byte b1, byte b2) : this()
            {
                _type = (byte)testOpType;
                this.b1 = b1;
                this.b2 = b2;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct TypeDetails
        {
            public UserType UserType => (UserType)_userType;

            [FieldOffset(0)]
            public byte stringConstantId;
            [FieldOffset(1)]
            public byte _userType;
            [FieldOffset(2)]
            public byte initLabelId;

            public TypeDetails(byte nameConstant, UserType userType, byte initChainLabelId) : this()
            {
                stringConstantId = nameConstant;
                _userType = (byte)userType;
                initLabelId = initChainLabelId;
            }
        }

        public OpCode OpCode => (OpCode)_opCode;
        public ReturnMode ReturnMode => (ReturnMode)b1;
        public NativeType NativeType => (NativeType)b1;
        public ValidateOp ValidateOp => (ValidateOp)b1;
        public bool BoolValue => b1 != 0;

        [FieldOffset(0)]
        public readonly byte _opCode;
        [FieldOffset(1)]
        public readonly byte b1;
        [FieldOffset(2)]
        public readonly byte b2;
        [FieldOffset(3)]
        public readonly byte b3;

        [FieldOffset(1)]
        public readonly ushort u1;

        [FieldOffset(1)]
        public readonly TypeDetails typeDetails;

        [FieldOffset(1)]
        public readonly TestOpDetails testOpDetails;

        [FieldOffset(1)]
        public readonly ClosureDetails closureDetails;

        public ByteCodePacket(OpCode opCode) : this()
        {
            _opCode = (byte)opCode;
        }

        public ByteCodePacket(OpCode opCode, byte b1, byte b2, byte b3) : this(opCode)
        {
            this.b1 = b1;
            this.b2 = b2;
            this.b3 = b3;
        }

        public ByteCodePacket(OpCode opCode, ReturnMode returnMode) : this(opCode)
        {
            b1 = (byte)returnMode;
        }

        public ByteCodePacket(OpCode opCode, NativeType nativeType) : this(opCode)
        {
            b1 = (byte)nativeType;
        }

        public ByteCodePacket(OpCode opCode, ValidateOp validateOp) : this(opCode)
        {
            b1 = (byte)validateOp;
        }

        public ByteCodePacket(OpCode opCode, bool b) : this(opCode)
        {
            b1 = b ? (byte)1 : (byte)0;
        }

        public ByteCodePacket(OpCode opCode, ushort us) : this(opCode)
        {
            u1 = us;
        }

        public ByteCodePacket(OpCode opCode, TypeDetails typeDetails) : this(opCode)
        {
            this.typeDetails = typeDetails;
        }

        public ByteCodePacket(OpCode opCode, TestOpDetails testOpDetails) : this(opCode)
        {
            this.testOpDetails = testOpDetails;
        }

        public ByteCodePacket(OpCode opCode, ClosureDetails closureDetails) : this(opCode)
        {
            this.closureDetails = closureDetails;
        }
    }

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
                case OpCode.NOT:
                case OpCode.NULL:
                case OpCode.POP:
                case OpCode.SWAP:
                case OpCode.FREEZE:
                case OpCode.MIXIN:
                case OpCode.GET_INDEX:
                case OpCode.SET_INDEX:
                case OpCode.EXPAND_COPY_TO_STACK:
                case OpCode.MEETS:
                case OpCode.SIGNS:
                case OpCode.EXPECT:
                case OpCode.COUNT_OF:
                case OpCode.DUPLICATE:
                case OpCode.CLOSE_UPVALUE:
                case OpCode.YIELD:
                case OpCode.TYPEOF:
                case OpCode.THROW:
                case OpCode.ENUM_VALUE:
                case OpCode.READ_ONLY:
                case OpCode.BUILD:
                case OpCode.RETURN:
                case OpCode.PUSH_BOOL:
                case OpCode.PUSH_BYTE:
                case OpCode.CALL:
                case OpCode.NATIVE_TYPE:
                case OpCode.VALIDATE:
                case OpCode.CONSTANT:
                case OpCode.GET_PROPERTY:
                case OpCode.SET_PROPERTY:
                case OpCode.METHOD:
                case OpCode.FIELD:
                case OpCode.REGISTER:
                case OpCode.INJECT:
                case OpCode.GOTO:
                case OpCode.GOTO_IF_FALSE:
                case OpCode.LABEL:
                case OpCode.JUMP_IF_FALSE:
                case OpCode.JUMP:
                case OpCode.LOOP:
                case OpCode.INVOKE:
                case OpCode.TYPE:
                case OpCode.NATIVE_CALL:
                case OpCode.ADD:
                case OpCode.SUBTRACT:
                case OpCode.MULTIPLY:
                case OpCode.DIVIDE:
                case OpCode.LESS:
                case OpCode.GREATER:
                case OpCode.MODULUS:
                case OpCode.EQUAL:
                case OpCode.TEST:
                case OpCode.CLOSURE:
                case OpCode.DEFINE_GLOBAL:
                case OpCode.FETCH_GLOBAL:
                case OpCode.ASSIGN_GLOBAL:
                case OpCode.GET_LOCAL:
                case OpCode.SET_LOCAL:
                case OpCode.GET_UPVALUE:
                case OpCode.SET_UPVALUE:
                {
                    CurrentInstructionIndex++;
                    var b1 = chunk.Instructions[CurrentInstructionIndex];
                    CurrentInstructionIndex++;
                    var b2 = chunk.Instructions[CurrentInstructionIndex];
                    CurrentInstructionIndex++;
                    var b3 = chunk.Instructions[CurrentInstructionIndex];
                    ProcessPacket(new ByteCodePacket(opCode,b1,b2,b3));
                }
                    break;
                
                default:
                    throw new UloxException($"Unhandled OpCode '{opCode}'.");
                }

                DefaultPostOpCode();
            }
        }
        
        protected abstract void PostChunkIterate(CompiledScript compiledScript, Chunk chunk);
        protected abstract void PreChunkInterate(CompiledScript compiledScript, Chunk chunk);
        protected abstract void DefaultOpCode(OpCode opCode);
        protected abstract void DefaultPostOpCode();
        protected abstract void ProcessOpAndByte(OpCode opCode, byte b);
        protected abstract void ProcessOp(OpCode opCode);
        protected abstract void ProcessPacket(ByteCodePacket packet);
    }
}
