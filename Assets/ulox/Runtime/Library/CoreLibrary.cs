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
        {
            var resTable = new Table();

            resTable.Add(nameof(print), Value.New(print));
            resTable.Add(nameof(Duplicate), Value.New(Duplicate));
            resTable.Add(nameof(str), Value.New(str));

            return resTable;
        }

        public Value print(VMBase vm, int argCount)
        {
            _printer?.Invoke(vm.GetArg(1).ToString());
            return Value.Null();
        }

        public Value str(VMBase vm, int argCount)
        {
            var v = vm.GetArg(1);
            return Value.New(v.str());
        }

        public Value Duplicate(VMBase vm, int argCount)
        {
            return Value.Copy(vm.GetArg(1));
        }
    }
}
