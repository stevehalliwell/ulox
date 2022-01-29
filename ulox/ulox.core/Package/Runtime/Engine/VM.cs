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
    public class Vm : IVm
    {
        public delegate NativeCallResult NativeCallDelegate(Vm vm, int argc);

        internal struct CallFrame
        {
            public int InstructionPointer;
            public byte StackStart;
            public byte ReturnStart;  //Used for return from mark and multi assign, as these do not overlap in the same callframe
            public ClosureInternal Closure;
            public NativeCallDelegate nativeFunc;
        }

        private readonly ClosureInternal NativeCallClosure;

        protected readonly FastStack<Value> _valueStack = new FastStack<Value>();
        protected readonly FastStack<Value> _returnStack = new FastStack<Value>();
        private readonly FastStack<CallFrame> _callFrames = new FastStack<CallFrame>();
        private CallFrame currentCallFrame;
        private IEngine _engine;
        public IEngine Engine => _engine;
        private readonly LinkedList<Value> openUpvalues = new LinkedList<Value>();
        private readonly Table _globals = new Table();
        public TestRunner TestRunner { get; protected set; } = new TestRunner(() => new Vm());
        public DiContainer DiContainer { get; private set; } = new DiContainer();

        public Vm()
        {
            var nativeChunk = new Chunk("NativeCallChunkWrapper", FunctionType.Function);
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

        public Value GetArg(int index)
            => _valueStack[currentCallFrame.StackStart + index];

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

        public virtual void CopyFrom(IVm otherVM)
        {
            _engine = otherVM.Engine;

            if (otherVM is Vm asVmBase)
            {
                foreach (var val in asVmBase._globals)
                {
                    SetGlobal(val.Key, val.Value);
                }

                TestRunner = asVmBase.TestRunner;
                DiContainer = asVmBase.DiContainer.ShallowCopy();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte(Chunk chunk)
            => chunk.Instructions[currentCallFrame.InstructionPointer++];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUShort(Chunk chunk)
        {
            var bhi = chunk.Instructions[currentCallFrame.InstructionPointer++];
            var blo = chunk.Instructions[currentCallFrame.InstructionPointer++];
            return (ushort)((bhi << 8) | blo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushNewCallframe(CallFrame callFrame)
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

                case OpCode.BUILD:
                    DoBuildOp(chunk);
                    break;

                case OpCode.NATIVE_CALL:
                    DoNativeCall(opCode);
                    break;

                case OpCode.VALIDATE:
                    DoValidateOp(chunk);
                    break;

                case OpCode.GET_PROPERTY:
                    DoGetPropertyOp(chunk);
                    break;

                case OpCode.SET_PROPERTY:
                    DoSetPropertyOp(chunk);
                    break;

                case OpCode.CLASS:
                    DoClassOp(chunk);
                    break;

                case OpCode.METHOD:
                    DoMethodOp(chunk);
                    break;

                case OpCode.MIXIN:
                    DoMixinOp(chunk);
                    break;

                case OpCode.INVOKE:
                    DoInvokeOp(chunk);
                    break;

                case OpCode.INHERIT:
                    DoInheritOp(chunk);
                    break;

                case OpCode.GET_SUPER:
                    DoGetSuperOp(chunk);
                    break;

                case OpCode.SUPER_INVOKE:
                    DoSuperInvokeOp(chunk);
                    break;

                case OpCode.TEST:
                    TestRunner.DoTestOpCode(this, chunk);
                    break;

                case OpCode.REGISTER:
                    DoRegisterOp(chunk);
                    break;

                case OpCode.INJECT:
                    DoInjectOp(chunk);
                    break;

                case OpCode.FREEZE:
                    DoFreezeOp();
                    break;
                case OpCode.NATIVE_TYPE:
                    DoNativeTypeOp(chunk);
                    break;
                case OpCode.GET_INDEX:
                    DoGetIndexOp();
                    break;
                case OpCode.SET_INDEX:
                    DoSetIndexOp();
                    break;
                case OpCode.TYPEOF:
                    DoTypeOfOp();
                    break;
                case OpCode.NONE:
                default:
                    throw new VMException($"Unhandled OpCode '{opCode}'.");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoTypeOfOp()
        {
            var target = Pop();
            Push(target.GetLoxClassType());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoSetIndexOp()
        {
            var newValue = Pop();
            var index = Pop();
            var listValue = Pop();
            var nativeCol = listValue.val.asInstance as INativeCollection;
            nativeCol.Set(index, newValue);
            Push(newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoGetIndexOp()
        {
            var index = Pop();
            var listValue = Pop();
            var nativeCol = listValue.val.asInstance as INativeCollection;
            Push(nativeCol.Get(index));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoNativeTypeOp(Chunk chunk)
        {
            var nativeTypeRequested = (NativeType)ReadByte(chunk);
            switch (nativeTypeRequested)
            {
            case NativeType.List:
                Push(NativeListClass.SharedNativeListClassValue);
                break;
            case NativeType.Map:
                Push(NativeMapClass.SharedNativeMapClassValue);
                break;
            case NativeType.Dynamic:
                Push(DynamicClass.SharedDynamicClassValue);
                break;
            default:
                throw new VMException($"Unhanlded native type creation '{nativeTypeRequested}'.");
            }

            PushCallFrameFromValue(Peek(0), 0);
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
            var givenVar = Pop();
            var str = givenVar.str();
            var buildOpType = (BuildOpType)ReadByte(chunk);
            switch (buildOpType)
            {
            case BuildOpType.Bind:
                Engine.Context.BindLibrary(str);
                break;

            case BuildOpType.Queue:
                Engine.LocateAndQueue(str);
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
        private void PushCallFrameFromValue(Value callee, int argCount)
        {
            switch (callee.type)
            {
            case ValueType.NativeFunction:
                PushFrameCallNative(callee.val.asNativeFunc, argCount);
                break;

            case ValueType.Closure:
                Call(callee.val.asClosure, argCount);
                break;

            case ValueType.Class:
                CreateInstance(callee.val.asClass, argCount);
                break;

            case ValueType.BoundMethod:
                CallMethod(callee.val.asBoundMethod, argCount);
                break;

            case ValueType.CombinedClosures:
                CallCombinedClosures(callee, argCount);
                break;

            default:
                throw new VMException($"Invalid Call, value type {callee.type} is not handled.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallCombinedClosures(Value callee, int argCount)
        {
            var combinedClosures = callee.val.asCombined;
            var stackCopyStartIndex = _valueStack.Count - argCount - 1;
            for (int i = 0; i < combinedClosures.Count; i++)
            {
                DuplicateStackValuesNew(stackCopyStartIndex, argCount);

                var closure = combinedClosures[i];
                Call(closure, argCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Call(ClosureInternal closureInternal, int argCount)
        {
            if (argCount != closureInternal.chunk.Arity)
                throw new VMException($"Wrong number of params given to '{closureInternal.chunk.Name}'" +
                    $", got '{argCount}' but expected '{closureInternal.chunk.Arity}'");

            if(closureInternal.chunk.FunctionType == FunctionType.PureFunction)
            {
                for (int i = 0; i < argCount; i++)
                    ValidatePureArg(i, closureInternal);
            }

            PushNewCallframe(new CallFrame()
            {
                StackStart = (byte)(_valueStack.Count - argCount - 1),
                Closure = closureInternal
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidatePureArg(int peekVal, ClosureInternal closureInternal)
        {
            var value = Peek(peekVal);
            if (!value.IsPure)
            {
                throw new VMException($"Pure call '{closureInternal.chunk.Name}' with non-pure confirming argument '{value}'.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushFrameCallNative(NativeCallDelegate nativeCallDel, int argCount)
        {
            PushFrameCallNativeWithFixedStackStart(nativeCallDel, (byte)(_valueStack.Count - argCount - 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushFrameCallNativeWithFixedStackStart(NativeCallDelegate nativeCallDel, byte stackStart)
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

            if (lhs.type == ValueType.Double
                && rhs.type == ValueType.Double)
            {
                DoDoubleMathOp(opCode, rhs, lhs);
                return;
            }

            if (opCode == OpCode.ADD
                &&
                (
                 lhs.type == ValueType.String
                 || rhs.type == ValueType.String)
                )
            {
                Push(Value.New(lhs.str() + rhs.str()));
                return;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoComparisonOp(OpCode opCode)
        {
            var rhs = Pop();
            var lhs = Pop();

            if (lhs.type == ValueType.Instance)
                if (DoCustomComparisonOp(opCode, lhs, rhs))
                    return;

            if (opCode == OpCode.EQUAL)
            {
                Push(Value.New(lhs.Compare(ref lhs, ref rhs)));
                return;
            }

            if (lhs.type != ValueType.Double || rhs.type != ValueType.Double)
                throw new VMException($"Cannot '{opCode}' compare on different types '{lhs.type}' and '{rhs.type}'.");

            //do we need specific handling of NaNs on either side
            switch (opCode)
            {
            case OpCode.LESS:
                Push(Value.New(lhs.val.asDouble < rhs.val.asDouble));
                break;

            case OpCode.GREATER:
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoClassOp(Chunk chunk)
        {
            var constantIndex = ReadByte(chunk);
            var name = chunk.ReadConstant(constantIndex);
            var klass = new ClassInternal(name.val.asString);
            var klassValue = Value.New(klass);
            Push(klassValue);
            var initChain = ReadUShort(chunk);
            if (initChain != 0)
            {
                klass.AddInitChain(currentCallFrame.Closure, initChain);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoFreezeOp()
        {
            var instVal = Pop();
            switch (instVal.type)
            {
            case ValueType.Instance:
                instVal.val.asInstance.Freeze();
                break;

            case ValueType.Class:
                instVal.val.asClass.Freeze();
                break;

            default:
                throw new VMException($"Freeze attempted on unsupported type '{instVal.type}'.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoInjectOp(Chunk chunk)
        {
            var constantIndex = ReadByte(chunk);
            var name = chunk.ReadConstant(constantIndex).val.asString;
            if (DiContainer.TryGetValue(name, out var found))
                Push(found);
            else
                throw new VMException($"Inject failure. Nothing has been registered (yet) with name '{name}'.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoRegisterOp(Chunk chunk)
        {
            var constantIndex = ReadByte(chunk);
            var name = chunk.ReadConstant(constantIndex).val.asString;
            var implementation = Pop();
            DiContainer.Set(name, implementation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoSuperInvokeOp(Chunk chunk)
        {
            var constantIndex = ReadByte(chunk);
            var methName = chunk.ReadConstant(constantIndex).val.asString;
            var argCount = ReadByte(chunk);
            var superClass = Pop().val.asClass;
            InvokeFromClass(superClass, methName, argCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoGetSuperOp(Chunk chunk)
        {
            var constantIndex = ReadByte(chunk);
            var name = chunk.ReadConstant(constantIndex).val.asString;
            var superClassVal = Pop();
            var superClass = superClassVal.val.asClass;
            BindMethod(superClass, name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoGetPropertyOp(Chunk chunk)
        {
            //use class to build a cached route to the field, introduce an cannot cache instruction
            //  once there are class vars this can be done through that as those are known and safe, not
            //  dynamically added at will to arbitrary objects.
            //if the class has the property name then we know it MUST be there, find it's index and then rewrite
            //  problem then is that the chunk could be given different object types, we need to fall back if a
            //  different type is given or generate variants for each type
            //the problem here is we don't know that the targetVal is of the same type that we are caching so
            //  turn it off for now.
            var targetVal = Peek();

            InstanceInternal instance = null;

            switch (targetVal.type)
            {
            default:
                throw new VMException($"Only classes and instances have properties. Got a {targetVal.type} with value '{targetVal}'.");
            case ValueType.Class:
                instance = targetVal.val.asClass;
                break;

            case ValueType.Instance:
                instance = targetVal.val.asInstance;
                break;
            }

            var constantIndex = ReadByte(chunk);
            var name = chunk.ReadConstant(constantIndex).val.asString;

            if (instance.TryGetField(name, out var val))
            {
                DiscardPop();
                Push(val);
                return;
            }

            BindMethod(instance.FromClass, name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoSetPropertyOp(Chunk chunk)
        {
            var targetVal = Peek(1);

            InstanceInternal instance = null;

            switch (targetVal.type)
            {
            default:
                throw new VMException($"Only classes and instances have properties. Got a {targetVal.type} with value '{targetVal}'.");
            case ValueType.Class:
                instance = targetVal.val.asClass;
                break;

            case ValueType.Instance:
                instance = targetVal.val.asInstance;
                break;
            }

            var constantIndex = ReadByte(chunk);
            var name = chunk.ReadConstant(constantIndex).val.asString;

            instance.SetField(name, Peek());

            var value = Pop();
            DiscardPop();
            Push(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoInvokeOp(Chunk chunk)
        {
            var constantIndex = ReadByte(chunk);
            var methodName = chunk.ReadConstant(constantIndex).val.asString;
            var argCount = ReadByte(chunk);

            var receiver = Peek(argCount);
            switch (receiver.type)
            {
            case ValueType.Instance:
                {
                    var inst = receiver.val.asInstance;

                    //it could be a field
                    if (inst.TryGetField(methodName, out var fieldFunc))
                    {
                        _valueStack[_valueStack.Count - 1 - argCount] = fieldFunc;
                        PushCallFrameFromValue(fieldFunc, argCount);
                    }
                    else
                    {
                        var fromClass = inst.FromClass;
                        if (!fromClass.TryGetMethod(methodName, out var method))
                        {
                            throw new VMException($"No method of name '{methodName}' found on '{fromClass}'.");
                        }

                        PushCallFrameFromValue(method, argCount);
                    }
                }
                break;

            case ValueType.Class:
                {
                    var klass = receiver.val.asClass;
                    PushCallFrameFromValue(klass.GetMethod(methodName), argCount);
                }
                break;

            default:
                throw new VMException($"Cannot invoke on '{receiver}'.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoMethodOp(Chunk chunk)
        {
            var constantIndex = ReadByte(chunk);
            var name = chunk.ReadConstant(constantIndex).val.asString;
            Value method = Peek();
            var klass = Peek(1).val.asClass;
            klass.AddMethod(name, method);
            DiscardPop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoMixinOp(Chunk chunk)
        {
            Value klass = Pop();
            Value mixin = Pop();
            klass.val.asClass.AddMixin(mixin);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoInheritOp(Chunk chunk)
        {
            var superVal = Peek(1);
            if (superVal.type != ValueType.Class)
                throw new VMException("Super class must be a class.");
            var superClass = superVal.val.asClass;

            var subVal = Peek();
            if (subVal.type != ValueType.Class)
                throw new VMException("Child class must be a class.");
            var subClass = subVal.val.asClass;

            subClass.InheritFrom(superClass);

            DiscardPop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BindMethod(ClassInternal fromClass, HashedString methodName)
        {
            if (!fromClass.TryGetMethod(methodName, out var method))
                throw new VMException($"Undefined property {methodName}");

            var receiver = Peek();
            var meth = method.val.asClosure;
            var bound = Value.New(new BoundMethod(receiver, meth));

            DiscardPop();
            Push(bound);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvokeFromClass(ClassInternal fromClass, HashedString methodName, int argCount)
        {
            if (!fromClass.TryGetMethod(methodName, out var method))
                throw new VMException($"No method of name '{methodName}' found on '{fromClass}'.");

            PushCallFrameFromValue(method, argCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallMethod(BoundMethod asBoundMethod, int argCount)
        {
            _valueStack[_valueStack.Count - 1 - argCount] = asBoundMethod.Receiver;
            Call(asBoundMethod.Method, argCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateInstance(ClassInternal asClass, int argCount)
        {
            var instInternal = asClass.MakeInstance();
            var inst = Value.New(instInternal);
            _valueStack[_valueStack.Count - 1 - argCount] = inst;

            InitNewInstance(asClass, argCount, inst, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitNewInstance(ClassInternal klass, int argCount, Value inst, bool isleaf)
        {
            var stackCount = _valueStack.Count;
            var instLocOnStack = (byte)(stackCount - argCount - 1);

            if (isleaf)
            {
                // Freeze is then called on the inst left behindby the
                //  using custom callframe as we need the location of the inst but no params, as they will all be gone
                //  by the time we get to execute.
                DuplicateStackValuesNew(instLocOnStack, argCount);
                PushFrameCallNativeWithFixedStackStart(ClassFinishCreation, instLocOnStack);
            }

            if (!klass.Initialiser.IsNull)
            {
                //with an init list we don't return this
                PushCallFrameFromValue(klass.Initialiser, argCount);

                //push a native call here so we can bind the fields to init param names
                if (klass.Initialiser.type == ValueType.Closure &&
                    klass.Initialiser.val.asClosure.chunk.Arity > 0)
                {
                    DuplicateStackValuesNew(instLocOnStack, argCount);
                    PushFrameCallNative(CopyMatchingParamsToFields, argCount);
                }
            }
            else if (argCount != 0)
            {
                throw new VMException("Args given for a class that does not have an 'init' method");
            }

            foreach (var initChain in klass.InitChains)
            {
                if (initChain.Item2 != -1)
                {
                    if (!klass.Initialiser.IsNull)
                        Push(inst);

                    PushNewCallframe(new CallFrame()
                    {
                        Closure = initChain.Item1,
                        InstructionPointer = initChain.Item2,
                        StackStart = (byte)(_valueStack.Count - 1), //last thing checked
                    });
                }
            }

            if (klass.Super != null)
            {
                var argsToSuperInit = PrepareSuperInit(klass, argCount, inst, stackCount);

                InitNewInstance(klass.Super, argsToSuperInit, inst, false);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private NativeCallResult CopyMatchingParamsToFields(Vm vm, int argCount)
        {
            var instVal = vm.GetArg(0);

            var inst = instVal.val.asInstance;

            var initChunk = inst.FromClass.Initialiser.val.asClosure.chunk;
            var argConstantIds = initChunk.ArgumentConstantIds;

            const int argOffset = 1;

            for (int i = 0; i < argConstantIds.Count; i++)
            {
                var arg = initChunk.Constants[i];
                if (arg.type == ValueType.String)
                {
                    var paramName = arg.val.asString;
                    if (inst.HasField(paramName))
                    {
                        var value = vm.GetArg(i + argOffset);
                        inst.SetField(paramName, value);
                    }
                }
            }

            return NativeCallResult.SuccessfulStatement;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private NativeCallResult ClassFinishCreation(Vm vm, int argCount)
        {
            var instVal = vm.GetArg(0);
            var inst = instVal.val.asInstance;
            inst.FromClass.FinishCreation(inst);
            vm.PushReturn(instVal);
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PrepareSuperInit(ClassInternal klass, int argCount, Value inst, int stackCount)
        {
            int argsToSuperInit = 0;
            if (!klass.Super.Initialiser.IsNull || klass.Super.InitChains.Count > 0)
            {
                //push inst and push args it expects
                Push(inst);
                if (!klass.Super.Initialiser.IsNull)
                {
                    argsToSuperInit = klass.Super.Initialiser.val.asClosure.chunk.Arity;
                    for (int i = 0; i < klass.Super.Initialiser.val.asClosure.chunk.Arity; i++)
                    {
                        Push(_valueStack[stackCount - argCount + i]);
                    }
                }
            }

            return argsToSuperInit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DoCustomMathOp(OpCode opCode, Value lhs, Value rhs)
        {
            if (lhs.type == ValueType.Instance)
            {
                var lhsInst = lhs.val.asInstance;
                var opClosure = lhsInst.FromClass.GetMathOpClosure(opCode);
                //identify if lhs has a matching method or field
                if (!opClosure.IsNull)
                {
                    CallOperatorOverloadedbyFunction(lhs, rhs, opClosure);
                    return true;
                }

                if (lhsInst.FromClass.Name == DynamicClass.DynamicClassName)
                {
                    return HandleDynamicCustomMathOp(opCode, lhs, rhs);
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DoCustomComparisonOp(OpCode opCode, Value lhs, Value rhs)
        {
            var lhsInst = lhs.val.asInstance;
            var opClosure = lhsInst.FromClass.GetCompareOpClosure(opCode);
            //identify if lhs has a matching method or field
            if (!opClosure.IsNull)
            {
                CallOperatorOverloadedbyFunction(lhs, rhs, opClosure);
                return true;
            }

            if (lhsInst.FromClass.Name == DynamicClass.DynamicClassName)
            {
                return HandleDynamicCustomCompOp(opCode, lhs, rhs);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallOperatorOverloadedbyFunction(Value lhs, Value rhs, Value opClosure)
        {
            Push(lhs);
            Push(lhs);
            Push(rhs);

            PushNewCallframe(new CallFrame()
            {
                Closure = opClosure.val.asClosure,
                StackStart = (byte)(_valueStack.Count - 3),
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HandleDynamicCustomMathOp(OpCode opCode, Value lhs, Value rhs)
        {
            var targetName = ClassInternal.MathOperatorMethodNames[(int)opCode - ClassInternal.FirstMathOp];
            if (lhs.val.asInstance.TryGetField(targetName, out var matchingValue))
            {
                if (matchingValue.type == ValueType.Closure)
                {
                    CallOperatorOverloadedbyFunction(lhs, rhs, matchingValue);
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HandleDynamicCustomCompOp(OpCode opCode, Value lhs, Value rhs)
        {
            var targetName = ClassInternal.ComparisonOperatorMethodNames[(int)opCode - ClassInternal.FirstCompOp];
            if (lhs.val.asInstance.TryGetField(targetName, out var matchingValue))
            {
                if (matchingValue.type == ValueType.Closure)
                {
                    CallOperatorOverloadedbyFunction(lhs, rhs, matchingValue);
                    return true;
                }
            }
            return false;
        }
    }
}
