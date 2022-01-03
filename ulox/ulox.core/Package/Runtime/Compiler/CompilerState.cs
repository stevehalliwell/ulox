using System;
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

        public void AddLocal(string name, int depth = -1)
        {
            if (localCount == byte.MaxValue)
                throw new CompilerException("Too many local variables.");

            locals[localCount++] = new Local(name, depth);
        }

        public int ResolveLocal(string name)
        {
            for (int i = localCount - 1; i >= 0; i--)
            {
                var local = locals[i];
                if (name == local.Name)
                {
                    if (local.Depth == -1)
                        throw new CompilerException($"{name}. Cannot referenece a variable in it's own initialiser.");  //todo all of these throws need to report names and locations
                    return i;
                }
            }

            return -1;
        }

        public int AddUpvalue(byte index, bool isLocal)
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
            {
                throw new CompilerException("Too many closure variables in function.");
            }

            upvalues[upvalueCount] = new Upvalue(index, isLocal);
            return chunk.UpvalueCount++;
        }

        public int ResolveUpvalue(string name)
        {
            if (enclosing == null) return -1;

            int local = enclosing.ResolveLocal(name);
            if (local != -1)
            {
                enclosing.locals[local].IsCaptured = true;
                return AddUpvalue((byte)local, true);
            }

            int upvalue = enclosing.ResolveUpvalue(name);
            if (upvalue != -1)
            {
                return AddUpvalue((byte)upvalue, false);
            }

            return -1;
        }

        public void DeclareVariableByName(string declName)
        {
            if (scopeDepth == 0) return;

            for (int i = localCount - 1; i >= 0; i--)
            {
                var local = locals[i];
                if (local.Depth != -1 && local.Depth < scopeDepth)
                    break;

                if (declName == local.Name)
                    throw new CompilerException($"Already a variable with name '{declName}' in this scope.");
            }

            AddLocal(declName);
        }

        public void MarkInitialised()
        {
            if (scopeDepth == 0) return;
            LastLocal.Depth = scopeDepth;
        }
    }
}
