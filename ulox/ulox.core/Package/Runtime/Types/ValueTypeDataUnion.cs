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

        [FieldOffset(8 * 1)]
        public HashedString asString;

        [FieldOffset(8 * 1)]
        public Vm.NativeCallDelegate asNativeFunc;

        [FieldOffset(8 * 1)]
        public object asObject;

        public ClosureInternal asClosure => asObject as ClosureInternal;
        public List<ClosureInternal> asCombined => asObject as List<ClosureInternal>;
        public UpvalueInternal asUpvalue => asObject as UpvalueInternal;
        public ClassInternal asClass => asObject as ClassInternal;
        public InstanceInternal asInstance => asObject as InstanceInternal;
        public Chunk asChunk => asObject as Chunk;
        public BoundMethod asBoundMethod => asObject as BoundMethod;
    }
}
