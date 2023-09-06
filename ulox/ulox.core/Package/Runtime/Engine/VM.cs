using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Vm
    {
        private readonly ClosureInternal NativeCallClosure;

        private readonly FastStack<Value> _valueStack = new FastStack<Value>();
        internal FastStack<Value> ValueStack => _valueStack;
        private readonly FastStack<Value> _returnStack = new FastStack<Value>();
        internal FastStack<Value> ReturnStack => _returnStack;
        private readonly FastStack<CallFrame> _callFrames = new FastStack<CallFrame>();
        internal FastStack<CallFrame> CallFrames => _callFrames;
        private CallFrame _currentCallFrame;
        private Chunk _currentChunk;
        public Engine Engine { get; private set; }
        private readonly LinkedList<Value> openUpvalues = new LinkedList<Value>();
        public Table Globals { get; private set; } = new Table();
        public TestRunner TestRunner { get; private set; } = new TestRunner(() => new Vm());

        public Vm()
        {
            var nativeChunk = new Chunk("NativeCallChunkWrapper", "Native", FunctionType.Function);
            nativeChunk.WritePacket(new ByteCodePacket(OpCode.NATIVE_CALL), 0);
            NativeCallClosure = new ClosureInternal() { chunk = nativeChunk };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(Value val) => _valueStack.Push(val);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value Pop() => _valueStack.Pop();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (Value, Value) Pop2()
        {
#if FASTSTACK_FORCE_CLEAR
            _valueStack.DiscardPop(2);
            return (_valueStack.Peek(-2), _valueStack.Peek(-1));
#else
            return (_valueStack.Pop(), _valueStack.Pop());
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DiscardPop(int amt = 1) => _valueStack.DiscardPop(amt);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value Peek(int ind = 0) => _valueStack.Peek(ind);

        public Value GetArg(int index)
            => _valueStack[_currentCallFrame.StackStart + index];

        public Value GetNextArg(ref int index)
            => _valueStack[_currentCallFrame.StackStart + (++index)];

        public int CurrentFrameStackValues => _valueStack.Count - _currentCallFrame.StackStart;
        public Value StackTop => _valueStack.Peek();
        public int StackCount => _valueStack.Count;

        public void SetEngine(Engine engine) => Engine = engine;

        public InterpreterResult PushCallFrameAndRun(Value func, byte args)
        {
            PushCallFrameFromValue(func, args);
            return Run();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetNativeReturn(byte returnIndex, Value val)
        {
            _valueStack[_currentCallFrame.StackStart + _currentCallFrame.ArgCount + returnIndex + 1] = val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCurrentCallFrameToYieldOnReturn()
        {
            _currentCallFrame.YieldOnReturn = true;
        }

        public void CopyFrom(Vm otherVM)
        {
            Engine = otherVM.Engine;

            Globals.CopyFrom(otherVM.Globals);

            foreach (var val in otherVM._valueStack)
            {
                Push(val);
            }

            TestRunner = otherVM.TestRunner;
        }

        public void CopyStackFrom(Vm vm)
        {
            _valueStack.Reset();

            for (int i = 0; i < vm._valueStack.Count; i++)
            {
                _valueStack.Push(vm._valueStack[i]);
            }

            for (int i = 0; i < vm._callFrames.Count; i++)
            {
                _callFrames.Push(vm._callFrames[i]);
            }

            _currentCallFrame = vm._currentCallFrame;
            _currentChunk = vm._currentChunk;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ByteCodePacket ReadPacket(Chunk chunk)
            => chunk.Instructions[_currentCallFrame.InstructionPointer++];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushNewCallframe(CallFrame callFrame)
        {
            if (_callFrames.Count > 0)
            {
                //save current state
                _callFrames.SetAt(_callFrames.Count - 1, _currentCallFrame);
            }

            _currentCallFrame = callFrame;
            _currentChunk = _currentCallFrame.Closure.chunk;
            _callFrames.Push(callFrame);
            for (int i = 0; i < _currentChunk.ReturnCount; i++)
            {
                ValueStack.Push(Value.Null());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte PopCallFrame()
        {
            var poppedStackStart = _currentCallFrame.StackStart;
            //remove top
            _callFrames.Pop();

            //update cache
            if (_callFrames.Count > 0)
            {
                _currentCallFrame = _callFrames.Peek();
                _currentChunk = _currentCallFrame.Closure.chunk;
            }
            else
            {
                _currentCallFrame = default;
                _currentChunk = default;
            }
            return poppedStackStart;
        }

        public InterpreterResult Interpret(Chunk chunk)
        {
            //push this empty string to match the expectation of the function compiler
            Push(Value.New(""));
            Push(Value.New(new ClosureInternal() { chunk = chunk }));
            PushCallFrameFromValue(Peek(), 0);

            return Run();
        }

        public InterpreterResult Run(Program program)
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
                var chunk = _currentChunk;

                var packet = ReadPacket(chunk);
                var opCode = packet.OpCode;

                switch (opCode)
                {
                case OpCode.PUSH_CONSTANT:
                    Push(chunk.ReadConstant(packet.b1));
                    break;

                case OpCode.RETURN:
                    if (DoReturnOp(chunk))
                        return InterpreterResult.OK;

                    break;
                case OpCode.MULTI_VAR:
                    DoMultiVarOp(chunk, packet.BoolValue);
                    break;
                case OpCode.YIELD:
                    return InterpreterResult.YIELD;

                case OpCode.NEGATE:
                    Push(Value.New(-PopOrLocal(packet.b1).val.asDouble));
                    break;

                case OpCode.ADD:
                {
                    var (rhs, lhs) = Pop2OrLocals(packet.b1, packet.b2);

                    if (lhs.type == ValueType.Double
                        && rhs.type == ValueType.Double)
                    {
                        Push(Value.New(lhs.val.asDouble + rhs.val.asDouble));
                        break;
                    }

                    if (lhs.type == ValueType.String
                         || rhs.type == ValueType.String)
                    {
                        Push(Value.New(lhs.str() + rhs.str()));
                        break;
                    }

                    DoInstanceOverload(opCode, rhs, lhs);
                }
                break;
                case OpCode.SUBTRACT:
                {
                    var (rhs, lhs) = Pop2OrLocals(packet.b1, packet.b2);

                    if (lhs.type == ValueType.Double
                        && rhs.type == ValueType.Double)
                    {
                        Push(Value.New(lhs.val.asDouble - rhs.val.asDouble));
                        break;
                    }

                    DoInstanceOverload(opCode, rhs, lhs);
                }
                break;
                case OpCode.MULTIPLY:
                {
                    var (rhs, lhs) = Pop2OrLocals(packet.b1, packet.b2);

                    if (lhs.type == ValueType.Double
                        && rhs.type == ValueType.Double)
                    {
                        Push(Value.New(lhs.val.asDouble * rhs.val.asDouble));
                        break;
                    }

                    DoInstanceOverload(opCode, rhs, lhs);
                }
                break;
                case OpCode.DIVIDE:
                {
                    var (rhs, lhs) = Pop2OrLocals(packet.b1, packet.b2);

                    if (lhs.type == ValueType.Double
                        && rhs.type == ValueType.Double)
                    {
                        Push(Value.New(lhs.val.asDouble / rhs.val.asDouble));
                        break;
                    }

                    DoInstanceOverload(opCode, rhs, lhs);
                }
                break;
                case OpCode.MODULUS:
                {
                    var (rhs, lhs) = Pop2OrLocals(packet.b1, packet.b2);

                    if (lhs.type == ValueType.Double
                        && rhs.type == ValueType.Double)
                    {
                        Push(Value.New(lhs.val.asDouble % rhs.val.asDouble));
                        break;
                    }

                    DoInstanceOverload(opCode, rhs, lhs);
                }
                break;

                case OpCode.EQUAL:
                {
                    var (rhs, lhs) = Pop2OrLocals(packet.b1, packet.b2);

                    if (lhs.type != ValueType.Instance)
                    {
                        Push(Value.New(Value.Compare(ref lhs, ref rhs)));
                        break;
                    }

                    if (!DoCustomOverloadOp(opCode, lhs, rhs, Value.Null()))
                        Push(Value.New(Value.Compare(ref lhs, ref rhs)));
                }
                break;

                case OpCode.LESS:
                {
                    var (rhs, lhs) = Pop2OrLocals(packet.b1, packet.b2);

                    if (lhs.type != ValueType.Instance)
                    {
                        if (lhs.type != ValueType.Double || rhs.type != ValueType.Double)
                            ThrowRuntimeException($"Cannot '{opCode}' compare on different types '{lhs.type}' and '{rhs.type}'");

                        Push(Value.New(lhs.val.asDouble < rhs.val.asDouble));
                        break;
                    }

                    DoInstanceOverload(opCode, rhs, lhs);
                }
                break;
                case OpCode.GREATER:
                {
                    var (rhs, lhs) = Pop2OrLocals(packet.b1, packet.b2);

                    if (lhs.type != ValueType.Instance)
                    {
                        if (lhs.type != ValueType.Double || rhs.type != ValueType.Double)
                            ThrowRuntimeException($"Cannot '{opCode}' compare on different types '{lhs.type}' and '{rhs.type}'");

                        Push(Value.New(lhs.val.asDouble > rhs.val.asDouble));
                        break;
                    }

                    DoInstanceOverload(opCode, rhs, lhs);
                }
                break;

                case OpCode.NOT:
                    Push(Value.New(PopOrLocal(packet.b1).IsFalsey()));
                    break;

                case OpCode.PUSH_VALUE:
                {
                    switch (packet.pushValueDetails.ValueType)
                    {
                    case PushValueOpType.Null:
                        Push(Value.Null());
                        break;
                    case PushValueOpType.Bool:
                        Push(Value.New(packet.pushValueDetails._b));
                        break;
                    case PushValueOpType.Int:
                        Push(Value.New(packet.pushValueDetails._i));
                        break;
                    case PushValueOpType.Float:
                        //trick to prevent 1.2 turning into 1.2000000000046
                        Push(Value.New((double)(decimal)packet.pushValueDetails._f));
                        break;
                    default:
                        break;
                    }
                }
                break;

                case OpCode.POP:
                    DiscardPop(packet.b1);
                    break;

                case OpCode.SWAP:
                {
                    //swap last stack values
                    var count = _valueStack.Count;
                    var temp = _valueStack[count - 1];
                    _valueStack[count - 1] = _valueStack[count - 2];
                    _valueStack[count - 2] = temp;
                }
                break;

                case OpCode.DUPLICATE:
                {
                    var v = PopOrLocal(packet.b1);
                    Push(v);
                    Push(v);
                }
                break;

                case OpCode.GET_LOCAL:
                    Push(_valueStack[_currentCallFrame.StackStart + packet.b1]);
                    break;

                case OpCode.SET_LOCAL:
                    _valueStack[_currentCallFrame.StackStart + packet.b1] = Peek();
                    break;

                case OpCode.GET_UPVALUE:
                    DoGetUpvalueOp(chunk, packet.b1);
                    break;

                case OpCode.SET_UPVALUE:
                    DoSetUpvalueOp(chunk, packet.b1);
                    break;

                case OpCode.DEFINE_GLOBAL:
                {
                    var globalName = chunk.ReadConstant(packet.b1);
                    var popped = Pop();
                    Globals.AddOrSet(globalName.val.asString, popped);
                }
                break;

                case OpCode.FETCH_GLOBAL:
                {
                    var globalName = chunk.ReadConstant(packet.b1);
                    var actualName = globalName.val.asString;

                    if (Globals.Get(actualName, out var found))
                        Push(found);
                    else
                        ThrowRuntimeException($"No global of name {actualName} could be found");
                }
                break;

                case OpCode.ASSIGN_GLOBAL:
                    DoAssignGlobalOp(chunk, packet.b1);
                    break;

                case OpCode.CALL:
                {
                    var argCount = packet.b1;
                    PushCallFrameFromValue(Peek(argCount), argCount);
                }
                break;

                case OpCode.CLOSURE:
                    DoClosureOp(chunk, packet.closureDetails);
                    break;

                case OpCode.CLOSE_UPVALUE:
                    DoCloseUpvalueOp();
                    break;

                case OpCode.THROW:
                {
                    var frame = _callFrames.Peek();
                    var currentInstruction = frame.InstructionPointer;
                    throw new PanicException(
                        Pop().ToString(),
                        currentInstruction,
                        VmUtil.GetLocationNameFromFrame(frame, currentInstruction),
                        VmUtil.GenerateValueStackDump(this),
                        VmUtil.GenerateCallStackDump(this));
                }
                case OpCode.BUILD:
                    DoBuildOp(chunk);
                    break;

                case OpCode.NATIVE_CALL:
                    DoNativeCall(opCode);
                    break;

                case OpCode.VALIDATE:
                    DoValidateOp(chunk, packet.ValidateOp);
                    break;

                case OpCode.GET_PROPERTY:
                {
                    var targetVal = PopOrLocal(packet.b3);
                    DoGetPropertyOp(chunk, packet.b1, targetVal);
                }
                break;

                case OpCode.SET_PROPERTY:
                {
                    var (newVal, targetVal) = Pop2OrLocals(packet.b2, packet.b3);
                    DoSetPropertyOp(chunk, packet.b1, targetVal, newVal);
                }
                break;

                case OpCode.TYPE:
                    DoUserTypeOp(chunk, packet.typeDetails);
                    break;

                case OpCode.METHOD:
                    DoMethodOp(chunk, packet.b1);
                    break;

                case OpCode.FIELD:
                    DoFieldOp(chunk, packet.b1);
                    break;

                case OpCode.MIXIN:
                    DoMixinOp(chunk);
                    break;

                case OpCode.INVOKE:
                    DoInvokeOp(chunk, packet);
                    break;

                case OpCode.TEST:
                    TestRunner.DoTestOpCode(this, chunk, packet.testOpDetails);
                    break;

                case OpCode.FREEZE:
                    DoFreezeOp();
                    break;

                case OpCode.NATIVE_TYPE:
                    DoNativeTypeOp(chunk, packet.NativeType);
                    break;

                case OpCode.GET_INDEX:
                {
                    var (index, listValue) = Pop2OrLocals(packet.b1, packet.b2);
                    DoGetIndexOp(opCode, index, listValue);
                }
                break;

                case OpCode.SET_INDEX:
                {
                    var (newValue, index, listValue) = Pop3OrLocals(packet.b1, packet.b2, packet.b3);
                    DoSetIndexOp(opCode, newValue, index, listValue);
                }
                break;

                case OpCode.EXPAND_COPY_TO_STACK:
                    DoExpandCopyToStackOp(opCode);
                    break;

                case OpCode.TYPEOF:
                    DoTypeOfOp();
                    break;

                //can merge into validate, this is not perf critical
                case OpCode.MEETS:
                    DoMeetsOp();
                    break;

                //can merge into validate, this is not perf critical
                case OpCode.SIGNS:
                    DoSignsOp();
                    break;

                case OpCode.COUNT_OF:
                {
                    var target = PopOrLocal(packet.b1);
                    DoCountOfOp(target);
                }
                break;

                case OpCode.EXPECT:
                    DoExpectOp();
                    break;

                case OpCode.GOTO:
                    _currentCallFrame.InstructionPointer = chunk.GetLabelPosition(packet.b1);
                    break;

                case OpCode.GOTO_IF_FALSE:
                    if (Peek().IsFalsey())
                        _currentCallFrame.InstructionPointer = chunk.GetLabelPosition(packet.b1);
                    break;

                case OpCode.LABEL:
                    break;

                case OpCode.ENUM_VALUE:
                {
                    var (enumObject, val, key) = Pop3OrLocals(packet.b1, packet.b2, packet.b3);
                    (enumObject.val.asClass as EnumClass).AddEnumValue(key, val);
                }
                break;

                case OpCode.READ_ONLY:
                    DoReadOnlyOp(chunk);
                    break;

                case OpCode.UPDATE:
                    DoUpdateOp();
                    break;

                case OpCode.NONE:
                default:
                    ThrowRuntimeException($"Unhandled OpCode '{opCode}'.");
                    break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Value PopOrLocal(byte b1)
        {
            return b1 == ByteCodeOptimiser.NOT_LOCAL_BYTE ? Pop() : _valueStack[_currentCallFrame.StackStart + b1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (Value rhs, Value lhs) Pop2OrLocals(byte b1, byte b2)
        {
            var rhs = b2 == ByteCodeOptimiser.NOT_LOCAL_BYTE ? Pop() : _valueStack[_currentCallFrame.StackStart + b2];
            var lhs = b1 == ByteCodeOptimiser.NOT_LOCAL_BYTE ? Pop() : _valueStack[_currentCallFrame.StackStart + b1];
            return (rhs, lhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (Value newValue, Value index, Value listValue) Pop3OrLocals(byte b1, byte b2, byte b3)
        {
            var newValue = b3 == ByteCodeOptimiser.NOT_LOCAL_BYTE ? Pop() : _valueStack[_currentCallFrame.StackStart + b3];
            var index = b2 == ByteCodeOptimiser.NOT_LOCAL_BYTE ? Pop() : _valueStack[_currentCallFrame.StackStart + b2];
            var listValue = b1 == ByteCodeOptimiser.NOT_LOCAL_BYTE ? Pop() : _valueStack[_currentCallFrame.StackStart + b1];
            return (newValue, index, listValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoInstanceOverload(OpCode opCode, Value rhs, Value lhs)
        {
            if (lhs.type != rhs.type)
                ThrowRuntimeException($"Cannot perform op across types '{lhs.type}' and '{rhs.type}'");

            if (lhs.type != ValueType.Instance)
                ThrowRuntimeException($"Cannot perform op on non math types '{lhs.type}' and '{rhs.type}'");

            if (!DoCustomOverloadOp(opCode, lhs, rhs, Value.Null()))
                ThrowRuntimeException($"Cannot perform op '{opCode}' on user types '{lhs.val.asInstance.FromUserType}' and '{rhs.val.asInstance.FromUserType}'");
        }

        public void ThrowRuntimeException(string msg)
        {
            var frame = _currentCallFrame;
            var currentInstruction = frame.InstructionPointer;

            throw new RuntimeUloxException(msg,
                currentInstruction,
                VmUtil.GetLocationNameFromFrame(frame, currentInstruction),
                VmUtil.GenerateValueStackDump(this),
                VmUtil.GenerateCallStackDump(this));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoMeetsOp()
        {
            var (rhs, lhs) = Pop2();
            var (meets, _) = ProcessContract(lhs, rhs);
            Push(Value.New(meets));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoSignsOp()
        {
            var (rhs, lhs) = Pop2();
            var (meets, msg) = ProcessContract(lhs, rhs);
            if (!meets)
                ThrowRuntimeException($"Sign failure with msg '{msg}'");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoUpdateOp()
        {
            var (rhs, lhs) = Pop2();

            var res = Value.UpdateFrom(lhs, rhs, this);

            Push(res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoCountOfOp(Value target)
        {
            if (target.type == ValueType.Instance)
            {
                if (target.val.asInstance is INativeCollection col)
                {
                    Push(col.Count());
                    return;
                }
                else
                {
                    if (DoCustomOverloadOp(OpCode.COUNT_OF, target, Value.Null(), Value.Null()))
                    {
                        return;
                    }
                }
            }

            ThrowRuntimeException($"Cannot perform countof on '{target}'");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoExpectOp()
        {
            var (msg, expected) = Pop2();

            if (expected.IsFalsey())
            {
                ThrowRuntimeException($"Expect failed, '{msg}'");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoReadOnlyOp(Chunk chunk)
        {
            var target = Pop();
            if (target.type != ValueType.Instance
                && target.type != ValueType.UserType)
                ThrowRuntimeException($"Cannot perform readonly on '{target}'. Got unexpected type '{target.type}'");

            target.val.asInstance.ReadOnly();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (bool meets, string msg) ProcessContract(Value lhs, Value rhs)
        {
            switch (lhs.type)
            {
            case ValueType.UserType:
                return MeetValidator.ValidateClassMeetsClass(lhs.val.asClass, rhs.val.asClass);

            case ValueType.Instance:
                switch (rhs.type)
                {
                case ValueType.UserType:
                    return MeetValidator.ValidateInstanceMeetsClass(lhs.val.asInstance, rhs.val.asClass);

                case ValueType.Instance:
                    return MeetValidator.ValidateInstanceMeetsInstance(lhs.val.asInstance, rhs.val.asInstance);
                default:
                    ThrowRuntimeException($"Unsupported meets operation, got left hand side of type '{lhs.type}'");
                    break;
                }
                break;
            default:
                ThrowRuntimeException($"Unsupported meets operation, got left hand side of type '{lhs.type}'");
                break;
            }
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoTypeOfOp()
        {
            var target = Pop();
            Push(target.GetClassType());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoSetIndexOp(OpCode opCode, Value newValue, Value index, Value listValue)
        {
            if (listValue.val.asInstance is INativeCollection nativeCol)
            {
                nativeCol.Set(index, newValue);
                Push(newValue);
                return;
            }

            //attempt overload method call
            if (listValue.type == ValueType.Instance
                && DoCustomOverloadOp(opCode, listValue, index, newValue))
                return;

            ThrowRuntimeException($"Cannot perform set index on type '{listValue.type}'");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoExpandCopyToStackOp(OpCode opCode)
        {
            var v = Pop();
            if (v.type == ValueType.Instance
                && v.val.asInstance is NativeListInstance nativeList)
            {
                var l = nativeList.List;
                for (int i = 0; i < l.Count; i++)
                {
                    Push(l[i]);
                }
            }
            else
            {
                Push(v);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoGetIndexOp(OpCode opCode, Value index, Value listValue)
        {
            if (listValue.val.asInstance is INativeCollection nativeCol)
            {
                Push(nativeCol.Get(index));
                return;
            }

            //attempt overload method call
            if (listValue.type == ValueType.Instance)
            {
                if (DoCustomOverloadOp(opCode, listValue, index, Value.Null()))
                    return;
            }

            ThrowRuntimeException($"Cannot perform get index on type '{listValue.type}'");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoNativeTypeOp(Chunk chunk, NativeType nativeTypeRequested)
        {
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
                ThrowRuntimeException($"Unhanlded native type creation '{nativeTypeRequested}'");
                break;
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
        private void DoSetUpvalueOp(Chunk chunk, byte slot)
        {
            var upval = _currentCallFrame.Closure.upvalues[slot].val.asUpvalue;
            if (!upval.isClosed)
                _valueStack[upval.index] = Peek();
            else
                upval.value = Peek();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoGetUpvalueOp(Chunk chunk, byte slot)
        {
            var upval = _currentCallFrame.Closure.upvalues[slot].val.asUpvalue;
            if (!upval.isClosed)
                Push(_valueStack[upval.index]);
            else
                Push(upval.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoValidateOp(Chunk chunk, ValidateOp validateOp)
        {
            switch (validateOp)
            {
            case ValidateOp.MultiReturnMatches:
                var requestedResultsValue = Pop();
                var requestedResults = (int)requestedResultsValue.val.asDouble;
                var availableResults = _returnStack.Count;
                if (requestedResults != availableResults)
                    ThrowRuntimeException($"Multi var assign to result mismatch. Taking '{requestedResults}' but results contains '{availableResults}'");
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
            Engine.LocateAndQueue(str);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoAssignGlobalOp(Chunk chunk, byte globalId)
        {
            var globalName = chunk.ReadConstant(globalId);
            var actualName = globalName.val.asString;
            if (!Globals.Contains(actualName))
            {
                ThrowRuntimeException($"Global var of name '{actualName}' was not found");
            }
            Globals.Set(actualName, Peek());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoClosureOp(Chunk chunk, ByteCodePacket.ClosureDetails closureDetails)
        {
            var type = closureDetails.ClosureType;
            var b1 = closureDetails.b1;
            var b2 = closureDetails.b2;
            ClosureInternal closure = default;

            if (type != ClosureType.Closure)
                ThrowRuntimeException($"Closure type '{type}' unexpected.");

            var constantIndex = b1;
            var func = chunk.ReadConstant(constantIndex);
            var closureVal = Value.New(new ClosureInternal() { chunk = func.val.asChunk });
            Push(closureVal);

            closure = closureVal.val.asClosure;

            if (b2 != closure.upvalues.Length)
                ThrowRuntimeException($"Closure upvalue count mismatch. Expected '{b2}' but got '{closure.upvalues.Length}'");

            for (int i = 0; i < closure.upvalues.Length; i++)
            {
                var packet = ReadPacket(chunk);
                var isLocal = packet.closureDetails.b1;
                var index = packet.closureDetails.b2;
                if (isLocal == 1)
                {
                    var local = _currentCallFrame.StackStart + index;
                    closure.upvalues[i] = CaptureUpvalue(local);
                }
                else
                {
                    closure.upvalues[i] = _currentCallFrame.Closure.upvalues[index];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DoReturnOp(Chunk chunk)
        {
            var origCallFrameCount = _callFrames.Count;
            var wantsToYieldOnReturn = _currentCallFrame.YieldOnReturn;

            ProcessReturns();

            return _callFrames.Count == 0
                || (_callFrames.Count < origCallFrameCount && wantsToYieldOnReturn);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoNativeCall(OpCode opCode)
        {
            if (_currentCallFrame.nativeFunc == null)
                ThrowRuntimeException($"{opCode} without nativeFunc encountered. This is not allowed");
            var argCount = CurrentFrameStackValues;
            var res = _currentCallFrame.nativeFunc.Invoke(this);

            if (res == NativeCallResult.SuccessfulExpression)
            {
                ProcessReturns();
            }
            else if (res == NativeCallResult.SuccessfulStatement)
            {
                PopFrameAndDiscard();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessReturns()
        {
            _returnStack.Reset();
            if (_currentCallFrame.ReturnCount != 0)
            {
                var returnStart = _currentCallFrame.StackStart + _currentCallFrame.ArgCount + 1;
                for (int i = 0; i < _currentCallFrame.ReturnCount; i++)
                {
                    _returnStack.Push(_valueStack[returnStart + i]);
                }
            }
            else
            {
                _returnStack.Push(ValueStack[_currentCallFrame.StackStart]);
            }
            FinishReturnOp();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoMultiVarOp(Chunk chunk, bool start)
        {
            if (start)
                _currentCallFrame.MultiAssignStart = (byte)StackCount;
            else
                CacheStackForMultiAssignValidation();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinishReturnOp()
        {
            CloseUpvalues(_currentCallFrame.StackStart);

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
            var toRemove = System.Math.Max(0, _valueStack.Count - prevStackStart);

            DiscardPop(toRemove);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TransferReturnToStack()
        {
            for (int i = 0; i < _returnStack.Count; i++)
            {
                Push(_returnStack[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CacheStackForMultiAssignValidation()
        {
            //this is only so the multi return validate mechanism can continue to function,
            //it's not actually contributing to how multi return works.
            _returnStack.Reset();
            var returnCount = _valueStack.Count - _currentCallFrame.MultiAssignStart;
            for (int i = 0; i < returnCount; i++)
            {
                _returnStack.Push(Value.Null());
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
        public void PushCallFrameFromValue(Value callee, byte argCount)
        {
            switch (callee.type)
            {
            case ValueType.NativeFunction:
                if (argCount != callee.val.asByte1)
                    ThrowRuntimeException($"Native function '{callee.val.asNativeFunc.Method.Name}' expected '{callee.val.asByte1}' arguments but got '{argCount}'");

                PushFrameCallNative(callee.val.asNativeFunc, argCount, callee.val.asByte0);
                break;

            case ValueType.Closure:
                Call(callee.val.asClosure, argCount);
                break;

            case ValueType.UserType:
                CreateInstance(callee.val.asClass, argCount);
                break;

            case ValueType.BoundMethod:
                CallMethod(callee.val.asBoundMethod, argCount);
                break;

            case ValueType.CombinedClosures:
                CallCombinedClosures(callee, argCount);
                break;

            default:
                ThrowRuntimeException($"Invalid Call, value type {callee.type} is not handled");
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallCombinedClosures(Value callee, byte argCount)
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
        private void Call(ClosureInternal closureInternal, byte argCount)
        {
            if (argCount != closureInternal.chunk.Arity)
                ThrowRuntimeException($"Wrong number of params given to '{closureInternal.chunk.Name}'" +
                    $", got '{argCount}' but expected '{closureInternal.chunk.Arity}'");


            var stackStart = (byte)System.Math.Max(0, _valueStack.Count - argCount - 1);
            PushNewCallframe(new CallFrame()
            {
                StackStart = stackStart,
                Closure = closureInternal,
                ArgCount = argCount,
                ReturnCount = closureInternal.chunk.ReturnCount
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushFrameCallNative(CallFrame.NativeCallDelegate nativeCallDel, byte argCount, byte returnCount)
        {
            PushFrameCallNativeWithFixedStackStart(nativeCallDel, (byte)(_valueStack.Count - argCount - 1), argCount, returnCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushFrameCallNativeWithFixedStackStart(CallFrame.NativeCallDelegate nativeCallDel, byte stackStart, byte argCount, byte returnCount)
        {
            PushNewCallframe(new CallFrame()
            {
                StackStart = stackStart,
                Closure = NativeCallClosure,
                nativeFunc = nativeCallDel,
                InstructionPointer = 0,
                ArgCount = argCount,
                ReturnCount = returnCount,
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DuplicateStackValuesNew(int startAt, int count)
        {
            for (int i = 0; i <= count; i++)
            {
                Push(_valueStack[startAt + i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoUserTypeOp(Chunk chunk, ByteCodePacket.TypeDetails typeDetails)
        {
            var constantIndex = typeDetails.stringConstantId;
            var name = chunk.ReadConstant(constantIndex);
            var userType = typeDetails.UserType;
            UserTypeInternal klass = userType == UserType.Enum
                ? new EnumClass(name.val.asString)
                : new UserTypeInternal(name.val.asString, userType);
            var klassValue = Value.New(klass);
            Push(klassValue);
            var initChainLabelID = typeDetails.initLabelId;
            var initChain = chunk.Labels[initChainLabelID];
            if (initChain != 0)
            {
                klass.AddInitChain(_currentCallFrame.Closure, (ushort)initChain);
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

            case ValueType.UserType:
                instVal.val.asClass.Freeze();
                break;

            default:
                ThrowRuntimeException($"Freeze attempted on unsupported type '{instVal.type}'");
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoGetPropertyOp(Chunk chunk, byte constantIndex, Value targetVal)
        {
            //use class to build a cached route to the field, introduce an cannot cache instruction
            //  once there are class vars this can be done through that as those are known and safe, not
            //  dynamically added at will to arbitrary objects.
            //if the class has the property name then we know it MUST be there, find it's index and then rewrite
            //  problem then is that the chunk could be given different object types, we need to fall back if a
            //  different type is given or generate variants for each type
            //the problem here is we don't know that the targetVal is of the same type that we are caching so
            //  turn it off for now.

            InstanceInternal instance = null;

            switch (targetVal.type)
            {
            default:
                ThrowRuntimeException($"Only classes and instances have properties. Got a {targetVal.type} with value '{targetVal}'");
                break;
            case ValueType.UserType:
                instance = targetVal.val.asClass;
                break;

            case ValueType.Instance:
                instance = targetVal.val.asInstance;
                break;
            }

            var name = chunk.ReadConstant(constantIndex).val.asString;

            if (instance.Fields.Get(name, out var val))
            {
                Push(val);
                return;
            }

            //attempt to bind the method
            var fromClass = instance.FromUserType;
            var methodName = name;

            if (fromClass == null)
                ThrowRuntimeException($"Cannot bind method '{methodName}', there is no fromClass");

            if (!fromClass.Methods.Get(methodName, out var method))
                ThrowRuntimeException($"Undefined property '{methodName}'");

            var receiver = targetVal;
            var meth = method.val.asClosure;
            var bound = Value.New(new BoundMethod(receiver, meth));

            Push(bound);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoSetPropertyOp(Chunk chunk, byte constantIndex, Value targetVal, Value newVal)
        {
            InstanceInternal instance = null;

            switch (targetVal.type)
            {
            default:
                ThrowRuntimeException($"Only classes and instances have properties. Got a {targetVal.type} with value '{targetVal}'");
                break;
            case ValueType.UserType:
                instance = targetVal.val.asClass;
                break;

            case ValueType.Instance:
                instance = targetVal.val.asInstance;
                break;
            }

            var name = chunk.ReadConstant(constantIndex).val.asString;

            instance.SetField(name, newVal);

            Push(newVal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoInvokeOp(Chunk chunk, ByteCodePacket packet)
        {
            var constantIndex = packet.b1;
            var argCount = packet.b2;

            var methodName = chunk.ReadConstant(constantIndex).val.asString;

            var receiver = Peek(argCount);
            switch (receiver.type)
            {
            case ValueType.Instance:
            {
                var inst = receiver.val.asInstance;

                //it could be a field
                if (inst.Fields.Get(methodName, out var fieldFunc))
                {
                    _valueStack[_valueStack.Count - 1 - argCount] = fieldFunc;
                    PushCallFrameFromValue(fieldFunc, argCount);
                }
                else
                {
                    var fromClass = inst.FromUserType;
                    if (fromClass == null)
                    {
                        ThrowRuntimeException($"Cannot invoke '{methodName}' on '{receiver}' with no class");
                    }

                    if (!fromClass.Methods.Get(methodName, out var method))
                    {
                        ThrowRuntimeException($"No method of name '{methodName}' found on '{fromClass}'");
                    }

                    PushCallFrameFromValue(method, argCount);
                }
            }
            break;

            case ValueType.UserType:
            {
                var klass = receiver.val.asClass;
                klass.Methods.Get(methodName, out var methObj);
                PushCallFrameFromValue(methObj, argCount);
            }
            break;

            default:
                ThrowRuntimeException($"Cannot invoke '{methodName}' on '{receiver}'");
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoMethodOp(Chunk chunk, byte constantIndex)
        {
            var name = chunk.ReadConstant(constantIndex).val.asString;
            Value method = Peek();
            var klass = Peek(1).val.asClass;
            klass.AddMethod(name, method, this);
            DiscardPop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoFieldOp(Chunk chunk, byte constantIndex)
        {
            var klass = Pop().val.asClass;
            klass.AddFieldName(chunk.ReadConstant(constantIndex).val.asString);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoMixinOp(Chunk chunk)
        {
            //pop2
            var (klass, mixin) = Pop2();
            klass.val.asClass.MixinClass(mixin, this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallMethod(BoundMethod asBoundMethod, byte argCount)
        {
            _valueStack[_valueStack.Count - 1 - argCount] = asBoundMethod.Receiver;
            Call(asBoundMethod.Method, argCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateInstance(UserTypeInternal asClass, byte argCount)
        {
            var instInternal = asClass.MakeInstance();
            var inst = Value.New(instInternal);
            _valueStack[_valueStack.Count - 1 - argCount] = inst;

            InitNewInstance(asClass, argCount, inst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitNewInstance(UserTypeInternal klass, byte argCount, Value inst)
        {
            var stackCount = _valueStack.Count;
            var instLocOnStack = (byte)(stackCount - argCount - 1);

            DuplicateStackValuesNew(instLocOnStack, argCount);
            PushFrameCallNativeWithFixedStackStart(ClassFinishCreation, instLocOnStack, argCount, 1);

            if (!klass.Initialiser.IsNull())
            {
                //with an init list we don't return this
                PushCallFrameFromValue(klass.Initialiser, argCount);

                //push a native call here so we can bind the fields to init param names
                if (klass.Initialiser.type == ValueType.Closure &&
                    klass.Initialiser.val.asClosure.chunk.Arity > 0)
                {
                    DuplicateStackValuesNew(instLocOnStack, argCount);
                    PushFrameCallNative(CopyMatchingParamsToFields, argCount, 1);
                }
            }
            else if (argCount != 0)
            {
                ThrowRuntimeException($"Expected zero args for class '{klass}', as it does not have an 'init' method but got {argCount} args");
            }

            foreach (var (closure, instruction) in klass.InitChains)
            {
                if (!klass.Initialiser.IsNull())
                    Push(inst);

                PushNewCallframe(new CallFrame()
                {
                    Closure = closure,
                    InstructionPointer = instruction,
                    StackStart = (byte)(_valueStack.Count - 1), //last thing checked
                });
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private NativeCallResult CopyMatchingParamsToFields(Vm vm)
        {
            var instVal = vm.GetArg(0);

            var inst = instVal.val.asInstance;

            var initChunk = inst.FromUserType.Initialiser.val.asClosure.chunk;
            var argConstantIds = initChunk.ArgumentConstantIds;

            const int argOffset = 1;

            for (int i = 0; i < argConstantIds.Count; i++)
            {
                var arg = initChunk.Constants[i];
                if (arg.type == ValueType.String)
                {
                    var paramName = arg.val.asString;
                    if (inst.Fields.Contains(paramName))
                    {
                        var value = vm.GetArg(i + argOffset);
                        inst.SetField(paramName, value);
                    }
                }
            }

            return NativeCallResult.SuccessfulStatement;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private NativeCallResult ClassFinishCreation(Vm vm)
        {
            var instVal = vm.GetArg(0);
            var inst = instVal.val.asInstance;
            inst.FromUserType.FinishCreation(inst);
            vm.SetNativeReturn(0, instVal);
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DoCustomOverloadOp(OpCode opCode, Value self, Value arg1, Value arg2)
        {
            var lhsInst = self.val.asInstance;
            var opClosure = lhsInst.FromUserType.GetOverloadClosure(opCode);
            //identify if lhs has a matching method or field
            if (!opClosure.IsNull())
            {
                CallOperatorOverloadedbyFunction(opClosure.val.asClosure, self, arg1, arg2);
                return true;
            }

            if (lhsInst.FromUserType.Name == DynamicClass.DynamicClassName)
            {
                var targetName = UserTypeInternal.OverloadableMethodNames[UserTypeInternal.OpCodeToOverloadIndex[opCode]];
                if (self.val.asInstance.Fields.Get(targetName, out var matchingValue))
                {
                    if (matchingValue.type == ValueType.Closure)
                    {
                        CallOperatorOverloadedbyFunction(matchingValue.val.asClosure, self, arg1, arg2);
                        return true;
                    }
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallOperatorOverloadedbyFunction(ClosureInternal opClosure, Value self, Value arg1, Value arg2)
        {
            //presently we support multiple forms of overloads
            //  math and comparison, taking self and other
            //  get index, taking self and index
            //  set index taking self, index, value
            // the arg count tells us which one
            Push(self);

            var arity = opClosure.chunk.Arity;

            switch (opClosure.chunk.Arity)
            {
            case 1:
                Push(self);
                break;
            case 2:
                Push(self);
                Push(arg1);
                break;
            case 3:
                Push(self);
                Push(arg1);
                Push(arg2);
                break;
            }

            PushNewCallframe(new CallFrame()
            {
                Closure = opClosure,
                StackStart = (byte)(_valueStack.Count - 1 - arity),
                ArgCount = arity,
                ReturnCount = 1,
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void MoveInstructionPointerTo(ushort loc)
        {
            _currentCallFrame.InstructionPointer = loc;
        }
    }
}
