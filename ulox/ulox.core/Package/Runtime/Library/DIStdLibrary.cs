namespace ULox
{
    internal static class DIStdLibrary
    {
        internal static InstanceInternal MakeDIInstance()
        {
            var diLibInst = new InstanceInternal();
            diLibInst.AddFieldsToInstance(
            (nameof(Count), Value.New(Count)),
                (nameof(GenerateDump), Value.New(GenerateDump)),
                (nameof(Freeze), Value.New(Freeze)));
            diLibInst.Freeze();
            return diLibInst;
        }

        private static NativeCallResult Count(Vm vm, int argCount)
        {
            var di = FromVm(vm);
            vm.PushReturn(Value.New(di.Count));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult GenerateDump(Vm vm, int argCount)
        {
            var di = FromVm(vm);
            vm.PushReturn(Value.New(di.GenerateDump()));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Freeze(Vm vm, int argCount)
        {
            var di = FromVm(vm);
            di.Freeze();
            return NativeCallResult.SuccessfulExpression;
        }

        private static DiContainer FromVm(Vm vMBase)
            => vMBase.DiContainer;
    }
}
