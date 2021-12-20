namespace ULox
{
    public class CoreLibrary : IULoxLibrary
    {
        private System.Action<string> _printer;

        public string Name => nameof(CoreLibrary);

        public CoreLibrary(System.Action<string> printer)
        {
            _printer = printer;
        }

        public Table GetBindings()
            => this.GenerateBindingTable(
                (nameof(print), Value.New(print)),
                (nameof(Duplicate), Value.New(Duplicate)),
                (nameof(str), Value.New(str))
                );

        public NativeCallResult print(VMBase vm, int argCount)
        {
            _printer?.Invoke(vm.GetArg(1).ToString());
            return NativeCallResult.SuccessfulExpression;
        }

        public NativeCallResult str(VMBase vm, int argCount)
        {
            var v = vm.GetArg(1);
            vm.PushReturn(Value.New(v.str()));
            return NativeCallResult.SuccessfulExpression;
        }

        public NativeCallResult Duplicate(VMBase vm, int argCount)
        {
            vm.PushReturn(Value.Copy(vm.GetArg(1)));
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
