namespace ULox
{
    public enum NativeCallResult
    {
        SuccessfulExpression,
        Failure
    }
    
    public struct CallFrame
    {
        public delegate NativeCallResult NativeCallDelegate(Vm vm);

        public int InstructionPointer;
        public byte StackStart;
        public byte ReturnCount;
        public byte ArgCount;
        public byte MultiAssignStart;
        public ClosureInternal Closure;
        public NativeCallDelegate nativeFunc;
        public bool YieldOnReturn;
    }
}
