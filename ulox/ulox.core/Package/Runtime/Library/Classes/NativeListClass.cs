using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections;
using System;

namespace ULox
{
    public sealed class NativeListClass : UserTypeInternal
    {
        public static readonly Value SharedNativeListClassValue = Value.New(new NativeListClass());

        public override InstanceInternal MakeInstance() => CreateInstance();

        public static NativeListInstance CreateInstance()
            => new(SharedNativeListClassValue.val.asClass);

        public NativeListClass()
            : base(new HashedString("NativeList"), UserType.Native)
        {
            this.AddMethodsToClass(
                (nameof(Count), Value.New(Count, 1, 0)),
                (nameof(Resize), Value.New(Resize, 1, 2)),
                (nameof(Add), Value.New(Add, 1, 1)),
                (nameof(Remove), Value.New(Remove, 1, 1)),
                (nameof(RemoveAt), Value.New(RemoveAt, 1, 1)),
                (nameof(Reverse), Value.New(Reverse, 1, 0)),
                (nameof(Map), Value.New(Map, 1, 1)),
                (nameof(Reduce), Value.New(Reduce, 1, 1)),
                (nameof(Fold), Value.New(Fold, 1, 2)),
                (nameof(Filter), Value.New(Filter, 1, 1)),
                (nameof(OrderBy), Value.New(OrderBy, 1, 1)),
                (nameof(First), Value.New(First, 1, 1)),
                (nameof(Fork), Value.New(Fork, 1, 1)),
                (nameof(Until), Value.New(Until, 1, 1)),

                (nameof(Grow), Value.New(Grow, 1, 2)),
                (nameof(Shrink), Value.New(Shrink, 1, 1)),
                (nameof(Clear), Value.New(Clear, 1, 0)),

                (nameof(Front), Value.New(Front, 1, 0)),
                (nameof(Back), Value.New(Back, 1, 0)),

                (nameof(Shuffle), Value.New(Shuffle, 1, 0)),
                (nameof(Contains), Value.New(Contains, 1, 1))
                                  );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static FastList<Value> GetArg0NativeListInstance(Vm vm)
        {
            var inst = vm.GetArg(0);
            var nativeListinst = inst.val.asInstance as NativeListInstance;
            return nativeListinst.List;
        }

        private NativeCallResult Count(Vm vm)
        {
            vm.SetNativeReturn(0, Value.New(GetArg0NativeListInstance(vm).Count));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Add(Vm vm)
        {
            ThrowIfReadOnly(vm);
            var top = vm.GetArg(1);
            var inst = vm.GetArg(0);
            var nativeListinst = inst.val.asInstance as NativeListInstance;
            nativeListinst.List.Add(top);
            vm.SetNativeReturn(0, inst);
            return NativeCallResult.SuccessfulExpression;
        }

        private static void ThrowIfReadOnly(Vm vm)
        {
            var inst = vm.GetArg(0);
            var nativeListinst = inst.val.asInstance as NativeListInstance;
            if (nativeListinst.IsReadOnly)
                vm.ThrowRuntimeException($"Attempted to modify a read only list");
        }

        private NativeCallResult Resize(Vm vm)
        {
            ThrowIfReadOnly(vm);
            var count = vm.GetArg(1);
            var fillWith = vm.GetArg(2);
            var list = GetArg0NativeListInstance(vm);

            int size = (int)count.val.asDouble;
            while (list.Count < size)
                list.Add(fillWith);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Remove(Vm vm)
        {
            ThrowIfReadOnly(vm);
            var top = vm.GetArg(1);
            var inst = GetArg0NativeListInstance(vm);
            inst.Remove(top);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult RemoveAt(Vm vm)
        {
            ThrowIfReadOnly(vm);
            var index = vm.GetArg(1);
            var atVal = GetArg0NativeListInstance(vm)[(int)index.val.asDouble];
            GetArg0NativeListInstance(vm).RemoveAt((int)index.val.asDouble);
            vm.SetNativeReturn(0, atVal);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Reverse(Vm vm)
        {
            ThrowIfReadOnly(vm);
            GetArg0NativeListInstance(vm).Reverse();
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Grow(Vm vm)
        {
            ThrowIfReadOnly(vm);
            var size = vm.GetArg(1).val.asDouble;
            var val = vm.GetArg(2);
            var inst = vm.GetArg(0);
            var nativeListinst = inst.val.asInstance as NativeListInstance;
            while (nativeListinst.List.Count < size) nativeListinst.List.Add(Value.Copy(val));
            vm.SetNativeReturn(0, inst);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Shrink(Vm vm)
        {
            ThrowIfReadOnly(vm);
            var size = vm.GetArg(1).val.asDouble;
            var inst = vm.GetArg(0);
            var nativeListinst = inst.val.asInstance as NativeListInstance;
            while (nativeListinst.List.Count > size) nativeListinst.List.RemoveAt(nativeListinst.List.Count - 1);
            vm.SetNativeReturn(0, inst);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Map(Vm vm)
        {
            var fn = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);
            var retval = MakeInstance();
            var retvalList = ((NativeListInstance)retval).List;

            retvalList.EnsureCapacity(list.Count);

            for (int i = 0; i < list.Count; i++)
            {
                vm.Push(fn);
                vm.Push(list[i]);
                vm.PushCallFrameRunYield(fn, 1);
                retvalList.Add(vm.Pop());
            }

            vm.SetNativeReturn(0, Value.New(retval));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Reduce(Vm vm)
        {
            var fn = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);
            var runningVal = list[0];

            for (int i = 1; i < list.Count; i++)
            {
                vm.Push(fn);
                vm.Push(list[i]);
                vm.Push(runningVal);
                vm.PushCallFrameRunYield(fn, 2);
                runningVal = vm.Pop();
            }

            vm.SetNativeReturn(0, runningVal);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Fold(Vm vm)
        {
            var fn = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);
            var runningVal = vm.GetArg(2);

            for (int i = 0; i < list.Count; i++)
            {
                vm.Push(fn);
                vm.Push(list[i]);
                vm.Push(runningVal);
                vm.PushCallFrameRunYield(fn, 2);
                runningVal = vm.Pop();
            }

            vm.SetNativeReturn(0, runningVal);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Filter(Vm vm)
        {
            var fn = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);
            var retval = MakeInstance();
            var retvalList = ((NativeListInstance)retval).List;

            retvalList.EnsureCapacity(list.Count);

            for (int i = 0; i < list.Count; i++)
            {
                var testVal = list[i];
                vm.Push(fn);
                vm.Push(testVal);
                vm.PushCallFrameRunYield(fn, 1);

                var filterRes = vm.Pop();

                if (!filterRes.IsFalsey())
                    retvalList.Add(testVal);
            }

            vm.SetNativeReturn(0, Value.New(retval));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult OrderBy(Vm vm)
        {
            var fn = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);

            var orderByArr = new (Value val, int index)[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                var valAtIndex = list[i];
                vm.Push(fn);
                vm.Push(valAtIndex);
                vm.PushCallFrameRunYield(fn, 1);

                var orderBy = vm.Pop();
                orderByArr[i] = (orderBy, i);
            }

            orderByArr = orderByArr.OrderBy(x => x.val, ValueComparer.Instance).ToArray();

            var retval = MakeInstance();
            var retvalList = ((NativeListInstance)retval).List;
            retvalList.EnsureCapacity(list.Count);

            for (int i = 0; i < orderByArr.Length; i++)
            {
                retvalList.Add(list[orderByArr[i].index]);
            }

            vm.SetNativeReturn(0, Value.New(retval));
            return NativeCallResult.SuccessfulExpression;
        }

        public class ValueComparer : IComparer<Value>
        {
            public readonly static ValueComparer Instance = new();

            public int Compare(Value x, Value y)
            {
                switch (x.type)
                {
                case ValueType.Null:
                    return 0;
                case ValueType.Double:
                    return x.val.asDouble.CompareTo(y.val.asDouble);
                case ValueType.Bool:
                    return x.val.asBool.CompareTo(y.val.asBool);
                case ValueType.String:
                    return x.val.asString.CompareTo(y.val.asString);
                case ValueType.Chunk:
                case ValueType.NativeFunction:
                case ValueType.Closure:
                case ValueType.Upvalue:
                case ValueType.UserType:
                case ValueType.Instance:
                case ValueType.BoundMethod:
                case ValueType.Object:
                    return Comparer.Default.Compare(x.val.asObject, y.val.asObject);
                default:
                    throw new Exception();
                }
            }
        }

        private NativeCallResult First(Vm vm)
        {
            var fn = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);

            for (int i = 0; i < list.Count; i++)
            {
                var testVal = list[i];
                vm.Push(fn);
                vm.Push(testVal);
                vm.PushCallFrameRunYield(fn, 1);

                var filterRes = vm.Pop();

                if (!filterRes.IsFalsey())
                {
                    vm.SetNativeReturn(0, testVal);
                    return NativeCallResult.SuccessfulExpression;
                }
            }

            vm.SetNativeReturn(0, Value.Null());
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Fork(Vm vm)
        {
            var sharedVarToRunOn = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);
            var retval = MakeInstance();
            var retvalList = ((NativeListInstance)retval).List;

            retvalList.EnsureCapacity(list.Count);

            for (int i = 0; i < list.Count; i++)
            {
                var fn = list[i];
                vm.Push(fn);
                vm.Push(sharedVarToRunOn);
                vm.PushCallFrameRunYield(fn, 1);

                var result = vm.Pop();
                retvalList.Add(result);
            }

            vm.SetNativeReturn(0, Value.New(retval));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Until(Vm vm)
        {
            var sharedVarToRunOn = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);

            for (int i = 0; i < list.Count; i++)
            {
                var fn = list[i];
                vm.Push(fn);
                vm.Push(sharedVarToRunOn);
                vm.PushCallFrameRunYield(fn, 1);

                var result = vm.Pop();

                if (!result.IsFalsey())
                {
                    break;
                }
            }

            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Clear(Vm vm)
        {
            var list = GetArg0NativeListInstance(vm);
            vm.SetNativeReturn(0, Value.New(list.Count));
            list.Clear();
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Front(Vm vm)
        {
            var list = GetArg0NativeListInstance(vm);
            vm.SetNativeReturn(0, list.Count > 0 ? list[0] : Value.Null());
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Back(Vm vm)
        {
            var list = GetArg0NativeListInstance(vm);
            vm.SetNativeReturn(0, list.Count > 0 ? list[list.Count - 1] : Value.Null());
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Shuffle(Vm vm)
        {
            var list = GetArg0NativeListInstance(vm);
            var rnd = new Random();
            for (int i = 0; i < list.Count; i++)
            {
                int swapIndex = rnd.Next(list.Count);
                var temp = list[i];
                list[i] = list[swapIndex];
                list[swapIndex] = temp;
            }
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Contains(Vm vm)
        {
            var list = GetArg0NativeListInstance(vm);
            var val = vm.GetArg(1);
            var found = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (ValueComparer.Instance.Compare(list[i], val) == 0)
                {
                    found = true;
                    break;
                }
            }
            vm.SetNativeReturn(0, Value.New(found));
            return NativeCallResult.SuccessfulExpression;
        }
    }
}