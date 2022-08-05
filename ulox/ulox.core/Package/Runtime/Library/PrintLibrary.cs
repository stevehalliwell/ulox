namespace ULox
{
    public class PrintLibrary : IULoxLibrary
    {
        private readonly System.Action<string> _printer;

        public string Name => nameof(PrintLibrary);

        public PrintLibrary(System.Action<string> printer)
        {
            _printer = printer;
        }

        public Table GetBindings()
            => this.GenerateBindingTable(
                (nameof(print), Value.New(print))
                                        );

        public NativeCallResult print(Vm vm, int argCount)
        {
            _printer.Invoke(vm.GetArg(1).ToString());
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
