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

        private NativeCallResult Count(Vm vm, int argCount)
        {
            var di = FromVm(vm);
            vm.PushReturn(Value.New(di.Count));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult GenerateDump(Vm vm, int argCount)
        {
            var di = FromVm(vm);
            vm.PushReturn(Value.New(di.GenerateDump()));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Freeze(Vm vm, int argCount)
        {
            var di = FromVm(vm);
            di.Freeze();
            return NativeCallResult.SuccessfulExpression;
        }

        private DiContainer FromVm(Vm vMBase)
            => vMBase.DiContainer;
    }
}
