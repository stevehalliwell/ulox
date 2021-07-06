namespace ULox
{
    public struct CallFrame
    {
        public int InstructionPointer;
        public int StackStart;
        public ClosureInternal Closure;
    }
}
