using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
                (nameof(Remove), Value.New(Remove))
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

        public override InstanceInternal MakeInstance()
        {
            return new NativeListInstance(this);
        }
    }
}
