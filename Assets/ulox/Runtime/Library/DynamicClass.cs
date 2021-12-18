namespace ULox
{
    public class DynamicClass : ClassInternal
    {
        public static readonly HashedString Name = new HashedString("Dynamic");

        public DynamicClass() : base(Name)
        {
            this.AddMethodsToClass(
                (nameof(HasField), Value.New(HasField)),
                (nameof(RemoveField), Value.New(RemoveField))
                );
        }

        private NativeCallResult HasField(VMBase vm, int argCount)
        {
            var obj = vm.GetArg(1);
            var fieldName = vm.GetArg(2);
            if (obj.type != ValueType.Instance || fieldName.type != ValueType.String)
                throw new AssertException($"Cannot perform {nameof(HasField)} on given types, '{obj}', '{fieldName}'.");

            var inst = obj.val.asInstance;
            var b = inst.HasField(fieldName.val.asString);

            vm.PushReturn(Value.New(b));
            return NativeCallResult.Success;
        }

        private NativeCallResult RemoveField(VMBase vm, int argCount)
        {
            var obj = vm.GetArg(1);
            var fieldName = vm.GetArg(2);
            if (obj.type != ValueType.Instance || fieldName.type != ValueType.String)
                throw new AssertException($"Cannot perform {nameof(RemoveField)} on given types, '{obj}', '{fieldName}'.");

            var inst = obj.val.asInstance;
            var fieldNameStr = fieldName.val.asString;
            inst.RemoveField(fieldNameStr);

            vm.PushReturn(Value.Null());
            return NativeCallResult.Success;
        }

        public override void FinishCreation(InstanceInternal inst)
        {
            base.FinishCreation(inst);
            inst.Unfreeze();
        }
    }
}
