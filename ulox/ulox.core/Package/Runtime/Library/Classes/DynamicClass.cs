﻿namespace ULox
{
    public sealed class DynamicClass : UserTypeInternal
    {
        public static readonly HashedString DynamicClassName = new HashedString("Dynamic");
        public static readonly Value SharedDynamicClassValue = Value.New(new DynamicClass());

        public DynamicClass() : base(DynamicClassName, UserType.Native)
        {
            this.AddMethodsToClass(
                (nameof(HasField), Value.New(HasField, 1, 2)),
                (nameof(RemoveField), Value.New(RemoveField, 1, 2))
                                  );
        }

        private NativeCallResult HasField(Vm vm)
        {
            var obj = vm.GetArg(1);
            var fieldName = vm.GetArg(2);
            if (obj.type != ValueType.Instance || fieldName.type != ValueType.String)
                vm.ThrowRuntimeException($"Cannot perform {nameof(HasField)} on given types, '{obj}', '{fieldName}'");

            var inst = obj.val.asInstance;
            var b = inst.HasField(fieldName.val.asString);

            vm.SetNativeReturn(0, Value.New(b));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult RemoveField(Vm vm)
        {
            var obj = vm.GetArg(1);
            var fieldName = vm.GetArg(2);
            if (obj.type != ValueType.Instance || fieldName.type != ValueType.String)
                vm.ThrowRuntimeException($"Cannot perform {nameof(RemoveField)} on given types, '{obj}', '{fieldName}'");

            var inst = obj.val.asInstance;

            if (inst.IsReadOnly)
                vm.ThrowRuntimeException($"Cannot remove field from read only instance, '{inst}'");

            var fieldNameStr = fieldName.val.asString;
            inst.RemoveField(fieldNameStr);

            return NativeCallResult.SuccessfulExpression;
        }

        public override void FinishCreation(InstanceInternal inst)
        {
            base.FinishCreation(inst);
            inst.Unfreeze();
        }
    }
}
