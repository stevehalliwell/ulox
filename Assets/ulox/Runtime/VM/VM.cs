using System.Runtime.CompilerServices;

namespace ULox
{
    public class VM : VMBase
    {
        public TestRunner TestRunner { get; private set; } = new TestRunner();

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
            switch(opCode)
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
                    Push(Value.New(new ClassInternal() { name = name.val.asString }));
                }
                break;
            case OpCode.METHOD:
                {
                    var constantIndex = ReadByte(chunk);
                    var name = chunk.ReadConstant(constantIndex).val.asString;
                    DefineMethod(name);
                }
                break;
            case OpCode.INVOKE:
                {
                    DoInvokeOp(chunk);
                }
                break;
            case OpCode.INHERIT:
                {
                    DoInheritOp();
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
            case OpCode.INIT_CHAIN_START:
                {
                    var loc = ReadUShort(chunk);
                    var klass = Peek().val.asClass;
                    klass.initChainStartLocation = loc;
                    klass.initChainStartClosure = currentCallFrame.Closure;
                }
                break;


                //TODO: Need to handle duplicate named tests being found
            case OpCode.TEST_START:
                    TestRunner.StartTest(chunk.ReadConstant(ReadByte(chunk)).val.asString);
                break;
            case OpCode.TEST_END:
                    TestRunner.EndTest(chunk.ReadConstant(ReadByte(chunk)).val.asString);
                break;
            case OpCode.TEST_CHAIN_START:
                    DoTestChainOp(chunk);
                break;
            default:
                return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoTestChainOp(Chunk chunk)
        {
            var loc = ReadUShort(chunk);
            if (TestRunner.Enabled)
            {
                //chain ends in a return, running as a frame makes the inner locals match
                PushNewCallframe(new CallFrame()
                {
                    Closure = currentCallFrame.Closure,
                    InstructionPointer = loc,
                    StackStart = _valueStack.Count - 1,
                });
                currentCallFrame.InstructionPointer = loc;
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
                        if (!fromClass.methods.TryGetValue(methodName, out var method))
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
                    CallValue(klass.methods[methodName], argCount);
                }
                break;
            default:
                throw new VMException($"Cannot invoke on '{receiver}'.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DefineMethod(string name)
        {
            Value method = Peek();
            var klass = Peek(1).val.asClass;
            klass.methods[name] = method;
            if (name == ClassCompilette.InitMethodName)
            {
                klass.initialiser = method;
            }
            DiscardPop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoInheritOp()
        {
            var superClass = Peek(1);
            if (superClass.type != ValueType.Class)
                throw new VMException("Superclass must be a class.");

            var subClass = Peek();
            var subMethods = subClass.val.asClass.methods;
            var superMethods = superClass.val.asClass.methods;
            foreach (var item in superMethods)
            {
                var k = item.Key;
                var v = item.Value;
                subMethods.Add(k, v);
            }

            DiscardPop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BindMethod(ClassInternal fromClass, string methodName)
        {
            if (!fromClass.methods.TryGetValue(methodName, out var method))
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
            if (!fromClass.methods.TryGetValue(methodName, out var method))
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
    }
}
