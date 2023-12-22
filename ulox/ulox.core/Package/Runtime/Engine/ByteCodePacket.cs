using System.Runtime.InteropServices;

namespace ULox
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct ByteCodePacket
    {
        [StructLayout(LayoutKind.Explicit)]
        public readonly struct PushValueDetails
        {
            public PushValueOpType ValueType => (PushValueOpType)_valueType;

            [FieldOffset(0)]
            public readonly byte _valueType;
            [FieldOffset(1)]
            public readonly bool _b;
            [FieldOffset(1)]
            public readonly int _i;
            [FieldOffset(1)]
            public readonly float _f;

            public PushValueDetails(PushValueOpType nullType) : this()
            {
                _valueType = (byte)PushValueOpType.Null;
            }

            public PushValueDetails(bool b) : this()
            {
                _valueType = (byte)PushValueOpType.Bool;
                _b = b;
            }

            public PushValueDetails(int i) : this()
            {
                _valueType = (byte)PushValueOpType.Int;
                _i = i;
            }

            public PushValueDetails(float f) : this()
            {
                _valueType = (byte)PushValueOpType.Float;
                _f = f;
            }
        }

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

        public OpCode OpCode => (OpCode)_opCode;
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
        public readonly PushValueDetails pushValueDetails;

        [FieldOffset(1)]
        public readonly TestOpDetails testOpDetails;

        [FieldOffset(1)]
        public readonly ClosureDetails closureDetails;
        
        public ByteCodePacket(OpCode opCode)
            : this(
                  opCode,
                  ByteCodeOptimiser.NOT_LOCAL_BYTE,
                  ByteCodeOptimiser.NOT_LOCAL_BYTE,
                  ByteCodeOptimiser.NOT_LOCAL_BYTE)
        {
        }

        public ByteCodePacket(OpCode opCode, byte b1)
            : this(
                  opCode,
                  b1,
                  ByteCodeOptimiser.NOT_LOCAL_BYTE,
                  ByteCodeOptimiser.NOT_LOCAL_BYTE)
        {
        }
        
        public ByteCodePacket(OpCode opCode, byte b1, byte b2)
            : this(
                  opCode,
                  b1,
                  b2,
                  ByteCodeOptimiser.NOT_LOCAL_BYTE)
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

        public ByteCodePacket(OpCode opCode, bool b) : this(opCode)
        {
            b1 = b ? (byte)1 : (byte)0;
        }

        public ByteCodePacket(PushValueDetails details) : this(OpCode.PUSH_VALUE)
        {
            this.pushValueDetails = details;
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
