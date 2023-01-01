using System.Collections.Generic;
using System.Linq;
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
    public sealed class Vm
    {
        public delegate NativeCallResult NativeCallDelegate(Vm vm, int argc);

        internal struct CallFrame
        {
            public int InstructionPointer;
            public byte StackStart;
            public byte ReturnStart;  //Used for return from mark and multi assign, as these do not overlap in the same callframe
            public ClosureInternal Closure;
            public NativeCallDelegate nativeFunc;
            public bool YieldOnReturn;
        }

        private readonly ClosureInternal NativeCallClosure;

        private readonly FastStack<Value> _valueStack = new FastStack<Value>();
        private readonly FastStack<Value> _returnStack = new FastStack<Value>();
        private readonly FastStack<CallFrame> _callFrames = new FastStack<CallFrame>();
        private CallFrame _currentCallFrame;
        private Chunk _currentChunk;
        public Engine Engine { get; private set; }
        private readonly LinkedList<Value> openUpvalues = new LinkedList<Value>();
        private readonly Table _globals = new Table();
        public TestRunner TestRunner { get; private set; } = new TestRunner(() => new Vm());
        public DiContainer DiContainer { get; private set; } = new DiContainer();

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
        private void DiscardPop(int amt = 1) => _valueStack.DiscardPop(amt);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value Peek(int ind = 0) => _valueStack.Peek(ind);

        public string GenerateValueStackDump() => DumpStack(_valueStack);

        public string GenerateReturnDump() => DumpStack(_returnStack);

        public string GenerateGlobalsDump()
        {
            var sb = new System.Text.StringBuilder();

            foreach (var item in _globals)
            {
                sb.Append($"{item.Key} : {item.Value}");
            }

            return sb.ToString();
        }

        public string GenerateCallStackDump()
        {
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < _callFrames.Count; i++)
            {
                var cf = _callFrames.Peek(i);
                sb.AppendLine(GetLocationNameFromFrame(cf));
            }

            return sb.ToString();
        }

        public Value GetGlobal(HashedString name) => _globals[name];

        public void SetGlobal(HashedString name, Value val) => _globals[name] = val;

        public Value GetArg(int index)
            => _valueStack[_currentCallFrame.StackStart + index];

        public int CurrentFrameStackValues => _valueStack.Count - _currentCallFrame.StackStart;
        public Value StackTop => _valueStack.Peek();
        public int StackCount => _valueStack.Count;

        public void SetEngine(Engine engine) => Engine = engine;

        public InterpreterResult PushCallFrameAndRun(Value func, int args)
        {
            PushCallFrameFromValue(func, args);
            return Run();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushReturn(Value val)
        {
            if (_currentCallFrame.ReturnStart == 0)
                _currentCallFrame.ReturnStart = (byte)StackCount;

            _valueStack.Push(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCurrentCallFrameToYieldOnReturn()
        {
            _currentCallFrame.YieldOnReturn = true;
        }

        public void CopyFrom(Vm otherVM)
        {
            Engine = otherVM.Engine;

            foreach (var val in otherVM._globals)
            {
                SetGlobal(val.Key, val.Value);
            }

            TestRunner = otherVM.TestRunner;
            DiContainer = otherVM.DiContainer.ShallowCopy();
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
                case OpCode.CONSTANT:
                    Push(chunk.ReadConstant(packet.b1));
                    break;

                case OpCode.RETURN:
                    if (DoReturnOp(chunk, packet.ReturnMode))
                        return InterpreterResult.OK;

                    break;

                case OpCode.YIELD:
                    return InterpreterResult.YIELD;

                case OpCode.NEGATE:
                    Push(Value.New(-Pop().val.asDouble));
                    break;

                case OpCode.ADD:
                {
                    var rhs = Pop();
                    var lhs = Pop();

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

                    if (lhs.type != rhs.type)
                        ThrowRuntimeException($"Cannot perform math op across types '{lhs.type}' and '{rhs.type}'");

                    if (lhs.type != ValueType.Instance)
                        ThrowRuntimeException($"Cannot perform math op on non math types '{lhs.type}' and '{rhs.type}'");

                    if (!DoCustomOverloadOp(opCode, lhs, rhs, Value.Null()))
                        ThrowRuntimeException($"Cannot perform math op '{opCode}' on user types '{lhs.val.asInstance.FromUserType}' and '{rhs.val.asInstance.FromUserType}'");

                }
                break;
                case OpCode.SUBTRACT:
                case OpCode.MULTIPLY:
                case OpCode.DIVIDE:
                case OpCode.MODULUS:
                {
                    var rhs = Pop();
                    var lhs = Pop();

                    if (lhs.type == ValueType.Double
                        && rhs.type == ValueType.Double)
                    {
                        var res = Value.New(0);
                        switch (opCode)
                        {
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
                        break;
                    }

                    if (lhs.type != rhs.type)
                        ThrowRuntimeException($"Cannot perform math op across types '{lhs.type}' and '{rhs.type}'");

                    if (lhs.type != ValueType.Instance)
                        ThrowRuntimeException($"Cannot perform math op on non math types '{lhs.type}' and '{rhs.type}'");

                    if (!DoCustomOverloadOp(opCode, lhs, rhs, Value.Null()))
                        ThrowRuntimeException($"Cannot perform math op '{opCode}' on user types '{lhs.val.asInstance.FromUserType}' and '{rhs.val.asInstance.FromUserType}'");

                }
                break;

                case OpCode.EQUAL:
                {
                    var rhs = Pop();
                    var lhs = Pop();

                    if (lhs.type == ValueType.Instance
                        && DoCustomOverloadOp(opCode, lhs, rhs, Value.Null()))
                        break;

                    Push(Value.New(Value.Compare(ref lhs, ref rhs)));
                }
                break;

                case OpCode.LESS:
                {
                    var rhs = Pop();
                    var lhs = Pop();

                    if (lhs.type == ValueType.Instance
                        && DoCustomOverloadOp(opCode, lhs, rhs, Value.Null()))
                        break;

                    if (lhs.type != ValueType.Double || rhs.type != ValueType.Double)
                        ThrowRuntimeException($"Cannot '{opCode}' compare on different types '{lhs.type}' and '{rhs.type}'");

                    Push(Value.New(lhs.val.asDouble < rhs.val.asDouble));

                }
                break;
                case OpCode.GREATER:
                {
                    var rhs = Pop();
                    var lhs = Pop();

                    if (lhs.type == ValueType.Instance
                        && DoCustomOverloadOp(opCode, lhs, rhs, Value.Null()))
                        break;

                    if (lhs.type != ValueType.Double || rhs.type != ValueType.Double)
                        ThrowRuntimeException($"Cannot '{opCode}' compare on different types '{lhs.type}' and '{rhs.type}'");

                    Push(Value.New(lhs.val.asDouble > rhs.val.asDouble));
                }
                break;

                case OpCode.NOT:
                    Push(Value.New(Pop().IsFalsey()));
                    break;

                case OpCode.PUSH_BOOL:
                    Push(Value.New(packet.BoolValue));
                    break;

                case OpCode.NULL:
                    Push(Value.Null());
                    break;

                case OpCode.PUSH_BYTE:
                    Push(Value.New(packet.b1));
                    break;

                case OpCode.POP:
                    DiscardPop(packet.b1);
                    break;

                case OpCode.SWAP:
                    DoSwapOp();
                    break;

                case OpCode.DUPLICATE:
                    DoDuplicateOp();
                    break;

                case OpCode.JUMP_IF_FALSE:
                    if (Peek().IsFalsey())
                        _currentCallFrame.InstructionPointer += packet.u1;
                    break;

                case OpCode.JUMP:
                    _currentCallFrame.InstructionPointer += packet.u1;
                    break;

                case OpCode.LOOP:
                    _currentCallFrame.InstructionPointer -= packet.u1;
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
                    _globals[globalName.val.asString] = Pop();
                }
                break;

                case OpCode.FETCH_GLOBAL:
                {
                    var globalName = chunk.ReadConstant(packet.b1);
                    var actualName = globalName.val.asString;

                    if (_globals.TryGetValue(actualName, out var found))
                    {
                        Push(found);
                    }
                    else
                    {
                        ThrowRuntimeException($"No global of name {actualName} could be found");
                    }
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
                        GetLocationNameFromFrame(frame, currentInstruction),
                        GenerateValueStackDump(),
                        GenerateCallStackDump());
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
                    DoGetPropertyOp(chunk, packet.b1);
                    break;

                case OpCode.SET_PROPERTY:
                    DoSetPropertyOp(chunk, packet.b1);
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

                case OpCode.REGISTER:
                    DoRegisterOp(chunk, packet.b1);
                    break;

                case OpCode.INJECT:
                    DoInjectOp(chunk, packet.b1);
                    break;

                case OpCode.FREEZE:
                    DoFreezeOp();
                    break;

                case OpCode.NATIVE_TYPE:
                    DoNativeTypeOp(chunk, packet.NativeType);
                    break;

                case OpCode.GET_INDEX:
                    DoGetIndexOp(opCode);
                    break;

                case OpCode.SET_INDEX:
                    DoSetIndexOp(opCode);
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
                    DoCountOfOp();
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
                    DoEnumValueOp(chunk);
                    break;

                case OpCode.READ_ONLY:
                    DoReadOnlyOp(chunk);
                    break;

                case OpCode.NONE:
                default:
                    ThrowRuntimeException($"Unhandled OpCode '{opCode}'.");
                    break;
                }
            }
        }

        public void ThrowRuntimeException(string msg)
        {
            var frame = _currentCallFrame;
            var currentInstruction = frame.InstructionPointer;

            throw new RuntimeUloxException(msg,
                currentInstruction,
                GetLocationNameFromFrame(frame, currentInstruction),
                GenerateValueStackDump(),
                GenerateCallStackDump());
        }

        private static string DumpStack(FastStack<Value> valueStack)
        {
            var stackVars = valueStack
                .Select(x => x.ToString())
                .Take(valueStack.Count)
                .Reverse();

            return string.Join(System.Environment.NewLine, stackVars);
        }

        private static string GetLocationNameFromFrame(CallFrame frame, int currentInstruction = -1)
        {
            if (frame.nativeFunc != null)
            {
                var name = frame.nativeFunc.Method.Name;
                if (frame.nativeFunc.Target != null)
                    name = frame.nativeFunc.Target.GetType().Name + "." + frame.nativeFunc.Method.Name;
                return $"native:'{name}'";
            }

            var line = -1;
            if (currentInstruction != -1)
                line = frame.Closure?.chunk?.GetLineForInstruction(currentInstruction) ?? -1;

            var locationName = frame.Closure?.chunk.GetLocationString(line);
            return $"chunk:'{locationName}'";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoMeetsOp()
        {
            var (meets, _) = ProcessContract();
            Push(Value.New(meets));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoSignsOp()
        {
            var (meets, msg) = ProcessContract();
            if (!meets)
                ThrowRuntimeException($"Sign failure with msg '{msg}'");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoCountOfOp()
        {
            var target = Pop();
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
            var msg = Pop();
            var expected = Pop();

            if (expected.IsFalsey())
            {
                ThrowRuntimeException($"Expect failed, got {(msg.IsNull() ? "falsey" : msg.ToString())}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoEnumValueOp(Chunk chunk)
        {
            var enumObject = Pop();
            var val = Pop();
            var key = Pop();
            (enumObject.val.asClass as EnumClass).AddEnumValue(key, val);
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
        private (bool meets, string msg) ProcessContract()
        {
            var rhs = Pop();
            var lhs = Pop();

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
        private void DoSetIndexOp(OpCode opCode)
        {
            var newValue = Pop();
            var index = Pop();
            var listValue = Pop();
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
        private void DoGetIndexOp(OpCode opCode)
        {
            var index = Pop();
            var listValue = Pop();

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
        private void DoNativeCall(OpCode opCode)
        {
            if (_currentCallFrame.nativeFunc == null)
                ThrowRuntimeException($"{opCode} without nativeFunc encountered. This is not allowed");

            var argCount = CurrentFrameStackValues;
            var res = _currentCallFrame.nativeFunc.Invoke(this, argCount);

            if (res == NativeCallResult.SuccessfulExpression)
            {
                if (_currentCallFrame.ReturnStart != 0)
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
        private void DoDuplicateOp()
        {
            var v = Pop();

            Push(v);
            Push(v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoAssignGlobalOp(Chunk chunk, byte globalId)
        {
            var globalName = chunk.ReadConstant(globalId);
            var actualName = globalName.val.asString;
            if (!_globals.ContainsKey(actualName))
            {
                ThrowRuntimeException($"Global var of name '{actualName}' was not found");
            }
            _globals[actualName] = Peek();
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
        private bool DoReturnOp(Chunk chunk, ReturnMode returnMode)
        {
            var origCallFrameCount = _callFrames.Count;
            var wantsToYieldOnReturn = _currentCallFrame.YieldOnReturn;

            switch (returnMode)
            {
            case ReturnMode.One:
                var top = Pop();
                ReturnOneValue(top);
                break;

            case ReturnMode.Begin:
                _currentCallFrame.ReturnStart = (byte)StackCount;
                break;

            case ReturnMode.End:
                ReturnFromMark();
                break;

            case ReturnMode.MarkMultiReturnAssignStart:
                _currentCallFrame.ReturnStart = (byte)StackCount;
                break;

            case ReturnMode.MarkMultiReturnAssignEnd:
                ProcessStackForMultiAssign();
                break;

            default:
                ThrowRuntimeException($"Unhandled return mode '{returnMode}'");
                break;
            }

            return _callFrames.Count == 0
                || (_callFrames.Count < origCallFrameCount && wantsToYieldOnReturn);
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
            CloseUpvalues(_currentCallFrame.StackStart);

            PopFrameAndDiscard();
            TransferReturnToStack();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReturnFromMark()
        {
            var returnStart = _currentCallFrame.ReturnStart;
            _returnStack.Reset();
            for (int i = returnStart; i < StackCount; i++)
            {
                _returnStack.Push(_valueStack[i]);
            }

            DiscardPop(StackCount - returnStart);

            FinishReturnOp();
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

        //todo the returning function can tell us how many we are about to receive we don't need to track it separately?
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessStackForMultiAssign()
        {
            _returnStack.Reset();
            while (_valueStack.Count > _currentCallFrame.ReturnStart)
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
        public void PushCallFrameFromValue(Value callee, int argCount)
        {
            switch (callee.type)
            {
            case ValueType.NativeFunction:
                PushFrameCallNative(callee.val.asNativeFunc, argCount);
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
                ThrowRuntimeException($"Wrong number of params given to '{closureInternal.chunk.Name}'" +
                    $", got '{argCount}' but expected '{closureInternal.chunk.Arity}'");

            if (closureInternal.chunk.FunctionType == FunctionType.PureFunction)
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
                ThrowRuntimeException($"Pure call '{closureInternal.chunk.Name}' with non-pure confirming argument '{value}'");
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
        private void DoInjectOp(Chunk chunk, byte constantIndex)
        {
            var name = chunk.ReadConstant(constantIndex).val.asString;
            if (DiContainer.TryGetValue(name, out var found))
                Push(found);
            else
                ThrowRuntimeException($"Inject failure. Nothing has been registered (yet) with name '{name}'");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoRegisterOp(Chunk chunk, byte constantIndex)
        {
            var name = chunk.ReadConstant(constantIndex).val.asString;
            var implementation = Pop();
            DiContainer.Set(name, implementation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoGetPropertyOp(Chunk chunk, byte constantIndex)
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

            if (instance.TryGetField(name, out var val))
            {
                DiscardPop();
                Push(val);
                return;
            }

            BindMethod(instance.FromUserType, name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoSetPropertyOp(Chunk chunk, byte constantIndex)
        {
            var targetVal = Peek(1);

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

            instance.SetField(name, Peek());

            var value = Pop();
            DiscardPop();
            Push(value);
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
                if (inst.TryGetField(methodName, out var fieldFunc))
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

                    if (!fromClass.TryGetMethod(methodName, out var method))
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
                PushCallFrameFromValue(klass.GetMethod(methodName), argCount);
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
            var klass = Pop();
            var mixin = Pop();
            klass.val.asClass.AddMixin(mixin, this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BindMethod(UserTypeInternal fromClass, HashedString methodName)
        {
            if (fromClass == null)
                ThrowRuntimeException($"Cannot bind method '{methodName}', there is no fromClass");

            if (!fromClass.TryGetMethod(methodName, out var method))
                ThrowRuntimeException($"Undefined property '{methodName}'");

            var receiver = Peek();
            var meth = method.val.asClosure;
            var bound = Value.New(new BoundMethod(receiver, meth));

            DiscardPop();
            Push(bound);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallMethod(BoundMethod asBoundMethod, int argCount)
        {
            _valueStack[_valueStack.Count - 1 - argCount] = asBoundMethod.Receiver;
            Call(asBoundMethod.Method, argCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateInstance(UserTypeInternal asClass, int argCount)
        {
            var instInternal = asClass.MakeInstance();
            var inst = Value.New(instInternal);
            _valueStack[_valueStack.Count - 1 - argCount] = inst;

            InitNewInstance(asClass, argCount, inst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitNewInstance(UserTypeInternal klass, int argCount, Value inst)
        {
            var stackCount = _valueStack.Count;
            var instLocOnStack = (byte)(stackCount - argCount - 1);

            DuplicateStackValuesNew(instLocOnStack, argCount);
            PushFrameCallNativeWithFixedStackStart(ClassFinishCreation, instLocOnStack);

            if (!klass.Initialiser.IsNull())
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
                ThrowRuntimeException($"Args given for a class that does not have an 'init' method");
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
        private NativeCallResult CopyMatchingParamsToFields(Vm vm, int argCount)
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
            inst.FromUserType.FinishCreation(inst);
            vm.PushReturn(instVal);
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
                if (self.val.asInstance.TryGetField(targetName, out var matchingValue))
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
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void MoveInstructionPointerTo(ushort loc)
        {
            _currentCallFrame.InstructionPointer = loc;
        }
    }
}
