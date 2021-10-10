namespace ULox
{
    public class DynamicClass : ClassInternal
    {
        public const string Name = "Dynamic";

        public DynamicClass() : base(Name)
        {
            this.AddMethod(nameof(HasField), Value.New(HasField));
            this.AddMethod(nameof(RemoveField), Value.New(RemoveField));
        }

        private Value HasField(VMBase vm, int argCount)
        {
            var obj = vm.GetArg(1);
            var fieldName = vm.GetArg(2);
            if (obj.type != ValueType.Instance || fieldName.type != ValueType.String)
                throw new AssertException($"Cannot perform {nameof(HasField)} on given types, '{obj}', '{fieldName}'.");

            var inst = obj.val.asInstance;
            var b = inst.HasField(fieldName.val.asString);

            return Value.New(b);
        }

        private Value RemoveField(VMBase vm, int argCount)
        {
            var obj = vm.GetArg(1);
            var fieldName = vm.GetArg(2);
            if (obj.type != ValueType.Instance || fieldName.type != ValueType.String)
                throw new AssertException($"Cannot perform {nameof(RemoveField)} on given types, '{obj}', '{fieldName}'.");

            var inst = obj.val.asInstance;
            var fieldNameStr = fieldName.val.asString;
            var currentValue = inst.GetField(fieldNameStr);
            inst.RemoveField(fieldNameStr);

            return currentValue;
        }

        public override void FinishCreation(InstanceInternal inst)
        {
            base.FinishCreation(inst);
            inst.Unfreeze();
        }
    }
}
