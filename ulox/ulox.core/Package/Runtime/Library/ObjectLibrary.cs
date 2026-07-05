using System.Linq;

namespace ULox
{
    internal static class ObjectLibrary
    {
        static internal InstanceInternal MakeInstance()
        {
            var assertInst = new InstanceInternal();
            assertInst.AddFieldsToInstance(
                (nameof(Duplicate), Value.New(Duplicate, 1, 1)),
                (nameof(IsFrozen), Value.New(IsFrozen, 1, 1)),
                (nameof(Unfreeze), Value.New(Unfreeze, 1, 1)),
                (nameof(Freeze), Value.New(Freeze, 1, 1)),
                (nameof(TraverseUpdate), Value.New(TraverseUpdate, 1, 3))
                );
            assertInst.Freeze();
            return assertInst;
        }

        public static NativeCallResult Duplicate(Vm vm)
        {
            vm.SetNativeReturn(0, Value.Copy(vm.GetArg(1)));
            return NativeCallResult.SuccessfulExpression;
        }

        public static NativeCallResult IsFrozen(Vm vm)
        {
            var target = vm.GetArg(1);
            if (target.type == ValueType.Instance)
                vm.SetNativeReturn(0, Value.New(target.val.asInstance.IsFrozen));
            else if (target.type == ValueType.UserType)
                vm.SetNativeReturn(0, Value.New(target.val.asClass.IsFrozen));

            return NativeCallResult.SuccessfulExpression;
        }

        public static NativeCallResult Unfreeze(Vm vm)
        {
            var target = vm.GetArg(1);
            if (target.type == ValueType.Instance)
                target.val.asInstance.Unfreeze();
            if (target.type == ValueType.UserType)
                target.val.asClass.Unfreeze();

            return NativeCallResult.SuccessfulExpression;
        }

        public static NativeCallResult Freeze(Vm vm)
        {
            var instVal = vm.GetArg(1);
            switch (instVal.type)
            {
            case ValueType.Instance:
                instVal.val.asInstance.Freeze();
                break;

            case ValueType.UserType:
                instVal.val.asClass.Freeze();
                break;

            default:
                vm.ThrowRuntimeException($"Freeze attempted on unsupported type '{instVal.type}'");
                break;
            }

            return NativeCallResult.SuccessfulExpression;
        }

        public static NativeCallResult TraverseUpdate(Vm vm)
        {
            var instVal = vm.GetArg(1);
            var instVal2 = vm.GetArg(2);
            var updateMeth = vm.GetArg(3);
            if (updateMeth.type != ValueType.Closure
                && updateMeth.type != ValueType.NativeFunction)
            {
                vm.ThrowRuntimeException($"TraverseUpdate expected closure or native function, but got '{updateMeth.type}'");
            }
            //temp
            var ret = UpdateFrom(instVal, instVal2, updateMeth, vm);
            vm.SetNativeReturn(0, ret);
            return NativeCallResult.SuccessfulExpression;
        }

        internal static Value UpdateFrom(Value lhs, Value rhs, Value func, Vm vm)
        {
            if (!lhs.IsNull() && rhs.type != lhs.type && rhs.type != ValueType.Null)
                return lhs;

            if (rhs.IsNull())
                return rhs;

            switch (lhs.type)
            {
            case ValueType.BoundMethod:
            case ValueType.UserType:
            case ValueType.Upvalue:
            case ValueType.Closure:
            case ValueType.NativeFunction:
            case ValueType.Chunk:
            case ValueType.Null:
            case ValueType.Object:
            case ValueType.Double:
            case ValueType.Bool:
            case ValueType.String:
                lhs = ProcessUpdate(lhs, rhs, func, vm);
                break;
            case ValueType.Instance:
                if (lhs.val.asInstance is INativeCollection lhsNativeCol
                    && rhs.val.asInstance is INativeCollection rhsNativeCol
                    && lhsNativeCol.GetType() == rhsNativeCol.GetType())
                {
                    //todo this makes sense for a list but not for a map
                    lhs = ProcessUpdate(lhs, rhs, func, vm);
                }
                else
                {
                    //deal with regular field updates
                    var lhsInst = lhs.val.asInstance;
                    //todo this is now slow and bad
                    foreach (var item in lhsInst.Fields.ToArray())
                    {
                        if (rhs.val.asInstance.Fields.Get(item.Key, out var rhsField))
                        {
                            lhsInst.Fields.Get(item.Key, out var lhsVal);
                            lhsInst.Fields.Set(item.Key, ProcessUpdate(lhsVal, rhsField, func, vm));
                        }
                    }
                }
                break;
            default:
                vm.ThrowRuntimeException($"Unhandled value type '{lhs.type}' in update, with lhs '{lhs}' and rhs '{rhs}'");
                break;
            }

            return lhs;
        }

        private static Value ProcessUpdate(Value lhs, Value rhs, Value func, Vm vm)
        {
            vm.Push(lhs);
            vm.Push(rhs);
            vm.PushCallFrameRunYield(func, 2);
            lhs = vm.Peek();
            return lhs;
        }
    }
}