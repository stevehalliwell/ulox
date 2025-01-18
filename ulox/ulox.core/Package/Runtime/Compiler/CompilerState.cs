using System.Collections.Generic;

namespace ULox
{
    public enum FunctionType
    {
        Script,
        Function,
        Method,
        Init,
        TypeDeclare,
    }
    
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
            public bool IsAccessed { get; set; }
        }

        public sealed class LoopState
        {
            public Label ExitLabelID;
            public Label ContinueLabelID;
            public Label StartLabelID;
            public bool HasExit = false;
            public int ScopeDepth;

            public LoopState(Label exitLabelID)
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
                        compiler.ThrowCompilerException($"Cannot referenece variable '{name}' in it's own initialiser");

                    local.IsAccessed = true;
                    return i;
                }
            }

            return -1;
        }

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

                if (declName == local.Name)
                    compiler.ThrowCompilerException($"Cannot declare var with name '{declName}' at scope depth '{scopeDepth}' a var of the same name already exists at depth '{local.Depth}'");
            }

            AddLocal(compiler, declName);
        }

        public void MarkInitialised()
        {
            if (scopeDepth == 0) return;
            locals[localCount - 1].Depth = scopeDepth;
        }
    }
}
