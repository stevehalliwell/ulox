#define VM_STATS
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Vm
    {
        private readonly ClosureInternal NativeCallClosure;

        private readonly FastStack<Value> _valueStack = new FastStack<Value>();
        internal FastStack<Value> ValueStack => _valueStack;
        private readonly FastStack<CallFrame> _callFrames = new FastStack<CallFrame>();
        internal FastStack<CallFrame> CallFrames => _callFrames;
        private CallFrame _currentCallFrame;
        private Chunk _currentChunk;
        public Engine Engine { get; internal set; }
        private readonly LinkedList<Value> openUpvalues = new LinkedList<Value>();
        public Table Globals { get; private set; } = new Table();
        public TestRunner TestRunner { get; private set; } = new TestRunner(() => new Vm());
        public VmTracingReporter Tracing { get; set; }

        public Vm()
        {
            var nativeChunk = new Chunk("NativeCallChunkWrapper", "Native", "");
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
#if !FASTSTACK_FORCE_CLEAR
            _valueStack.DiscardPop(2);
            return (_valueStack.Peek(-2), _valueStack.Peek(-1));
#else
            return (_valueStack.Pop(), _valueStack.Pop());
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value Peek(int ind = 0) => _valueStack.Peek(ind);

        public Value GetArg(int index)
            => _valueStack[_currentCallFrame.StackStart + index];

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
        private ByteCodePacket ReadPacket(Chunk chunk)
            => chunk.Instructions[_currentCallFrame.InstructionPointer++];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushNewCallFrame(CallFrame callFrame)
        {
            if (_callFrames.Count > 0)
            {
                //save current state
                _callFrames.SetAt(_callFrames.Count - 1, _currentCallFrame);
            }

            _currentCallFrame = callFrame;
            _currentChunk = _currentCallFrame.Closure.chunk;
#if VM_STATS
            Tracing?.ProcessPushCallFrame(callFrame);
#endif
            _callFrames.Push(callFrame);
            for (int i = 0; i < callFrame.ReturnCount; i++)
            {
                ValueStack.Push(Value.Null());
            }
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

#if VM_STATS
                Tracing?.ProcessingOpCode(chunk, opCode);
#endif

                switch (opCode)
                {
                case OpCode.PUSH_CONSTANT:
                    Push(chunk.ReadConstant(packet.b1));
                    break;

                case OpCode.RETURN:
                    if (DoReturnOp())
                        return InterpreterResult.OK;

                    break;
                case OpCode.MULTI_VAR:
                    DoMultiVarOp(packet.b1, packet.b2);
                    break;
                case OpCode.YIELD:
                    return InterpreterResult.YIELD;

                case OpCode.NEGATE:
                    Push(Value.New(-PopOrLocal(packet.b1).val.asDouble));
                    break;

                case OpCode.ADD:
                {
                    var (rhs, lhs) = Pop2OrLocals(packet.b1, packet.b2);
                    var res = Value.Null();

                    if (lhs.type == ValueType.Double
                        && rhs.type == ValueType.Double)
                    {
                        res = Value.New(lhs.val.asDouble + rhs.val.asDouble);
                    }
                    else if (lhs.type == ValueType.String
                         || rhs.type == ValueType.String)
                    {
                        res = Value.New(lhs.str() + rhs.str());
                    }
                    else
                    {
                        ThrowRuntimeException($"Cannot perform op across types '{lhs.type}' and '{rhs.type}'");
                    }

                    SetLocalFromB3(packet.b3, res);
                    Push(res);
                }
                break;
                case OpCode.SUBTRACT:
                {
                    var (rhs, lhs) = Pop2OrLocals(packet.b1, packet.b2);
                    var res = Value.Null();

                    if (lhs.type == ValueType.Double
                        && rhs.type == ValueType.Double)
                    {
                        res = Value.New(lhs.val.asDouble - rhs.val.asDouble);
                    }
                    else
                    {
                        ThrowRuntimeException($"Cannot perform op across types '{lhs.type}' and '{rhs.type}'");
                    }

                    SetLocalFromB3(packet.b3, res);
                    Push(res);
                }
                break;
                case OpCode.MULTIPLY:
                {
                    var (rhs, lhs) = Pop2OrLocals(packet.b1, packet.b2);
                    var res = Value.Null();

                    if (lhs.type == ValueType.Double
                        && rhs.type == ValueType.Double)
                    {
                        res = Value.New(lhs.val.asDouble * rhs.val.asDouble);
                    }
                    else
                    {
                        ThrowRuntimeException($"Cannot perform op across types '{lhs.type}' and '{rhs.type}'");
                    }

                    SetLocalFromB3(packet.b3, res);
                    Push(res);
                }
                break;
                case OpCode.DIVIDE:
                {
                    var (rhs, lhs) = Pop2OrLocals(packet.b1, packet.b2);
                    var res = Value.Null();

                    if (lhs.type == ValueType.Double
                        && rhs.type == ValueType.Double)
                    {
                        res = Value.New(lhs.val.asDouble / rhs.val.asDouble);
                    }
                    else
                    {
                        ThrowRuntimeException($"Cannot perform op across types '{lhs.type}' and '{rhs.type}'");
                    }

                    SetLocalFromB3(packet.b3, res);
                    Push(res);
                }
                break;
                case OpCode.MODULUS:
                {
                    var (rhs, lhs) = Pop2OrLocals(packet.b1, packet.b2);
                    var res = Value.Null();

                    if (lhs.type == ValueType.Double
                        && rhs.type == ValueType.Double)
                    {
                        var lhsd = lhs.val.asDouble;
                        var rhsd = rhs.val.asDouble;
                        res = Value.New(((lhsd % rhsd) + rhsd) % rhsd);
                    }
                    else
                    {
                        ThrowRuntimeException($"Cannot perform op across types '{lhs.type}' and '{rhs.type}'");
                    }

                    SetLocalFromB3(packet.b3, res);
                    Push(res);
                }
                break;

                case OpCode.EQUAL:
                {
                    var (rhs, lhs) = Pop2OrLocals(packet.b1, packet.b2);
                    var res = Value.New(Value.Compare(ref lhs, ref rhs));
                    SetLocalFromB3(packet.b3, res);
                    Push(res);
                }
                break;

                case OpCode.LESS:
                {
                    var (rhs, lhs) = Pop2OrLocals(packet.b1, packet.b2);
                    var res = Value.Null();

                    if (lhs.type != ValueType.Instance)
                    {
                        if (lhs.type != ValueType.Double || rhs.type != ValueType.Double)
                            ThrowRuntimeException($"Cannot perform op '{opCode}' compare on different types '{lhs.type}' and '{rhs.type}'");

                        res = Value.New(lhs.val.asDouble < rhs.val.asDouble);
                    }
                    else
                    {
                        ThrowRuntimeException($"Cannot perform op '{opCode}' on user types '{lhs.val.asInstance.FromUserType}' and '{rhs.val.asInstance.FromUserType}'");
                    }

                    SetLocalFromB3(packet.b3, res);
                    Push(res);
                }
                break;
                case OpCode.GREATER:
                {
                    var (rhs, lhs) = Pop2OrLocals(packet.b1, packet.b2);
                    var res = Value.Null();

                    if (lhs.type != ValueType.Instance)
                    {
                        if (lhs.type != ValueType.Double || rhs.type != ValueType.Double)
                            ThrowRuntimeException($"Cannot perform op '{opCode}' compare on different types '{lhs.type}' and '{rhs.type}'");

                        res = Value.New(lhs.val.asDouble > rhs.val.asDouble);
                    }
                    else
                    {
                        ThrowRuntimeException($"Cannot perform op '{opCode}' on user types '{lhs.val.asInstance.FromUserType}' and '{rhs.val.asInstance.FromUserType}'");
                    }

                    SetLocalFromB3(packet.b3, res);
                    Push(res);
                }
                break;

                case OpCode.NOT:
                    Push(Value.New(PopOrLocal(packet.b1).IsFalsey()));
                    break;

                case OpCode.PUSH_VALUE:
                {
                    var pushType = (PushValueOpType)packet.b1;
                    switch (pushType)
                    {
                    case PushValueOpType.Null:
                        Push(Value.Null());
                        break;
                    case PushValueOpType.Bool:
                        Push(Value.New(packet.b2 == 1));
                        break;
                    case PushValueOpType.Byte:
                        Push(Value.New(packet.b2));
                        break;
                    case PushValueOpType.Bytes:
                        Push(Value.New(packet.b2));
                        Push(Value.New(packet.b3));
                        break;
                    default:
                        break;
                    }
                }
                break;

                case OpCode.POP:
                    _valueStack.DiscardPop(packet.b1);
                    break;

                case OpCode.DUPLICATE:
                {
                    var v = PopOrLocal(packet.b1);
                    Push(v);
                    Push(v);
                }
                break;

                case OpCode.GET_LOCAL:
                    var b3 = packet.b3;
                    var b2 = packet.b2;
                    var b1 = packet.b1;
                    _valueStack.Push(_valueStack[_currentCallFrame.StackStart + packet.b1]);
                    if (b2 != Optimiser.NOT_LOCAL_BYTE)
                        Push(_valueStack[_currentCallFrame.StackStart + b2]);
                    if (b3 != Optimiser.NOT_LOCAL_BYTE)
                        Push(_valueStack[_currentCallFrame.StackStart + b3]);
                    break;

                case OpCode.SET_LOCAL:
                    _valueStack[_currentCallFrame.StackStart + packet.b1] = _valueStack.Peek();
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
                        ThrowRuntimeException($"No global of name '{actualName}' could be found");
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
                    ThrowRuntimeException(Pop().ToString());
                    break;

                case OpCode.BUILD:
                    DoBuildOp();
                    break;

                case OpCode.NATIVE_CALL:
                    DoNativeCall(opCode);
                    break;

                case OpCode.VALIDATE:
                    DoValidateOp(packet.ValidateOp);
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

                case OpCode.INVOKE:
                    DoInvokeOp(chunk, packet);
                    break;

                case OpCode.TEST:
                    TestRunner.DoTestOpCode(this, chunk, packet.testOpDetails);
                    break;

                case OpCode.NATIVE_TYPE:
                    DoNativeTypeOp(chunk, packet.NativeType);
                    break;

                case OpCode.GET_INDEX:
                {
                    var (index, listValue) = Pop2OrLocals(packet.b1, packet.b2);
                    var res = Value.Null();
                    if (listValue.val.asInstance is INativeCollection nativeCol)
                    {
                        res = nativeCol.Get(index);
                    }
                    else
                    {
                        ThrowRuntimeException($"Cannot perform get index on type '{listValue.type}'");
                    }

                    SetLocalFromB3(packet.b3, res);
                    Push(res);
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

                case OpCode.COUNT_OF:
                {
                    var target = PopOrLocal(packet.b1);
                    DoCountOfOp(target);
                }
                break;

                case OpCode.GOTO:
                    _currentCallFrame.InstructionPointer = chunk.GetLabelPosition(packet.b1);
                    break;

                case OpCode.GOTO_IF_FALSE:
                    if (Peek().IsFalsey())
                        _currentCallFrame.InstructionPointer = chunk.GetLabelPosition(packet.b1);
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
                case OpCode.GET_FIELD:
                    DoGetFieldOp(chunk, packet.b1);
                    break;
                case OpCode.SET_FIELD:
                    DoSetFieldOp(chunk, packet.b1);
                    break;
                case OpCode.NONE:
                default:
                    ThrowRuntimeException($"Unhandled OpCode '{opCode}'.");
                    break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetLocalFromB3(byte localSetLocation, Value res)
        {
            if (localSetLocation != Optimiser.NOT_LOCAL_BYTE)
            {
                _valueStack[_currentCallFrame.StackStart + localSetLocation] = res;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Value PopOrLocal(byte b1)
        {
            return b1 == Optimiser.NOT_LOCAL_BYTE
                ? _valueStack.Pop()
                : _valueStack[_currentCallFrame.StackStart + b1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (Value rhs, Value lhs) Pop2OrLocals(byte b1, byte b2)
        {
            var rhs = b2 == Optimiser.NOT_LOCAL_BYTE ? Pop() : _valueStack[_currentCallFrame.StackStart + b2];
            var lhs = b1 == Optimiser.NOT_LOCAL_BYTE ? Pop() : _valueStack[_currentCallFrame.StackStart + b1];
            return (rhs, lhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (Value newValue, Value index, Value listValue) Pop3OrLocals(byte b1, byte b2, byte b3)
        {
            var newValue = b3 == Optimiser.NOT_LOCAL_BYTE ? Pop() : _valueStack[_currentCallFrame.StackStart + b3];
            var index = b2 == Optimiser.NOT_LOCAL_BYTE ? Pop() : _valueStack[_currentCallFrame.StackStart + b2];
            var listValue = b1 == Optimiser.NOT_LOCAL_BYTE ? Pop() : _valueStack[_currentCallFrame.StackStart + b1];
            return (newValue, index, listValue);
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
            }

            ThrowRuntimeException($"Cannot perform countof on '{target}'");
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
                }
                break;
            }
            ThrowRuntimeException($"Unsupported meets operation, got left hand side of type '{lhs.type}' and right hand side of type '{rhs.type}'");
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
        private void DoNativeTypeOp(Chunk chunk, NativeType nativeTypeRequested)
        {
            switch (nativeTypeRequested)
            {
            case NativeType.List:
                Push(NativeListClass.SharedNativeListClassValue);
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
            _valueStack.DiscardPop();
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
        private void DoValidateOp(ValidateOp validateOp)
        {
            switch (validateOp)
            {
            case ValidateOp.Meets:
            {
                var (rhs, lhs) = Pop2();
                var (meets, _) = ProcessContract(lhs, rhs);
                Push(Value.New(meets));
            }
            break;
            case ValidateOp.Signs:
            {
                var (rhs, lhs) = Pop2();
                var (meets, msg) = ProcessContract(lhs, rhs);
                if (!meets)
                    ThrowRuntimeException($"Sign failure with msg '{msg}'");
            }
            break;
            default:
                ThrowRuntimeException($"Unhandled validate op '{validateOp}'");
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoBuildOp()
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
            var closure = default(ClosureInternal);

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
        private bool DoReturnOp()
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
            var argCount = _valueStack.Count - _currentCallFrame.StackStart;
            var res = _currentCallFrame.nativeFunc.Invoke(this);

            if (res == NativeCallResult.SuccessfulExpression)
            {
                ProcessReturns();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessReturns()
        {
            var returnStart = _currentCallFrame.StackStart + _currentCallFrame.ArgCount + 1;
            var returnCount = _currentCallFrame.ReturnCount;
            if (_currentCallFrame.ReturnCount == 0)
            {
                returnStart = _currentCallFrame.StackStart;
                returnCount = 1;
            }

            CloseUpvalues(_currentCallFrame.StackStart);

            {
                var poppedStackStart = _currentCallFrame.StackStart;
                //remove top
                var popped = _callFrames.Pop();
#if VM_STATS
                Tracing?.ProcessPopCallFrame(popped);
#endif

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

                //transfer returns back down the stack
                for (var i = 0; i < returnCount; i++)
                    _valueStack[poppedStackStart + i] = _valueStack[returnStart + i];

                var toRemove = System.Math.Max(0, _valueStack.Count - poppedStackStart - returnCount);
                _valueStack.DiscardPop(toRemove);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoMultiVarOp(byte b1, byte b2)
        {
            if (b1 == 1)
                _currentCallFrame.MultiAssignStart = (byte)_valueStack.Count;
            else
            {
                //this is only so the multi return validate mechanism can continue to function,
                //it's not actually contributing to how multi return works.
                var returnCount = _valueStack.Count - _currentCallFrame.MultiAssignStart;
                var requestedResults = b2;
                var availableResults = returnCount;
                if (requestedResults != availableResults)
                    ThrowRuntimeException($"Multi var assign to result mismatch. Taking '{requestedResults}' but results contains '{availableResults}'");
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

            default:
                ThrowRuntimeException($"Invalid Call, value type {callee.type} is not handled");
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Call(ClosureInternal closureInternal, byte argCount)
        {
            if (argCount != closureInternal.chunk.Arity)
                ThrowRuntimeException($"Wrong number of params given to '{closureInternal.chunk.ChunkName}'" +
                    $", got '{argCount}' but expected '{closureInternal.chunk.Arity}'");


            var stackStart = (byte)System.Math.Max(0, _valueStack.Count - argCount - 1);
            PushNewCallFrame(new CallFrame()
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
            PushNewCallFrame(new CallFrame()
            {
                StackStart = (byte)(_valueStack.Count - argCount - 1),
                Closure = NativeCallClosure,
                nativeFunc = nativeCallDel,
                InstructionPointer = 0,
                ArgCount = argCount,
                ReturnCount = returnCount,
            });
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

            var instance = default(InstanceInternal);

            switch (targetVal.type)
            {
            case ValueType.UserType:
                instance = targetVal.val.asClass;
                break;

            case ValueType.Instance:
                instance = targetVal.val.asInstance;
                break;

            default:
                ThrowRuntimeException($"Only classes and instances have properties. Got a {targetVal.type} with value '{targetVal}'");
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

            if (fromClass == null)
            {
                //try to find a static method
                if (instance is UserTypeInternal userTypeInstance)
                {
                    if (userTypeInstance.Methods.Get(name, out var userTypeMethod))
                    {
                        Push(userTypeMethod);
                        return;
                    }
                    ThrowRuntimeException($"Undefined method '{name}', no method found on usertype'{userTypeInstance}'");
                }
                else
                {
                    ThrowRuntimeException($"Undefined property '{name}', cannot bind method as it has no fromClass");
                }
            }

            if (!fromClass.Methods.Get(name, out var method))
                ThrowRuntimeException($"Undefined method '{name}'");

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
        private void DoGetFieldOp(Chunk chunk, byte b1)
        {
            //Error checking is for comleteness, presenly this op is only emitted after validation that the property exists
            var argID = chunk.ReadConstant(b1);
            var target = _valueStack[_currentCallFrame.StackStart];
            if (target.type != ValueType.Instance)
                ThrowRuntimeException($"Cannot get field on non instance type '{target.type}'");

            var inst = target.val.asInstance;
            if (!inst.Fields.Get(argID.val.asString, out var val))
                ThrowRuntimeException($"No field of name '{argID.val.asString}' found on '{inst}'");

            Push(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoSetFieldOp(Chunk chunk, byte b1)
        {
            //Error checking is for comleteness, presenly this op is only emitted after validation that the property exists
            var argID = chunk.ReadConstant(b1);
            var target = _valueStack[_currentCallFrame.StackStart];
            if (target.type != ValueType.Instance)
                ThrowRuntimeException($"Cannot set field on non instance type '{target.type}'");

            var inst = target.val.asInstance;
            inst.SetField(argID.val.asString, Peek());
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
            var stackCount = _valueStack.Count;
            var instLocOnStack = (byte)(stackCount - argCount - 1);
            _valueStack[instLocOnStack] = inst;

            //InitNewInstance
            if (!asClass.Initialiser.IsNull())
            {
                //with an init list we don't return this
                PushCallFrameFromValue(asClass.Initialiser, argCount);
            }
            else if (argCount != 0)
            {
                ThrowRuntimeException($"Expected zero args for class '{asClass}', as it does not have an 'init' method but got {argCount} args");
            }

            foreach (var (chunk, labelID) in asClass.InitChains)
            {
                if (!asClass.Initialiser.IsNull())
                    Push(inst);

                PushNewCallFrame(new CallFrame()
                {
                    Closure = new ClosureInternal { chunk = chunk },
                    InstructionPointer = chunk.GetLabelPosition(labelID),
                    StackStart = (byte)(_valueStack.Count - 1), //last thing checked
                });
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void MoveInstructionPointerTo(ushort loc)
        {
            _currentCallFrame.InstructionPointer = loc;
        }

        public void PrepareTypes(TypeInfo typeInfo)
        {
            foreach (var type in typeInfo.Types)
            {
                if (Globals.Contains(new HashedString(type.Name))) continue;

                var klass = type.UserType == UserType.Class
                    ? new UserTypeInternal(type)
                    : new EnumClass(type);
                klass.PrepareFromType(this);
                var klassVal = Value.New(klass);
                Globals.AddOrSet(klass.Name, klassVal);
            }
        }
    }
}
