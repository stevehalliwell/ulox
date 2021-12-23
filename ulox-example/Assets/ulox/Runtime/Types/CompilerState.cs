using System.Collections.Generic;

namespace ULox
{
    public class CompilerState
    {
        public Local[] locals = new Local[byte.MaxValue + 1];
        public Upvalue[] upvalues = new Upvalue[byte.MaxValue + 1];
        public int localCount;
        public int scopeDepth;
        public Chunk chunk;
        public CompilerState enclosing;
        public FunctionType functionType;

        public CompilerState(CompilerState enclosingState, FunctionType funcType)
        {
            enclosing = enclosingState;
            functionType = funcType;
        }

        public Stack<LoopState> loopStates = new Stack<LoopState>();

        public Local LastLocal => locals[localCount - 1];
    }
}
