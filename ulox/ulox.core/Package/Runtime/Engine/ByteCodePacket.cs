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
}
