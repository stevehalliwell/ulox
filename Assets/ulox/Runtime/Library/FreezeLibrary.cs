namespace ULox
{
    public class FreezeLibrary : IULoxLibrary
    {
        public string Name => nameof(FreezeLibrary);

        public Table GetBindings()
        {
            var resTable = new Table();

            resTable.Add(nameof(IsFrozen), Value.New(IsFrozen));
            resTable.Add(nameof(Unfreeze), Value.New(Unfreeze));

            return resTable;
        }

        public Value IsFrozen(VMBase vm, int argCount)
        {
            var target = vm.GetArg(1);
            if (target.type == ValueType.Instance)
                return Value.New(target.val.asInstance.IsFrozen);
            if(target.type == ValueType.Class)
                return Value.New(target.val.asClass.IsFrozen);

            return Value.Null();
        }

        public Value Unfreeze(VMBase vm, int argCount)
        {
            var target = vm.GetArg(1);
            if (target.type == ValueType.Instance)
                target.val.asInstance.Unfreeze();
            if (target.type == ValueType.Class)
                target.val.asClass.Unfreeze();

            return Value.Null();
        }
    }
}
