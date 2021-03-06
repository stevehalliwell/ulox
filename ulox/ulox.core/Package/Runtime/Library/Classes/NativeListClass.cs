using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

namespace ULox
{
    public class NativeListClass : ClassInternal 
    {
        public static readonly Value SharedNativeListClassValue = Value.New(new NativeListClass());

        public NativeListClass()
            : base(new HashedString("NativeList"))
        {
            this.AddMethodsToClass(
                (nameof(Count), Value.New(Count)),
                (nameof(Resize), Value.New(Resize)),
                (nameof(Add), Value.New(Add)),
                (nameof(Remove), Value.New(Remove)),
                (nameof(Map), Value.New(Map)),
                (nameof(Reduce), Value.New(Reduce)),
                (nameof(Fold), Value.New(Fold)),
                (nameof(Filter), Value.New(Filter)),
                (nameof(OrderBy), Value.New(OrderBy)),
                (nameof(First), Value.New(First)),
                (nameof(Fork), Value.New(Fork)),
                (nameof(Until), Value.New(Until))
                                  );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<Value> GetArg0NativeListInstance(Vm vm)
        {
            var inst = vm.GetArg(0);
            var nativeListinst = inst.val.asInstance as NativeListInstance;
            return nativeListinst.List;
        }

        private NativeCallResult Count(Vm vm, int argCount)
        {
            vm.PushReturn(Value.New(GetArg0NativeListInstance(vm).Count));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Add(Vm vm, int argCount)
        {
            var top = vm.GetArg(1);
            GetArg0NativeListInstance(vm).Add(top);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Resize(Vm vm, int argCount)
        {
            var count = vm.GetArg(1);
            var fillWith = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);

            int size = (int)count.val.asDouble;
            while (list.Count < size)
                list.Add(fillWith);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Remove(Vm vm, int argCount)
        {
            var top = vm.GetArg(1);
            GetArg0NativeListInstance(vm).Remove(top);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Map(Vm vm, int argCount)
        {
            var fn = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);
            var retval = MakeInstance();
            var retvalList = ((NativeListInstance)retval).List;

            retvalList.Capacity = list.Count;

            for (int i = 0; i < list.Count; i++)
            {
                vm.Push(fn);
                vm.Push(list[i]);
                vm.PushCallFrameFromValue(fn, 1);
                vm.SetCurrentCallFrameToYieldOngReturn();
                vm.Run();
                retvalList.Add(vm.Pop());
            }

            vm.PushReturn(Value.New(retval));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Reduce(Vm vm, int argCount)
        {
            var fn = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);
            var runningVal = list[0];

            for (int i = 1; i < list.Count; i++)
            {
                vm.Push(fn);
                vm.Push(list[i]);
                vm.Push(runningVal);
                vm.PushCallFrameFromValue(fn, 2);
                vm.SetCurrentCallFrameToYieldOngReturn();
                vm.Run();
                runningVal = vm.Pop();
            }

            vm.PushReturn(runningVal);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Fold(Vm vm, int argCount)
        {
            var fn = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);
            var runningVal = vm.GetArg(2);

            for (int i = 0; i < list.Count; i++)
            {
                vm.Push(fn);
                vm.Push(list[i]);
                vm.Push(runningVal);
                vm.PushCallFrameFromValue(fn, 2);
                vm.SetCurrentCallFrameToYieldOngReturn();
                vm.Run();
                runningVal = vm.Pop();
            }

            vm.PushReturn(runningVal);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Filter(Vm vm, int argCount)
        {
            var fn = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);
            var retval = MakeInstance();
            var retvalList = ((NativeListInstance)retval).List;

            retvalList.Capacity = list.Count;

            for (int i = 0; i < list.Count; i++)
            {
                var testVal = list[i];
                vm.Push(fn);
                vm.Push(testVal);
                vm.PushCallFrameFromValue(fn, 1);
                vm.SetCurrentCallFrameToYieldOngReturn();
                vm.Run();
                
                var filterRes = vm.Pop();

                if(!filterRes.IsFalsey)
                    retvalList.Add(testVal);
            }

            vm.PushReturn(Value.New(retval));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult OrderBy(Vm vm, int argCount)
        {
            var fn = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);

            var orderByArr = new(Value val, int index)[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                var valAtIndex = list[i];
                vm.Push(fn);
                vm.Push(valAtIndex);
                vm.PushCallFrameFromValue(fn, 1);
                vm.SetCurrentCallFrameToYieldOngReturn();
                vm.Run();

                var orderBy = vm.Pop();
                orderByArr[i] = (orderBy, i);
            }

            orderByArr = orderByArr.OrderBy(x => x.val, ValueComparer.Instance).ToArray();

            var retval = MakeInstance();
            var retvalList = ((NativeListInstance)retval).List;
            retvalList.Capacity = list.Count;

            for (int i = 0; i < orderByArr.Length; i++)
            {
                retvalList.Add(list[orderByArr[i].index]);
            }

            vm.PushReturn(Value.New(retval));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult First(Vm vm, int argCount)
        {
            var fn = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);

            for (int i = 0; i < list.Count; i++)
            {
                var testVal = list[i];
                vm.Push(fn);
                vm.Push(testVal);
                vm.PushCallFrameFromValue(fn, 1);
                vm.SetCurrentCallFrameToYieldOngReturn();
                vm.Run();

                var filterRes = vm.Pop();

                if (!filterRes.IsFalsey)
                {
                    vm.PushReturn(testVal);
                    return NativeCallResult.SuccessfulExpression;
                }
            }

            vm.PushReturn(Value.Null());
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Fork(Vm vm, int argCount)
        {
            var sharedVarToRunOn = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);
            var retval = MakeInstance();
            var retvalList = ((NativeListInstance)retval).List;

            retvalList.Capacity = list.Count;

            for (int i = 0; i < list.Count; i++)
            {
                var fn = list[i];
                vm.Push(fn);
                vm.Push(sharedVarToRunOn);
                vm.PushCallFrameFromValue(fn, 1);
                vm.SetCurrentCallFrameToYieldOngReturn();
                vm.Run();

                var result = vm.Pop();
                retvalList.Add(result);
            }

            vm.PushReturn(Value.New(retval));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Until(Vm vm, int argCount)
        {
            var sharedVarToRunOn = vm.GetArg(1);
            var list = GetArg0NativeListInstance(vm);

            for (int i = 0; i < list.Count; i++)
            {
                var fn = list[i];
                vm.Push(fn);
                vm.Push(sharedVarToRunOn);
                vm.PushCallFrameFromValue(fn, 1);
                vm.SetCurrentCallFrameToYieldOngReturn();
                vm.Run();

                var result = vm.Pop();

                if (!result.IsFalsey)
                {
                    break;
                }
            }

            return NativeCallResult.SuccessfulExpression;
        }

        public override InstanceInternal MakeInstance() => CreateInstance();

        public static InstanceInternal CreateInstance() 
            => new NativeListInstance(SharedNativeListClassValue.val.asClass);
    }
}
