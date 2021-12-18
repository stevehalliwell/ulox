namespace ULox
{
    public class FreezeLibrary : IULoxLibrary
    {
        public string Name => nameof(FreezeLibrary);

        public Table GetBindings()
            => this.GenerateBindingTable(
                (nameof(IsFrozen), Value.New(IsFrozen)),
                (nameof(Unfreeze), Value.New(Unfreeze))
                );
       

        public NativeCallResult IsFrozen(VMBase vm, int argCount)
        {
            var target = vm.GetArg(1);
            if (target.type == ValueType.Instance)
                vm.PushReturn(Value.New(target.val.asInstance.IsFrozen));
            else if(target.type == ValueType.Class)
                vm.PushReturn(Value.New(target.val.asClass.IsFrozen));
            else
                vm.PushReturn(Value.Null());

            return NativeCallResult.Success;
        }

        public NativeCallResult Unfreeze(VMBase vm, int argCount)
        {
            var target = vm.GetArg(1);
            if (target.type == ValueType.Instance)
                target.val.asInstance.Unfreeze();
            if (target.type == ValueType.Class)
                target.val.asClass.Unfreeze();

            vm.PushReturn(Value.Null());
            return NativeCallResult.Success;
        }
    }
}
