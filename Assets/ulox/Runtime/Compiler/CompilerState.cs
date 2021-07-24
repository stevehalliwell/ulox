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
        //TODO move to class compilette
        public Stack<ClassCompilerState> classCompilerStates = new Stack<ClassCompilerState>();
        public FunctionType functionType;
        
        public CompilerState(CompilerState enclosingState, FunctionType funcType)
        {
            enclosing = enclosingState;
            functionType = funcType;
        }

        public class LoopState
        {
            public int loopContinuePoint = -1;
            public List<int> loopExitPatchLocations = new List<int>();

        }

        public Stack<LoopState> loopStates = new Stack<LoopState>();

        public Local LastLocal => locals[localCount - 1];
    }
}
