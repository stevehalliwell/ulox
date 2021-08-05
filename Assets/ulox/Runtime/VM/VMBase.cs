using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    //todo external code call lox function with params from external code
    //todo introduce labels so that instructions can be modified freely
    //  and afterwards labels can be removed with offsets by an optimizer step.
    //todo add support for long constant, when we overflow 255 constants in 1 chunk
    //todo delay calls to end of scope
    //todo caching of instance access to local vars, use delay call to write back to instance at close of scope
    //todo introduce optimiser, pass after compile, have compile build slim ast as it goes
    //  convert labels to offsets
    //  identify and remove unused constants
    //  identify push local 0 and paired get or set member, replace with specific this accessing ops
    //todo better string parsing token support
    //todo add conditional
    //todo add pods
    //todo add classof
    //todo multiple returns?
    //todo emit functions when no upvals are required https://github.com/munificent/craftinginterpreters/blob/master/note/answers/chapter25_closures/1.md
    //todo better, standardisead errors, including from native
    //todo track and output class information from compile
    //todo self asign needs safety to prevent their use in declarations.
    //tood add cname, fname, tname
    public abstract class VMBase
    {
        protected readonly FastStack<Value> _valueStack = new FastStack<Value>();
        private readonly FastStack<CallFrame> _callFrames = new FastStack<CallFrame>();
        protected CallFrame currentCallFrame;
        private readonly LinkedList<Value> openUpvalues = new LinkedList<Value>();
        private readonly Table _globals = Table.Empty();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void Push(Value val) => _valueStack.Push(val);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Value Pop() => _valueStack.Pop();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DiscardPop(int amt = 1) => _valueStack.DiscardPop(amt);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Value Peek(int ind = 0) => _valueStack.Peek(ind);
        public string GenerateStackDump() => new DumpStack().Generate(_valueStack);
        public string GenerateGlobalsDump() => new DumpGlobals().Generate(_globals);
        public void SetGlobal(string name, Value val) => _globals[name] = val;
        public Value GetArg(int index) => _valueStack[currentCallFrame.StackStart + index];
        public Value StackTop => _valueStack.Peek();

        public Value GetGlobal(string name) => _globals[name];

        public InterpreterResult CallFunction(Value func, int args)
        {
            CallValue(func, args);
            return Run();
        }

        protected abstract bool ExtendedOp(OpCode opCode, Chunk chunk);

        protected abstract bool ExtendedCall(Value callee, int argCount);

        public virtual void CopyFrom(VMBase otherVM)
        {
            foreach (var val in otherVM._globals)
            {
                SetGlobal(val.Key, val.Value);
            }
        }

        public Value FindFunctionWithArity(string name, int arity)
        {
            var globalVal = GetGlobal(name);

            if (globalVal.type == ValueType.Closure &&
                    globalVal.val.asClosure.chunk.Arity == arity)
            {
                return globalVal;
            }

            return Value.Null();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected byte ReadByte(Chunk chunk) => chunk.Instructions[currentCallFrame.InstructionPointer++];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ushort ReadUShort(Chunk chunk)
        {
            var bhi = chunk.Instructions[currentCallFrame.InstructionPointer++];
            var blo = chunk.Instructions[currentCallFrame.InstructionPointer++];
            return (ushort)((bhi << 8) | blo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void PushNewCallframe(CallFrame callFrame)
        {
            if (_callFrames.Count > 0)
            {
                //save current state
                _callFrames.SetAt(_callFrames.Count - 1, currentCallFrame);
            }

            currentCallFrame = callFrame;
            _callFrames.Push(callFrame);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PopCallFrame()
        {
            //remove top
            _callFrames.Pop();

            //update cache
            if (_callFrames.Count > 0)
                currentCallFrame = _callFrames.Peek();
            else
                currentCallFrame = default;
        }

        public InterpreterResult Interpret(Chunk chunk)
        {
            return Interpret(chunk, 0);
        }

        public InterpreterResult Interpret(Chunk chunk, int ip)
        {
            Push(Value.New(""));
            Push(Value.New(new ClosureInternal() { chunk = chunk }));
            CallValue(Peek(), 0);
            currentCallFrame.InstructionPointer = ip;

            return Run();
        }

        public InterpreterResult Run(IProgram program)
        {
            foreach (var compiled in program.CompiledScripts)
            {
                var res = Interpret(compiled.TopLevelChunk);

                if (res != InterpreterResult.OK)
                    return res;
            }

            return InterpreterResult.OK;
        }

        public InterpreterResult Run()
        {
            while (true)
            {
                var chunk = currentCallFrame.Closure.chunk;

                OpCode opCode = (OpCode)ReadByte(chunk);

                switch (opCode)
                {
                case OpCode.CONSTANT:
                    {
                        var constantIndex = ReadByte(chunk);
                        Push(chunk.ReadConstant(constantIndex));
                    }
                    break;
                case OpCode.RETURN:
                    {
                        DoReturnOp();

                        if (_callFrames.Count == 0)
                        {
                            return InterpreterResult.OK;
                        }
                    }
                    break;
                case OpCode.YIELD:
                    return InterpreterResult.YIELD;
                case OpCode.NEGATE:
                    Push(Value.New(-Pop().val.asDouble));
                    break;
                case OpCode.ADD:
                case OpCode.SUBTRACT:
                case OpCode.MULTIPLY:
                case OpCode.DIVIDE:
                case OpCode.MODULUS:
                    DoMathOp(opCode);
                    break;
                case OpCode.EQUAL:
                case OpCode.LESS:
                case OpCode.GREATER:
                    DoComparisonOp(opCode);
                    break;
                case OpCode.NOT:
                    Push(Value.New(Pop().IsFalsey));
                    break;
                case OpCode.PUSH_BOOL:
                    {
                        var b = ReadByte(chunk);
                        Push(Value.New(b == 1));
                    }
                    break;
                case OpCode.NULL:
                    Push(Value.Null());
                    break;
                case OpCode.PUSH_BYTE:
                    {
                        var b = ReadByte(chunk);
                        Push(Value.New(b));
                    }
                    break;
                case OpCode.POP:
                    DiscardPop();
                    break;
                case OpCode.SWAP:
                    DoSwapOp();
                    break;
                case OpCode.JUMP_IF_FALSE:
                    {
                        ushort jump = ReadUShort(chunk);
                        if (Peek().IsFalsey)
                            currentCallFrame.InstructionPointer += jump;
                    }
                    break;
                case OpCode.JUMP:
                    {
                        ushort jump = ReadUShort(chunk);
                        currentCallFrame.InstructionPointer += jump;
                    }
                    break;
                case OpCode.LOOP:
                    {
                        ushort jump = ReadUShort(chunk);
                        currentCallFrame.InstructionPointer -= jump;
                    }
                    break;
                case OpCode.GET_LOCAL:
                    {
                        var slot = ReadByte(chunk);
                        Push(_valueStack[currentCallFrame.StackStart + slot]);
                    }
                    break;
                case OpCode.SET_LOCAL:
                    {
                        var slot = ReadByte(chunk);
                        _valueStack[currentCallFrame.StackStart + slot] = Peek();
                    }
                    break;
                case OpCode.GET_UPVALUE:
                    {
                        var slot = ReadByte(chunk);
                        var upval = currentCallFrame.Closure.upvalues[slot].val.asUpvalue;
                        if (!upval.isClosed)
                            Push(_valueStack[upval.index]);
                        else
                            Push(upval.value);
                    }
                    break;
                case OpCode.SET_UPVALUE:
                    {
                        var slot = ReadByte(chunk);
                        var upval = currentCallFrame.Closure.upvalues[slot].val.asUpvalue;
                        if (!upval.isClosed)
                            _valueStack[upval.index] = Peek();
                        else
                            upval.value = Peek();
                    }
                    break;
                case OpCode.DEFINE_GLOBAL:
                        DoDefineGlobalOp(chunk);
                    break;
                case OpCode.FETCH_GLOBAL:
                        DoFetchGlobalOp(chunk);
                    break;
                case OpCode.ASSIGN_GLOBAL:
                        DoAssignGlobalOp(chunk);
                    break;
                case OpCode.CALL:
                    {
                        int argCount = ReadByte(chunk);
                        CallValue(Peek(argCount), argCount);
                    }
                    break;
                case OpCode.CLOSURE:
                        DoClosureOp(chunk);
                    break;
                case OpCode.CLOSE_UPVALUE:
                    CloseUpvalues(_valueStack.Count - 1);
                    DiscardPop();
                    break;
                case OpCode.THROW:
                        throw new PanicException(Pop().ToString());
                case OpCode.NONE:
                    break;


                default:
                    if(!ExtendedOp(opCode, chunk))
                        throw new VMException($"Unhandled OpCode '{opCode}'.");
                    break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoSwapOp()
        {
            var n0 = Pop();
            var n1 = Pop();

            Push(n0);
            Push(n1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoAssignGlobalOp(Chunk chunk)
        {
            var global = ReadByte(chunk);
            var globalName = chunk.ReadConstant(global);
            var actualName = globalName.val.asString;
            if (!_globals.ContainsKey(actualName))
            {
                throw new VMException($"Global var of name '{actualName}' was not found.");
            }
            _globals[actualName] = Peek();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoFetchGlobalOp(Chunk chunk)
        {
            var global = ReadByte(chunk);
            var globalName = chunk.ReadConstant(global);
            var actualName = globalName.val.asString;

            if(_globals.TryGetValue(actualName, out var found))
                Push(found);
            else
                throw new VMException($"No global of name {actualName} could be found.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoDefineGlobalOp(Chunk chunk)
        {
            var global = ReadByte(chunk);
            var globalName = chunk.ReadConstant(global);
            _globals[globalName.val.asString] = Pop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoClosureOp(Chunk chunk)
        {
            var constantIndex = ReadByte(chunk);
            var func = chunk.ReadConstant(constantIndex);
            var closureVal = Value.New(new ClosureInternal() { chunk = func.val.asChunk });
            Push(closureVal);

            var closure = closureVal.val.asClosure;

            for (int i = 0; i < closure.upvalues.Length; i++)
            {
                var isLocal = ReadByte(chunk);
                var index = ReadByte(chunk);
                if (isLocal == 1)
                {
                    var local = currentCallFrame.StackStart + index;
                    closure.upvalues[i] = CaptureUpvalue(local);
                }
                else
                {
                    closure.upvalues[i] = currentCallFrame.Closure.upvalues[index];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoReturnOp()
        {
            Value result = Pop();

            CloseUpvalues(currentCallFrame.StackStart);

            var prevStackStart = currentCallFrame.StackStart;

            PopCallFrame();

            var toRemove = System.Math.Max(0, _valueStack.Count - prevStackStart);

            DiscardPop(toRemove);

            Push(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CloseUpvalues(int last)
        {
            while (openUpvalues.Count > 0 &&
                openUpvalues.First.Value.val.asUpvalue.index >= last)
            {
                var upvalue = openUpvalues.First.Value.val.asUpvalue;
                upvalue.value = _valueStack[upvalue.index];
                upvalue.index = -1;
                upvalue.isClosed = true;
                openUpvalues.RemoveFirst();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Value CaptureUpvalue(int index)
        {
            var node = openUpvalues.First;

            while (node != null && node.Value.val.asUpvalue.index > index)
            {
                node = node.Next;
            }

            if (node != null && node.Value.val.asUpvalue.index == index)
            {
                return node.Value;
            }

            var upvalIn = new UpvalueInternal() { index = index };
            var upval = Value.New(upvalIn);

            if (node != null)
                openUpvalues.AddBefore(node, upval);
            else
                openUpvalues.AddLast(upval);

            return upval;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void CallValue(Value callee, int argCount)
        {
            switch (callee.type)
            {
            case ValueType.NativeFunction:
                CallNative(callee.val.asNativeFunc, argCount);
                break;
            case ValueType.Closure:
                Call(callee.val.asClosure, argCount);
                break;
            default:
                if(!ExtendedCall(callee, argCount))
                    throw new VMException($"Invalid Call, value type {callee.type} is not handled.");
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void Call(ClosureInternal closureInternal, int argCount)
        {
            if (argCount != closureInternal.chunk.Arity)
                throw new VMException($"Wrong number of params given to '{closureInternal.chunk.Name}'" +
                    $", got '{argCount}' but expected '{closureInternal.chunk.Arity}'");

            PushNewCallframe(new CallFrame()
            {
                StackStart = _valueStack.Count - argCount - 1,
                Closure = closureInternal
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallNative(System.Func<VMBase, int, Value> asNativeFunc, int argCount)
        {
            PushNewCallframe(new CallFrame()
            {
                StackStart = _valueStack.Count - argCount - 1,
                Closure = null
            });

            var stackPos = _valueStack.Count - argCount;
            var res = asNativeFunc.Invoke(this, argCount);

            DiscardPop(_valueStack.Count - (stackPos - 1));

            PopCallFrame();

            Push(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoMathOp(OpCode opCode)
        {
            var rhs = Pop();
            var lhs = Pop();

            if (lhs.type != rhs.type)
                throw new VMException($"Cannot perform math op across types '{lhs.type}' and '{rhs.type}'.");

            if (opCode == OpCode.ADD && lhs.type == ValueType.String && rhs.type == lhs.type)
            {
                Push(Value.New(lhs.val.asString + rhs.val.asString));
                return;
            }

            if (lhs.type == ValueType.Instance)
            {
                //identify if lhs has a matching method or field

                //push this, push lhs, push rhs

                //call the method
                //  method must take lhs, rhs and return a new result
            }

            if (lhs.type != ValueType.Double)
            {
                throw new VMException($"Cannot perform math op on non math types '{lhs.type}' and '{rhs.type}'.");
            }

            var res = Value.New(0);
            switch (opCode)
            {
            case OpCode.ADD:
                res.val.asDouble = lhs.val.asDouble + rhs.val.asDouble;
                break;
            case OpCode.SUBTRACT:
                res.val.asDouble = lhs.val.asDouble - rhs.val.asDouble;
                break;
            case OpCode.MULTIPLY:
                res.val.asDouble = lhs.val.asDouble * rhs.val.asDouble;
                break;
            case OpCode.DIVIDE:
                res.val.asDouble = lhs.val.asDouble / rhs.val.asDouble;
                break;
            case OpCode.MODULUS:
                res.val.asDouble = lhs.val.asDouble % rhs.val.asDouble;
                break;
            }
            Push(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoComparisonOp(OpCode opCode)
        {
            var rhs = Pop();
            var lhs = Pop();
            //todo fix handling of NaNs on either side
            switch (opCode)
            {
            case OpCode.EQUAL:
                Push(Value.New(lhs.Compare(ref lhs, ref rhs)));
                break;
            case OpCode.LESS:
                if (lhs.type != ValueType.Double || rhs.type != ValueType.Double)
                    throw new VMException($"Cannot less compare on different types '{lhs.type}' and '{rhs.type}'.");
                Push(Value.New(lhs.val.asDouble < rhs.val.asDouble));
                break;
            case OpCode.GREATER:
                if (lhs.type != ValueType.Double || rhs.type != ValueType.Double)
                    throw new VMException($"Cannot greater across on different types '{lhs.type}' and '{rhs.type}'.");
                Push(Value.New(lhs.val.asDouble > rhs.val.asDouble));
                break;
            }
        }
    }
}
