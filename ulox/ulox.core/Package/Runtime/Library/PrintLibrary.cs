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
                (nameof(print), Value.New(print,1,1)),
                (nameof(printh), Value.New(printh, 1,1))
                                        );

        public NativeCallResult print(Vm vm)
        {
            _printer.Invoke(vm.GetArg(1).ToString());
            return NativeCallResult.SuccessfulExpression;
        }

        public NativeCallResult printh(Vm vm)
        {
            var val = vm.GetArg(1);
            var valWriter = new StringBuilderValueHierarchyWriter();
            var objWalker = new ValueHierarchyWalker(valWriter);
            objWalker.Walk(val);
            _printer.Invoke(valWriter.GetString());
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
