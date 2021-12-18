namespace ULox
{
    public enum NativeCallResult
    {
        Success,
        Failure
    }

    public delegate NativeCallResult NativeCallDelegate(VMBase vm, int argc);

    public struct CallFrame
    {
        public int InstructionPointer;
        public byte StackStart;
        public byte ReturnStart;  //Used for return from mark and multi assign, as these do not overlap in the same callframe
        public ClosureInternal Closure;
        public NativeCallDelegate nativeFunc;
    }
}
