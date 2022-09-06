namespace ULox
{
    //TODO using wrong exception types in here
    public class DynamicClass : ClassInternal
    {
        public static readonly HashedString DynamicClassName = new HashedString("Dynamic");
        public static readonly Value SharedDynamicClassValue = Value.New(new DynamicClass());

        public DynamicClass() : base(DynamicClassName)
        {
            this.AddMethodsToClass(
                (nameof(HasField), Value.New(HasField)),
                (nameof(RemoveField), Value.New(RemoveField))
                                  );
        }

        private NativeCallResult HasField(Vm vm, int argCount)
        {
            var obj = vm.GetArg(1);
            var fieldName = vm.GetArg(2);
            if (obj.type != ValueType.Instance || fieldName.type != ValueType.String)
                throw new AssertException($"Cannot perform {nameof(HasField)} on given types, '{obj}', '{fieldName}'.");

            var inst = obj.val.asInstance;
            var b = inst.HasField(fieldName.val.asString);

            vm.PushReturn(Value.New(b));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult RemoveField(Vm vm, int argCount)
        {
            var obj = vm.GetArg(1);
            var fieldName = vm.GetArg(2);
            if (obj.type != ValueType.Instance || fieldName.type != ValueType.String)
                throw new AssertException($"Cannot perform {nameof(RemoveField)} on given types, '{obj}', '{fieldName}'.");

            var inst = obj.val.asInstance;
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
