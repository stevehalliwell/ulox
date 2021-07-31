using System.Runtime.CompilerServices;

namespace ULox
{
    public class Vm : VMBase
    {
        public TestRunner TestRunner { get; protected set; } = new TestRunner(() => new Vm());
        public DiContainer DiContainer { get; private set; } = new DiContainer();
        private DiContainer _diContainerToRestore;

        public override void CopyFrom(VMBase otherVMbase)
        {
            base.CopyFrom(otherVMbase);

            if (otherVMbase is Vm otherVm)
            {
                TestRunner = otherVm.TestRunner;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool ExtendedCall(Value callee, int argCount)
        {
            switch (callee.type)
            {
            case ValueType.Class:
                CreateInstance(callee.val.asClass, argCount);
                break;

            case ValueType.BoundMethod:
                CallMethod(callee.val.asBoundMethod, argCount);
                break;

            case ValueType.CombinedClosures:
                {
                    var combinedClosures = callee.val.asCombined;
                    for (int i = 0; i < combinedClosures.Count; i++)
                    {
                        DuplicateStackValues(argCount + 1);

                        var closure = combinedClosures[i];
                        Call(closure, argCount);
                    }
                }
                break;

            default:
                return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool ExtendedOp(OpCode opCode, Chunk chunk)
        {
            switch (opCode)
            {
            case OpCode.GET_PROPERTY:
                {
                    DoGetPropertyOp(chunk);
                }
                break;

            case OpCode.SET_PROPERTY:
                {
                    DoSetPropertyOp(chunk);
                }
                break;

            case OpCode.CLASS:
                {
                    var constantIndex = ReadByte(chunk);
                    var name = chunk.ReadConstant(constantIndex);
                    var klassValue = Value.New(new ClassInternal() { name = name.val.asString });
                    Push(klassValue);
                    var klass = klassValue.val.asClass;
                    var initChain = ReadUShort(chunk);
                    if (initChain != 0)
                    {
                        klass.AddInitChain(currentCallFrame.Closure, initChain);
                    }
                }
                break;

            case OpCode.METHOD:
                DoMethodOp(chunk);
                break;

            case OpCode.MIXIN:
                DoMixinOp(chunk);
                break;

            case OpCode.INVOKE:
                {
                    DoInvokeOp(chunk);
                }
                break;

            case OpCode.INHERIT:
                {
                    DoInheritOp(chunk);
                }
                break;

            case OpCode.GET_SUPER:
                {
                    var constantIndex = ReadByte(chunk);
                    var name = chunk.ReadConstant(constantIndex).val.asString;
                    var superClassVal = Pop();
                    var superClass = superClassVal.val.asClass;
                    BindMethod(superClass, name);
                }
                break;

            case OpCode.SUPER_INVOKE:
                {
                    var constantIndex = ReadByte(chunk);
                    var methName = chunk.ReadConstant(constantIndex).val.asString;
                    var argCount = ReadByte(chunk);
                    var superClass = Pop().val.asClass;
                    InvokeFromClass(superClass, methName, argCount);
                }
                break;

            case OpCode.TEST:
                TestRunner.DoTestOpCode(this, chunk);
                break;


            case OpCode.REGISTER:
                {
                    var constantIndex = ReadByte(chunk);
                    var name = chunk.ReadConstant(constantIndex).val.asString;
                    var implementation = Pop();
                    DiContainer[name] = implementation;
                }
                break;

            case OpCode.INJECT:
                {
                    var constantIndex = ReadByte(chunk);
                    var name = chunk.ReadConstant(constantIndex).val.asString;
                    if (DiContainer.TryGetValue(name, out var found))
                        Push(found);
                    else
                        throw new VMException($"Inject failure. Nothing has been registered (yet) with name '{name}'.");
                }
                break;
            default:
                return false;
            }
            return true;
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

            if (instance.fields.TryGetValue(name, out var val))
            {
                DiscardPop();
                Push(val);
                return;
            }

            BindMethod(instance.fromClass, name);
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

            instance.fields[name] = Peek();

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
                    if (inst.fields.TryGetValue(methodName, out var fieldFunc))
                    {
                        _valueStack[_valueStack.Count - 1 - argCount] = fieldFunc;
                        PushCallFrameFromValue(fieldFunc, argCount);
                    }
                    else
                    {
                        var fromClass = inst.fromClass;
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
            var flavour = mixin.val.asClass;
            klass.val.asClass.AddMixin(flavour);
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
        private void BindMethod(ClassInternal fromClass, string methodName)
        {
            if (!fromClass.TryGetMethod(methodName, out var method))
            {
                throw new VMException($"Undefined property {methodName}");
            }

            var bound = Value.New(new BoundMethod() { receiver = Peek(), method = method.val.asClosure });

            DiscardPop();
            Push(bound);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvokeFromClass(ClassInternal fromClass, string methodName, int argCount)
        {
            if (!fromClass.TryGetMethod(methodName, out var method))
            {
                throw new VMException($"No method of name '{methodName}' found on '{fromClass}'.");
            }

            PushCallFrameFromValue(method, argCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallMethod(BoundMethod asBoundMethod, int argCount)
        {
            _valueStack[_valueStack.Count - 1 - argCount] = asBoundMethod.receiver;
            Call(asBoundMethod.method, argCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateInstance(ClassInternal asClass, int argCount)
        {
            var instInternal = new InstanceInternal() { fromClass = asClass };
            var inst = Value.New(instInternal);
            _valueStack[_valueStack.Count - 1 - argCount] = inst;

            InitNewInstance(asClass, argCount, inst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitNewInstance(ClassInternal klass, int argCount, Value inst)
        {
            var stackCount = _valueStack.Count;
            if (!klass.initialiser.IsNull)
            {
                //with an init list we don't return this
                PushCallFrameFromValue(klass.initialiser, argCount);

                //push a native call here so we can bind the fiels to init param names
                if (klass.initialiser.type == ValueType.Closure &&
                    klass.initialiser.val.asClosure.chunk.Arity > 0)
                {
                    DuplicateStackValues(argCount + 1);
                    PushFrameCallNative(CopyMatchingParamsToFields, argCount);
                }
            }
            else if (argCount != 0)
            {
                throw new VMException("Args given for a class that does not have an 'init' method");
            }


            foreach (var initChain in klass.initChains)
            {
                if(initChain.Item2 != -1) 
                { 
                    if (!klass.initialiser.IsNull)
                        Push(inst);

                    PushNewCallframe(new CallFrame()
                    {
                        Closure = initChain.Item1,
                        InstructionPointer = initChain.Item2,
                        StackStart = _valueStack.Count - 1, //last thing checked
                    });
                }
            }

            if (klass.super != null)
            {
                var argsToSuperInit = PrepareSuperInit(klass, argCount, inst, stackCount);

                InitNewInstance(klass.super, argsToSuperInit, inst);
            }
        }

        private Value CopyMatchingParamsToFields(VMBase vm, int argCount)
        {
            var instVal = vm.GetArg(0);
            var inst = instVal.val.asInstance;

            var initChunk = inst.fromClass.initialiser.val.asClosure.chunk;
            var argConstantIds = initChunk.ArgumentConstantIds;

            const int argOffset = 1;

            for (int i = 0; i < argConstantIds.Count; i++)
            {
                var arg = initChunk.Constants[i];
                if (arg.type == ValueType.String)
                {
                    var paramName = arg.val.asString;
                    if (inst.fields.ContainsKey(paramName))
                    {
                        var value = vm.GetArg(i+ argOffset);
                        inst.fields[paramName] = value;
                    }
                }
            }

            return instVal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PrepareSuperInit(ClassInternal klass, int argCount, Value inst, int stackCount)
        {
            int argsToSuperInit = 0;
            if (!klass.super.initialiser.IsNull || klass.super.initChains.Count > 0)
            {
                //push inst and push args it expects
                Push(inst);
                if (!klass.super.initialiser.IsNull)
                {
                    argsToSuperInit = klass.super.initialiser.val.asClosure.chunk.Arity;
                    for (int i = 0; i < klass.super.initialiser.val.asClosure.chunk.Arity; i++)
                    {
                        Push(_valueStack[stackCount - argCount + i]);
                    }
                }
            }

            return argsToSuperInit;
        }

        protected override bool DoCustomMathOp(OpCode opCode, Value lhs, Value rhs)
        {
            if (lhs.type == ValueType.Instance)
            {
                var lhsInst = lhs.val.asInstance;
                int opIndex = (int)opCode - ClassInternal.FirstMathOp;
                var opClosure = lhsInst.fromClass.mathOperators[opIndex];
                //identify if lhs has a matching method or field
                if (!opClosure.IsNull)
                {
                    CallOperatorOverloadedbyFunction(lhs, rhs, opClosure);
                    return true;
                }

                if (lhsInst.fromClass.name == DynamicClass.Name)
                {
                    return HandleDynamicCustomMathOp(opCode, lhs, rhs);
                }
            }
            return false;
        }

        protected override bool DoCustomComparisonOp(OpCode opCode, Value lhs, Value rhs)
        {
            if (lhs.type == ValueType.Instance)
            {
                var lhsInst = lhs.val.asInstance;
                int opIndex = (int)opCode - ClassInternal.FirstCompOp;
                var opClosure = lhsInst.fromClass.compOperators[opIndex];
                //identify if lhs has a matching method or field
                if (!opClosure.IsNull)
                {
                    CallOperatorOverloadedbyFunction(lhs, rhs, opClosure);
                    return true;
                }

                if (lhsInst.fromClass.name == DynamicClass.Name)
                {
                    return HandleDynamicCustomCompOp(opCode, lhs, rhs);
                }
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
                StackStart = _valueStack.Count - 3,
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HandleDynamicCustomMathOp(OpCode opCode, Value lhs, Value rhs)
        {
            var targetName = ClassInternal.MathOperatorMethodNames[(int)opCode - ClassInternal.FirstMathOp];
            if (lhs.val.asInstance.fields.TryGetValue(targetName, out var matchingValue))
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
            if (lhs.val.asInstance.fields.TryGetValue(targetName, out var matchingValue))
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
