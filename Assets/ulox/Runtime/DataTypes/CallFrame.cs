namespace ULox
{
    public struct CallFrame
    {
        public int InstructionPointer;
        public byte StackStart;
        public byte ReturnStart;  //Used for return from mark and multi assign, as these do not overlap in the same callframe
        public ClosureInternal Closure;
        public System.Func<VMBase, int, Value> nativeFunc;
    }
}
