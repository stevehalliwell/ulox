using System.Runtime.CompilerServices;

namespace ULox
{
    public class VM : VMBase
    {
        public TestRunner TestRunner { get; private set; } = new TestRunner();

        public override void CopyFrom(VMBase otherVMbase)
        {
            base.CopyFrom(otherVMbase);

            if (otherVMbase is VM otherVm)
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
                        klass.initChainStartLocation = initChain;
                        klass.initChainStartClosure = currentCallFrame.Closure;
                    }
                }
                break;
            case OpCode.METHOD:
                {
                    DoMethodOp(chunk);
                }
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


            //TODO: Need to handle duplicate named tests being found
            case OpCode.TEST:
                var testOpType = (TestOpType)ReadByte(chunk);
                switch (testOpType)
                {
                case TestOpType.CaseStart:
                    TestRunner.StartTest(chunk.ReadConstant(ReadByte(chunk)).val.asString);
                    ReadByte(chunk);//byte we don't use
                    break;
                case TestOpType.CaseEnd:
                    TestRunner.EndTest(chunk.ReadConstant(ReadByte(chunk)).val.asString);
                    ReadByte(chunk);//byte we don't use
                    break;
                case TestOpType.TestSetStart:
                    DoTestSet(chunk);
                    break;
                case TestOpType.TestSetEnd:
                    TestRunner.CurrentTestSetName = string.Empty;
                    ReadByte(chunk);//byte we don't use
                    ReadByte(chunk);//byte we don't use
                    break;
                default:
                    return false;
                }
                break;
            default:
                return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoTestSet(Chunk chunk)
        {
            var name = chunk.ReadConstant(ReadByte(chunk)).val.asString;
            var testcaseCount = ReadByte(chunk);

            TestRunner.CurrentTestSetName = name;

            for (int i = 0; i < testcaseCount; i++)
            {
                var loc = ReadUShort(chunk);
                if (TestRunner.Enabled)
                {
                    try
                    {
                        var childVM = new VM();
                        childVM.CopyFrom(this);
                        childVM.Interpret(chunk, loc);
                    }
                    catch (PanicException)
                    {
                        //eat it, results in incomplete test
                    }
                }
            }
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
                        CallValue(fieldFunc, argCount);
                    }
                    else
                    {
                        var fromClass = inst.fromClass;
                        if (!fromClass.TryGetMethod(methodName, out var method))
                        {
                            throw new VMException($"No method of name '{methodName}' found on '{fromClass}'.");
                        }

                        CallValue(method, argCount);
                    }
                }
                break;
            case ValueType.Class:
                {
                    var klass = receiver.val.asClass;
                    CallValue(klass.GetMethod(methodName), argCount);
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

            CallValue(method, argCount);
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

            if (!asClass.initialiser.IsNull)
            {
                //with an init list we don't return this
                CallValue(asClass.initialiser, argCount);
            }
            else if (argCount != 0)
            {
                throw new VMException("Args given for a class that does not have an 'init' method");
            }

            if (asClass.initChainStartLocation != -1)
            {
                if (!asClass.initialiser.IsNull)
                    Push(inst);

                PushNewCallframe(new CallFrame()
                {
                    Closure = asClass.initChainStartClosure,
                    InstructionPointer = asClass.initChainStartLocation,
                    StackStart = _valueStack.Count - 1, //last thing checked
                });
            }
        }

        protected override bool DoCustomMathOp(OpCode opCode, Value lhs, Value rhs)
        {
            if (lhs.type == ValueType.Instance)
            {
                var lhsInst = lhs.val.asInstance;
                int opIndex = (int)opCode - ClassInternal.FirstMathOp;
                var opClosure = lhsInst.fromClass.operators[opIndex];
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

        private bool HandleDynamicCustomMathOp(OpCode opCode, Value lhs, Value rhs)
        {
            var targetName = ClassInternal.OperatorMethodNames[(int)opCode - ClassInternal.FirstMathOp];
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
