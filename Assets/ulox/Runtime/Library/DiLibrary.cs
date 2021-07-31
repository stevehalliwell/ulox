namespace ULox
{
    public class DiLibrary : IULoxLibrary
    {
        public string Name => nameof(DiLibrary);

        public Table GetBindings()
        {
            var resTable = new Table();
            var assertInst = new InstanceInternal();
            resTable.Add("DI", Value.New(assertInst));

            assertInst.fields[nameof(Count)] = Value.New(Count);
            assertInst.fields[nameof(GenerateDump)] = Value.New(GenerateDump);
            assertInst.fields[nameof(Freeze)] = Value.New(Freeze);

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
