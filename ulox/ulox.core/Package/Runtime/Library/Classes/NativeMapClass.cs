using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class NativeMapClass : UserTypeInternal
    {
        public static readonly Value SharedNativeMapClassValue = Value.New(new NativeMapClass());

        public NativeMapClass()
            : base(new HashedString("NativeMap"), UserType.Native)
        {
            this.AddMethodsToClass(
                (nameof(Count), Value.New(Count)),
                (nameof(Create), Value.New(Create)),
                (nameof(Read), Value.New(Read)),
                (nameof(ReadOrDefault), Value.New(ReadOrDefault)),
                (nameof(Update), Value.New(Update)),
                (nameof(Delete), Value.New(Delete))
                                  );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<Value, Value> GetArg0NativeMapInstance(Vm vm)
        {
            var inst = vm.GetArg(0);
            var nativeMapinst = inst.val.asInstance as NativeMapInstance;
            return nativeMapinst.Map;
        }

        private NativeCallResult Count(Vm vm, int argCount)
        {
            vm.PushReturn(Value.New(GetArg0NativeMapInstance(vm).Count));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Create(Vm vm, int argCount)
        {
            var map = GetArg0NativeMapInstance(vm);
            var key = vm.GetArg(1);
            var val = vm.GetArg(2);

            if (map.ContainsKey(key))
            {
                vm.PushReturn(Value.New(false));
                return NativeCallResult.SuccessfulExpression;
            }

            map[key] = val;
            vm.PushReturn(Value.New(true));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Read(Vm vm, int argCount)
        {
            var map = GetArg0NativeMapInstance(vm);
            var key = vm.GetArg(1);

            if (map.TryGetValue(key, out var val))
            {
                vm.PushReturn(val);
                return NativeCallResult.SuccessfulExpression;
            }
            
            throw new UloxException($"Map contains no key of '{key}'.");
        }

        private NativeCallResult ReadOrDefault(Vm vm, int argCount)
        {
            var map = GetArg0NativeMapInstance(vm);
            var key = vm.GetArg(1);

            if (map.TryGetValue(key, out var val))
            {
                vm.PushReturn(val);
                return NativeCallResult.SuccessfulExpression;
            }

            vm.PushReturn(vm.GetArg(2));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Update(Vm vm, int argCount)
        {
            var map = GetArg0NativeMapInstance(vm);
            var key = vm.GetArg(1);
            var val = vm.GetArg(2);

            if (!map.ContainsKey(key))
            {
                vm.PushReturn(Value.New(false));
                return NativeCallResult.SuccessfulExpression;
            }

            map[key] = val;
            vm.PushReturn(Value.New(true));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Delete(Vm vm, int argCount)
        {
            var map = GetArg0NativeMapInstance(vm);
            var key = vm.GetArg(1);

            vm.PushReturn(Value.New(map.Remove(key)));
            return NativeCallResult.SuccessfulExpression;
        }

        public override InstanceInternal MakeInstance()
        {
            return new NativeMapInstance(this);
        }
    }
}
