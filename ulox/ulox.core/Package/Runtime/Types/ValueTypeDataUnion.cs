using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ULox
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ValueTypeDataUnion
    {
        [FieldOffset(8 * 0)]
        public double asDouble;

        [FieldOffset(8 * 0)]
        public bool asBool;

        [FieldOffset(8 * 0 + 0)]
        public byte asByte0;
        [FieldOffset(8 * 0 + 1)]
        public byte asByte1;
        [FieldOffset(8 * 0 + 2)]
        public byte asByte2;
        [FieldOffset(8 * 0 + 3)]
        public byte asByte3;

        [FieldOffset(8 * 1)]
        public HashedString asString;

        [FieldOffset(8 * 1)]
        public CallFrame.NativeCallDelegate asNativeFunc;

        [FieldOffset(8 * 1)]
        public object asObject;

        public ClosureInternal asClosure => asObject as ClosureInternal;
        public List<ClosureInternal> asCombined => asObject as List<ClosureInternal>;
        public UpvalueInternal asUpvalue => asObject as UpvalueInternal;
        public UserTypeInternal asClass => asObject as UserTypeInternal;
        public InstanceInternal asInstance => asObject as InstanceInternal;
        public Chunk asChunk => asObject as Chunk;
        public BoundMethod asBoundMethod => asObject as BoundMethod;
    }
}
