namespace ULox
{
    public class CoreLibrary : ILoxByteCodeLibrary
    {
        private System.Action<string> _printer;

        public CoreLibrary(System.Action<string> printer)
        {
            _printer = printer;
        }

        public Table GetBindings()
        {
            var resTable = new Table();

            resTable.Add(nameof(print), Value.New(print));
            resTable.Add(nameof(Duplicate), Value.New(Duplicate));

            return resTable;
        }

        public Value print(VMBase vm, int argCount)
        {
            _printer?.Invoke(vm.GetArg(1).ToString());
            return Value.Null();
        }

        public Value Duplicate(VMBase vm, int argCount)
        {
            return Value.Copy(vm.GetArg(1));
        }
    }
}
