using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class CompilerState
    {
        internal sealed class Local
        {
            public Local(string name, int depth)
            {
                Name = name;
                Depth = depth;
            }

            public string Name { get; }
            public int Depth { get; set; }
            public bool IsCaptured { get; set; }
        }

        public sealed class LoopState
        {
            public byte ExitLabelID;
            public byte ContinueLabelID;
            public byte StartLabelID;
            public bool HasExit = false;
            public int ScopeDepth;

            public LoopState(byte exitLabelID)
            {
                ExitLabelID = exitLabelID;
            }
        }

        internal sealed class Upvalue
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

        public Stack<LoopState> LoopStates { get; } = new Stack<LoopState>();
        private Local LastLocal => locals[localCount - 1];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddLocal(Compiler compiler, string name, int depth = -1)
        {
            if (localCount == byte.MaxValue)
                compiler.ThrowCompilerException("Too many local variables.");

            locals[localCount++] = new Local(name, depth);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ResolveLocal(Compiler compiler, string name)
        {
            for (int i = localCount - 1; i >= 0; i--)
            {
                var local = locals[i];
                if (name == local.Name)
                {
                    if (local.Depth == -1)
                        compiler.ThrowCompilerException($"Cannot referenece variable '{name}' in it's own initialiser");
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AddUpvalue(Compiler compiler, byte index, bool isLocal)
        {
            int upvalueCount = chunk.UpvalueCount;
            for (int i = 0; i < upvalueCount; i++)
            {
                Upvalue upvalue = upvalues[i];
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DeclareVariableByName(Compiler compiler, string declName)
        {
            if (scopeDepth == 0) return;

            for (int i = localCount - 1; i >= 0; i--)
            {
                var local = locals[i];
                if (local.Depth != -1 && local.Depth < scopeDepth)
                    break;

                if (declName == local.Name)
                    compiler.ThrowCompilerException($"Already a variable with name '{declName}' in this scope");
            }

            AddLocal(compiler, declName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkInitialised()
        {
            if (scopeDepth == 0) return;
            LastLocal.Depth = scopeDepth;
        }
    }
}
