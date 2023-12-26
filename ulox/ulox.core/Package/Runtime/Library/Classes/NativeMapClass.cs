using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class NativeMapClass : UserTypeInternal
    {
        public static readonly Value SharedNativeMapClassValue = Value.New(new NativeMapClass());

        public override InstanceInternal MakeInstance()
        {
            return new NativeMapInstance(this);
        }

        public NativeMapClass()
            : base(new HashedString("NativeMap"), UserType.Native)
        {
            this.AddMethodsToClass(
                (nameof(Count), Value.New(Count,1, 0)),
                (nameof(Create), Value.New(Create, 1, 2)),
                (nameof(CreateOrUpdate), Value.New(CreateOrUpdate, 1, 2)),
                (nameof(Read), Value.New(Read, 1, 1)),
                (nameof(ReadOrDefault), Value.New(ReadOrDefault, 1, 2)),
                (nameof(Update), Value.New(Update, 1, 2)),
                (nameof(Delete), Value.New(Delete, 1, 1))
                                  );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<Value, Value> GetArg0NativeMapInstance(Vm vm)
        {
            var inst = vm.GetArg(0);
            var nativeMapinst = inst.val.asInstance as NativeMapInstance;
            return nativeMapinst.Map;
        }

        private NativeCallResult Count(Vm vm)
        {
            vm.SetNativeReturn(0, Value.New(GetArg0NativeMapInstance(vm).Count));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Create(Vm vm)
        {
            ThrowIfReadOnly(vm);
            var map = GetArg0NativeMapInstance(vm);
            var key = vm.GetArg(1);
            var val = vm.GetArg(2);

            if (map.ContainsKey(key))
            {
                vm.SetNativeReturn(0, Value.New(false));
                return NativeCallResult.SuccessfulExpression;
            }

            map[key] = val;
            vm.SetNativeReturn(0, Value.New(true));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult CreateOrUpdate(Vm vm)
        {
            ThrowIfReadOnly(vm);
            var map = GetArg0NativeMapInstance(vm);
            var key = vm.GetArg(1);
            var val = vm.GetArg(2);

            map[key] = val;
            
            vm.SetNativeReturn(0, vm.GetArg(0));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Read(Vm vm)
        {
            var map = GetArg0NativeMapInstance(vm);
            var key = vm.GetArg(1);

            if (map.TryGetValue(key, out var val))
            {
                vm.SetNativeReturn(0, val);
                return NativeCallResult.SuccessfulExpression;
            }
            
            throw new UloxException($"Map contains no key of '{key}'.");
        }

        private NativeCallResult ReadOrDefault(Vm vm)
        {
            var map = GetArg0NativeMapInstance(vm);
            var key = vm.GetArg(1);

            if (map.TryGetValue(key, out var val))
            {
                vm.SetNativeReturn(0, val);
                return NativeCallResult.SuccessfulExpression;
            }

            vm.SetNativeReturn(0, vm.GetArg(2));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Update(Vm vm)
        {
            ThrowIfReadOnly(vm);
            var map = GetArg0NativeMapInstance(vm);
            var key = vm.GetArg(1);
            var val = vm.GetArg(2);

            if (!map.ContainsKey(key))
            {
                vm.SetNativeReturn(0, Value.New(false));
                return NativeCallResult.SuccessfulExpression;
            }

            map[key] = val;
            vm.SetNativeReturn(0, Value.New(true));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Delete(Vm vm)
        {
            ThrowIfReadOnly(vm);
            var map = GetArg0NativeMapInstance(vm);
            var key = vm.GetArg(1);

            vm.SetNativeReturn(0, Value.New(map.Remove(key)));
            return NativeCallResult.SuccessfulExpression;
        }

        private static void ThrowIfReadOnly(Vm vm)
        {
            var inst = vm.GetArg(0);
            var nativeMapInstance = inst.val.asInstance as NativeMapInstance;
            if (nativeMapInstance.IsReadOnly)
                vm.ThrowRuntimeException($"Attempted to modify a read only map");
        }
    }
}
