namespace ULox
{
    public sealed class PrintLibrary : IULoxLibrary
    {
        private readonly System.Action<string> _printer;

        public PrintLibrary(System.Action<string> printer)
        {
            _printer = printer;
        }

        public Table GetBindings()
            => this.GenerateBindingTable(
                (nameof(print), Value.New(print,1,1))
                                        );

        public NativeCallResult print(Vm vm)
        {
            _printer.Invoke(vm.GetArg(1).ToString());
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
