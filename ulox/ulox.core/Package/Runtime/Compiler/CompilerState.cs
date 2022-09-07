using System.Collections.Generic;

namespace ULox
{
    public class CompilerState
    {
        internal class Local
        {
            public Local(string name, int depth)
            {
                Name = name;
                Depth = depth;
            }

            public string Name { get; private set; }
            public int Depth { get; set; }
            public bool IsCaptured { get; set; }
        }

        public class LoopState
        {
            public int loopContinuePoint = -1;
            public List<int> loopExitPatchLocations = new List<int>();
        }

        internal class Upvalue
        {
            public byte index;
            public bool isLocal;

            public Upvalue(byte index, bool isLocal)
            {
                this.index = index;
                this.isLocal = isLocal;
            }
        }

        internal Local[] locals = new Local[byte.MaxValue + 1];
        internal Upvalue[] upvalues = new Upvalue[byte.MaxValue + 1];
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

        public Stack<LoopState> LoopStates { get; private set; } = new Stack<LoopState>();
        private Local LastLocal => locals[localCount - 1];


        public void AddLocal(Compiler compiler, string name, int depth = -1)
        {
            if (localCount == byte.MaxValue)
                compiler.ThrowCompilerException("Too many local variables.");

            locals[localCount++] = new Local(name, depth);
        }

        public int ResolveLocal(Compiler compiler, string name)
        {
            for (int i = localCount - 1; i >= 0; i--)
            {
                var local = locals[i];
                if (name == local.Name)
                {
                    if (local.Depth == -1)
                        compiler.ThrowCompilerException($"{name}. Cannot referenece a variable in it's own initialiser.");  //todo all of these throws need to report names and locations
                    return i;
                }
            }

            return -1;
        }

        public int AddUpvalue(Compiler compiler, byte index, bool isLocal)
        {
            int upvalueCount = chunk.UpvalueCount;

            Upvalue upvalue = default;

            for (int i = 0; i < upvalueCount; i++)
            {
                upvalue = upvalues[i];
                if (upvalue.index == index && upvalue.isLocal == isLocal)
                {
                    return i;
                }
            }

            if (upvalueCount == byte.MaxValue)
                compiler.ThrowCompilerException("Too many closure variables in function.");

            upvalues[upvalueCount] = new Upvalue(index, isLocal);
            return chunk.UpvalueCount++;
        }

        public int ResolveUpvalue(Compiler compiler, string name)
        {
            if (enclosing == null) return -1;

            int local = enclosing.ResolveLocal(compiler, name);
            if (local != -1)
            {
                enclosing.locals[local].IsCaptured = true;
                return AddUpvalue(compiler, (byte)local, true);
            }

            int upvalue = enclosing.ResolveUpvalue(compiler, name);
            if (upvalue != -1)
            {
                return AddUpvalue(compiler, (byte)upvalue, false);
            }

            return -1;
        }

        public void DeclareVariableByName(Compiler compiler, string declName)
        {
            if (scopeDepth == 0) return;

            for (int i = localCount - 1; i >= 0; i--)
            {
                var local = locals[i];
                if (local.Depth != -1 && local.Depth < scopeDepth)
                    break;

                if (declName == local.Name)
                    compiler.ThrowCompilerException($"Already a variable with name '{declName}' in this scope.");
            }

            AddLocal(compiler, declName);
        }

        public void MarkInitialised()
        {
            if (scopeDepth == 0) return;
            LastLocal.Depth = scopeDepth;
        }
    }
}
