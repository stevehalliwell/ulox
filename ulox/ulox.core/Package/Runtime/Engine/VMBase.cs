using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    //todo introduce labels so that instructions can be modified freely
    //  and afterwards labels can be removed with offsets by an optimizer step.
    //todo delay calls to end of scope
    //todo introduce optimiser, pass after compile, have compile build slim ast as it goes
    //  convert labels to offsets
    //  identify and remove unused constants
    //  identify push local 0 and paired get or set member, replace with specific this accessing ops
    //todo better string parsing token support
    //todo add conditional
    //todo better, standardisead errors, including from native
    //todo track and output class information from compile
    //todo self asign needs safety to prevent their use in declarations.
    public class VMBase : IVm
    {
        private readonly ClosureInternal NativeCallClosure;

        protected readonly FastStack<Value> _valueStack = new FastStack<Value>();
        protected readonly FastStack<Value> _returnStack = new FastStack<Value>();
        private readonly FastStack<CallFrame> _callFrames = new FastStack<CallFrame>();
        protected CallFrame currentCallFrame;
        private IEngine _engine;
        public IEngine Engine => _engine;
        private readonly LinkedList<Value> openUpvalues = new LinkedList<Value>();
        private readonly Table _globals = new Table();

        public VMBase()
        {
            var nativeChunk = new Chunk("NativeCallChunkWrapper");
            nativeChunk.WriteByte((byte)OpCode.NATIVE_CALL, 0);
            NativeCallClosure = new ClosureInternal() { chunk = nativeChunk };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void Push(Value val) => _valueStack.Push(val);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Value Pop() => _valueStack.Pop();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DiscardPop(int amt = 1) => _valueStack.DiscardPop(amt);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Value Peek(int ind = 0) => _valueStack.Peek(ind);

        public string GenerateStackDump() => new DumpStack().Generate(_valueStack);

        public string GenerateReturnDump() => new DumpStack().Generate(_returnStack);

        public string GenerateGlobalsDump() => new DumpGlobals().Generate(_globals);

        public Value GetGlobal(HashedString name) => _globals[name];

        public void SetGlobal(HashedString name, Value val) => _globals[name] = val;

        public Value GetArg(int index) => _valueStack[currentCallFrame.StackStart + index];

        public int CurrentFrameStackValues => _valueStack.Count - currentCallFrame.StackStart;
        public Value StackTop => _valueStack.Peek();
        public int StackCount => _valueStack.Count;

        public void SetEngine(IEngine engine) => _engine = engine;

        public InterpreterResult PushCallFrameAndRun(Value func, int args)
        {
            PushCallFrameFromValue(func, args);
            return Run();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushReturn(Value val)
        {
            if (currentCallFrame.ReturnStart == 0)
                currentCallFrame.ReturnStart = (byte)StackCount;

            _valueStack.Push(val);
        }

        protected virtual bool ExtendedOp(OpCode opCode, Chunk chunk) => false;

        protected virtual bool ExtendedCall(Value callee, int argCount) => false;

        public virtual void CopyFrom(IVm otherVM)
        {
            _engine = otherVM.Engine;

            if (otherVM is VMBase asVmBase)
            {
                foreach (var val in asVmBase._globals)
                {
                    SetGlobal(val.Key, val.Value);
                }
            }
        }

        public Value FindFunctionWithArity(HashedString name, int arity)
        {
            try
            {
                var globalVal = GetGlobal(name);

                if (globalVal.type == ValueType.Closure &&
                    globalVal.val.asClosure.chunk.Arity == arity)
                {
                    return globalVal;
                }
            }
            catch (System.Exception)
            {
            }

            return Value.Null();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte(Chunk chunk) => chunk.Instructions[currentCallFrame.InstructionPointer++];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUShort(Chunk chunk)
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
        private byte PopCallFrame()
        {
            var poppedStackStart = currentCallFrame.StackStart;
            //remove top
            _callFrames.Pop();

            //update cache
            if (_callFrames.Count > 0)
                currentCallFrame = _callFrames.Peek();
            else
                currentCallFrame = default;

            return poppedStackStart;
        }

        public InterpreterResult Interpret(Chunk chunk)
        {
            return Interpret(chunk, 0);
        }

        public InterpreterResult Interpret(Chunk chunk, int ip)
        {
            //push this empty string to match the expectation of the function compiler
            Push(Value.New(""));
            Push(Value.New(new ClosureInternal() { chunk = chunk }));
            PushCallFrameFromValue(Peek(), 0);
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
            if (currentCallFrame.Closure == null && currentCallFrame.nativeFunc == null)
                return InterpreterResult.NOTHING;

            while (true)
            {
                var chunk = currentCallFrame.Closure.chunk;

                OpCode opCode = (OpCode)ReadByte(chunk);

                switch (opCode)
                {
                case OpCode.CONSTANT:
                    DoConstantOp(chunk);

                    break;

                case OpCode.RETURN:
                    if (DoReturnOp(chunk))
                        return InterpreterResult.OK;

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
                    DoNotOp();
                    break;

                case OpCode.PUSH_BOOL:
                    DoPushBoolOp(chunk);
                    break;

                case OpCode.NULL:
                    DoNullOp();
                    break;

                case OpCode.PUSH_BYTE:
                    DoPushByteOp(chunk);
                    break;

                case OpCode.POP:
                    DiscardPop();
                    break;

                case OpCode.SWAP:
                    DoSwapOp();
                    break;

                case OpCode.JUMP_IF_FALSE:
                    DoJumpIfFalseOp(chunk);
                    break;

                case OpCode.JUMP:
                    DoJumpOp(chunk);
                    break;

                case OpCode.LOOP:
                    DoLoopOp(chunk);
                    break;

                case OpCode.GET_LOCAL:
                    DoGetLocalOp(chunk);
                    break;

                case OpCode.SET_LOCAL:
                    DoSetLocalOp(chunk);
                    break;

                case OpCode.GET_UPVALUE:
                    DoGetUpvalueOp(chunk);
                    break;

                case OpCode.SET_UPVALUE:
                    DoSetUpvalueOp(chunk);
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
                    DoCallOp(chunk);
                    break;

                case OpCode.CLOSURE:
                    DoClosureOp(chunk);
                    break;

                case OpCode.CLOSE_UPVALUE:
                    DoCloseUpvalueOp();
                    break;

                case OpCode.THROW:
                    throw new PanicException(Pop().ToString());
                case OpCode.NONE:
                    break;

                case OpCode.BUILD:
                    DoBuildOp(chunk);
                    break;

                case OpCode.NATIVE_CALL:
                    DoNativeCall(opCode);
                    break;

                case OpCode.VALIDATE:
                    DoValidateOp(chunk);
                    break;

                default:
                    if (!ExtendedOp(opCode, chunk))
                        throw new VMException($"Unhandled OpCode '{opCode}'.");
                    break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoCloseUpvalueOp()
        {
            CloseUpvalues(_valueStack.Count - 1);
            DiscardPop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoCallOp(Chunk chunk)
        {
            int argCount = ReadByte(chunk);
            PushCallFrameFromValue(Peek(argCount), argCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoSetUpvalueOp(Chunk chunk)
        {
            var slot = ReadByte(chunk);
            var upval = currentCallFrame.Closure.upvalues[slot].val.asUpvalue;
            if (!upval.isClosed)
                _valueStack[upval.index] = Peek();
            else
                upval.value = Peek();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoGetUpvalueOp(Chunk chunk)
        {
            var slot = ReadByte(chunk);
            var upval = currentCallFrame.Closure.upvalues[slot].val.asUpvalue;
            if (!upval.isClosed)
                Push(_valueStack[upval.index]);
            else
                Push(upval.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoSetLocalOp(Chunk chunk)
        {
            var slot = ReadByte(chunk);
            _valueStack[currentCallFrame.StackStart + slot] = Peek();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoGetLocalOp(Chunk chunk)
        {
            var slot = ReadByte(chunk);
            Push(_valueStack[currentCallFrame.StackStart + slot]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoLoopOp(Chunk chunk)
        {
            ushort jump = ReadUShort(chunk);
            currentCallFrame.InstructionPointer -= jump;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoJumpOp(Chunk chunk)
        {
            ushort jump = ReadUShort(chunk);
            currentCallFrame.InstructionPointer += jump;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoJumpIfFalseOp(Chunk chunk)
        {
            ushort jump = ReadUShort(chunk);
            if (Peek().IsFalsey)
                currentCallFrame.InstructionPointer += jump;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoPushByteOp(Chunk chunk)
        {
            var b = ReadByte(chunk);
            Push(Value.New(b));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoNullOp()
        {
            Push(Value.Null());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoPushBoolOp(Chunk chunk)
        {
            var b = ReadByte(chunk);
            Push(Value.New(b == 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoNotOp()
        {
            Push(Value.New(Pop().IsFalsey));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoConstantOp(Chunk chunk)
        {
            var constantIndex = ReadByte(chunk);
            Push(chunk.ReadConstant(constantIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoValidateOp(Chunk chunk)
        {
            var validateOp = (ValidateOp)ReadByte(chunk);
            switch (validateOp)
            {
            case ValidateOp.MultiReturnMatches:
                var requestedResultsValue = Pop();
                var requestedResults = (int)requestedResultsValue.val.asDouble;
                var availableResults = _returnStack.Count;
                if (requestedResults != availableResults)
                    throw new VMException($"Multi var assign to result mismatch. Taking '{requestedResults}' but results contains '{availableResults}'.");
                break;

            default:
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoBuildOp(Chunk chunk)
        {
            var buildOpType = (BuildOpType)ReadByte(chunk);
            var constantIndex = ReadByte(chunk);
            var str = chunk.ReadConstant(constantIndex).val.asString;
            switch (buildOpType)
            {
            case BuildOpType.Bind:
                Engine.Context.BindLibrary(str.String);
                break;

            case BuildOpType.Queue:
                Engine.LocateAndQueue(str.String);
                break;

            default:
                throw new VMException($"Unhanlded BuildOpType '{buildOpType}'");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoNativeCall(OpCode opCode)
        {
            if (currentCallFrame.nativeFunc == null)
                throw new VMException($"{opCode} without nativeFunc encountered. This is not allowed.");

            var argCount = CurrentFrameStackValues;
            var res = currentCallFrame.nativeFunc.Invoke(this, argCount);

            if (res == NativeCallResult.SuccessfulExpression)
            {
                if (currentCallFrame.ReturnStart != 0)
                    ReturnFromMark();
                else
                    ReturnOneValue(Value.Null());
            }
            else if (res == NativeCallResult.SuccessfulStatement)
            {
                PopFrameAndDiscard();
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

            if (_globals.TryGetValue(actualName, out var found))
            {
                Push(found);
            }
            else
            {
                throw new VMException($"No global of name {actualName} could be found.");
            }
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
        private bool DoReturnOp(Chunk chunk)
        {
            var returnMode = (ReturnMode)ReadByte(chunk);
            switch (returnMode)
            {
            case ReturnMode.One:
                var top = Pop();
                ReturnOneValue(top);
                break;

            case ReturnMode.Begin:
                currentCallFrame.ReturnStart = (byte)StackCount;
                break;

            case ReturnMode.End:
                ReturnFromMark();
                break;

            case ReturnMode.MarkMultiReturnAssignStart:
                currentCallFrame.ReturnStart = (byte)StackCount;
                break;

            case ReturnMode.MarkMultiReturnAssignEnd:
                ProcessStackForMultiAssign();
                break;

            default:
                throw new VMException($"Unhandled return mode '{returnMode}'.");
                break;
            }

            return _callFrames.Count == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReturnOneValue(Value top)
        {
            _returnStack.Reset();
            _returnStack.Push(top);
            FinishReturnOp();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinishReturnOp()
        {
            CloseUpvalues(currentCallFrame.StackStart);
            PopFrameDiscardAndTransferReturns();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReturnFromMark()
        {
            var returnStart = currentCallFrame.ReturnStart;
            _returnStack.Reset();
            for (int i = returnStart; i < StackCount; i++)
            {
                _returnStack.Push(_valueStack[i]);
            }

            DiscardPop(StackCount - returnStart);

            FinishReturnOp();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PopFrameDiscardAndTransferReturns()
        {
            PopFrameAndDiscard();

            TransferReturnToStack();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PopFrameAndDiscard()
        {
            var prevStackStart = PopCallFrame();
            DiscardPopToCount(prevStackStart);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DiscardPopToCount(byte prevStackStart)
        {
            if (prevStackStart >= 0)
            {
                var toRemove = System.Math.Max(0, _valueStack.Count - prevStackStart);

                DiscardPop(toRemove);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TransferReturnToStack()
        {
            for (int i = 0; i < _returnStack.Count; i++)
            {
                Push(_returnStack[i]);
            }
        }

        //todo the returning function can tell us how many we are about to receive we don't need to track it separately?
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessStackForMultiAssign()
        {
            _returnStack.Reset();
            while (_valueStack.Count > currentCallFrame.ReturnStart)
                _returnStack.Push(Pop());

            for (int i = 0; i < _returnStack.Count; i++)
            {
                Push(_returnStack[i]);
            }
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
        protected void PushCallFrameFromValue(Value callee, int argCount)
        {
            switch (callee.type)
            {
            case ValueType.NativeFunction:
                PushFrameCallNative(callee.val.asNativeFunc, argCount);
                break;

            case ValueType.Closure:
                Call(callee.val.asClosure, argCount);
                break;

            default:
                if (!ExtendedCall(callee, argCount))
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
                StackStart = (byte)(_valueStack.Count - argCount - 1),
                Closure = closureInternal
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void PushFrameCallNative(NativeCallDelegate nativeCallDel, int argCount)
        {
            PushFrameCallNativeWithFixedStackStart(nativeCallDel, (byte)(_valueStack.Count - argCount - 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void PushFrameCallNativeWithFixedStackStart(NativeCallDelegate nativeCallDel, byte stackStart)
        {
            PushNewCallframe(new CallFrame()
            {
                StackStart = stackStart,
                Closure = NativeCallClosure,
                nativeFunc = nativeCallDel,
                InstructionPointer = 0
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoMathOp(OpCode opCode)
        {
            var rhs = Pop();
            var lhs = Pop();

            if(lhs.type == ValueType.Double
                && rhs.type == ValueType.Double)
            {
                DoDoubleMathOp(opCode, rhs, lhs);
                return;
            }

            if (lhs.type == ValueType.String
                || rhs.type == ValueType.String)
            {
                if (opCode == OpCode.ADD)
                {
                    Push(Value.New(lhs.str() + rhs.str()));
                    return;
                }
            }

            if (lhs.type != rhs.type)
                throw new VMException($"Cannot perform math op across types '{lhs.type}' and '{rhs.type}'.");

            if (DoCustomMathOp(opCode, lhs, rhs))
                return;

            throw new VMException($"Cannot perform math op on non math types '{lhs.type}' and '{rhs.type}'.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoDoubleMathOp(OpCode opCode, Value rhs, Value lhs)
        {
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

        protected virtual bool DoCustomMathOp(OpCode opCode, Value lhs, Value rhs) => false;

        protected virtual bool DoCustomComparisonOp(OpCode opCode, Value lhs, Value rhs) => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoComparisonOp(OpCode opCode)
        {
            var rhs = Pop();
            var lhs = Pop();

            if (DoCustomComparisonOp(opCode, lhs, rhs))
                return;

            //do we need specific handling of NaNs on either side
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DuplicateStackValuesNew(int startAt, int count)
        {
            for (int i = 0; i <= count; i++)
            {
                Push(_valueStack[startAt + i]);
            }
        }
    }
}
