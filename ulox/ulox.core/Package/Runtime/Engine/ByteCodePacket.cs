using System.Runtime.InteropServices;

namespace ULox
{
    public enum OpCode : byte
    {
        NONE,

        PUSH_CONSTANT,
        PUSH_VALUE,

        POP,
        DUPLICATE,

        DEFINE_GLOBAL,
        FETCH_GLOBAL,
        ASSIGN_GLOBAL,
        GET_LOCAL,
        SET_LOCAL,
        GET_UPVALUE,
        SET_UPVALUE,
        CLOSE_UPVALUE,

        NOT,
        EQUAL,
        LESS,
        GREATER,

        NEGATE,
        ADD,
        SUBTRACT,
        MULTIPLY,
        DIVIDE,
        MODULUS,

        CALL,
        CLOSURE,

        NATIVE_CALL,

        RETURN,
        YIELD,

        MULTI_VAR,

        THROW,

        VALIDATE,

        GET_PROPERTY,
        SET_PROPERTY,
        GET_FIELD,
        SET_FIELD,
        INVOKE,

        TEST,

        BUILD,

        NATIVE_TYPE,
        GET_INDEX,
        SET_INDEX,
        EXPAND_COPY_TO_STACK,

        TYPEOF,

        COUNT_OF,

        GOTO,
        GOTO_IF_FALSE,
        LABEL,

        ENUM_VALUE,
        READ_ONLY, //could collapse with FREEZE

        UPDATE,
    }

    public static class OpCodeUtil
    {
        public const int NumberOfOpCodes = (int)OpCode.UPDATE + 1;
    }
    
    public enum ClosureType : byte
    {
        Closure,
        UpValueInfo,
    }

    public enum NativeType : byte
    {
        List,
        Dynamic,
    }

    public enum TestOpType : byte
    {
        TestFixtureBodyInstruction,
        TestCase,
        CaseStart,
        CaseEnd,
        TestSetEnd,
    }

    public enum ValidateOp : byte
    {
        Meets,
        Signs,
    }
    
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct ByteCodePacket
    {
        [StructLayout(LayoutKind.Explicit)]
        public readonly struct TestOpDetails
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
        public readonly struct ClosureDetails
        {
            public ClosureType ClosureType => (ClosureType)_type;

            [FieldOffset(0)]
            public readonly byte _type;
            [FieldOffset(1)]
            public readonly byte b1;    //if we split closure and upvalue info, we can keep a bigger constant index
            [FieldOffset(2)]
            public readonly byte b2;    //used for upvalue not closure

            public ClosureDetails(ClosureType testOpType, byte b1, byte b2) : this()
            {
                _type = (byte)testOpType;
                this.b1 = b1;
                this.b2 = b2;
            }
        }

        public OpCode OpCode => (OpCode)_opCode;
        public NativeType NativeType => (NativeType)b1;
        public ValidateOp ValidateOp => (ValidateOp)b1;
        public bool BoolValue => b1 != 0;

        [FieldOffset(0)]
        public readonly uint _data;
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
        public readonly TestOpDetails testOpDetails;

        [FieldOffset(1)]
        public readonly ClosureDetails closureDetails;
        
        public ByteCodePacket(OpCode opCode)
            : this(
                  opCode,
                  Optimiser.NOT_LOCAL_BYTE,
                  Optimiser.NOT_LOCAL_BYTE,
                  Optimiser.NOT_LOCAL_BYTE)
        {
        }

        public ByteCodePacket(OpCode opCode, byte b1)
            : this(
                  opCode,
                  b1,
                  Optimiser.NOT_LOCAL_BYTE,
                  Optimiser.NOT_LOCAL_BYTE)
        {
        }
        
        public ByteCodePacket(OpCode opCode, byte b1, byte b2)
            : this(
                  opCode,
                  b1,
                  b2,
                  Optimiser.NOT_LOCAL_BYTE)
        {
        }

        public ByteCodePacket(OpCode opCode, byte b1, byte b2, byte b3) : this()
        {
            _opCode = (byte)opCode;
            this.b1 = b1;
            this.b2 = b2;
            this.b3 = b3;
        }

        public ByteCodePacket(OpCode opCode, NativeType nativeType) : this(opCode)
        {
            b1 = (byte)nativeType;
        }

        public ByteCodePacket(OpCode opCode, ValidateOp validateOp) : this(opCode)
        {
            b1 = (byte)validateOp;
        }

        public ByteCodePacket(OpCode opCode, TestOpDetails testOpDetails) : this(opCode)
        {
            this.testOpDetails = testOpDetails;
        }

        public ByteCodePacket(OpCode opCode, ClosureDetails closureDetails) : this(opCode)
        {
            this.closureDetails = closureDetails;
        }

        public override string ToString()
        {
            return $"{OpCode} {b1} {b2} {b3}";
        }

        private ByteCodePacket(uint data) : this()
        {
            _data = data;
        }

        internal static ByteCodePacket FromUint(uint v)
        {
            return new ByteCodePacket(v);
        }
    }
}
