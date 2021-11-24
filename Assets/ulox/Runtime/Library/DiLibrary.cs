namespace ULox
{
    public class DiLibrary : IULoxLibrary
    {
        public string Name => nameof(DiLibrary);

        public Table GetBindings()
        {
            var resTable = new Table();
            var diLibInst = new InstanceInternal();
            resTable.Add(new HashedString("DI"), Value.New(diLibInst));

            diLibInst.AddFieldsToInstance(
                (nameof(Count), Value.New(Count)),
                (nameof(GenerateDump), Value.New(GenerateDump)),
                (nameof(Freeze), Value.New(Freeze))
                );
            diLibInst.Freeze();

            return resTable;
        }

        private Value Count(VMBase vm, int argCount)
        {
            var di = FromVm(vm);
            return Value.New(di.Count);
        }

        private Value GenerateDump(VMBase vm, int argCount)
        {
            var di = FromVm(vm);
            return Value.New(di.GenerateDump());
        }

        //TODO rename this is confusing now that objects can freeze
        private Value Freeze(VMBase vm, int argCount)
        {
            var di = FromVm(vm);
            di.Freeze();
            return Value.Null();
        }

        private DiContainer FromVm(VMBase vMBase)
        {
            if(vMBase is Vm vm)
            {
                return vm.DiContainer;
            }
            throw new LoxException($"DiLibrary action taken on incommpatible vm.");
        }
    }
}
