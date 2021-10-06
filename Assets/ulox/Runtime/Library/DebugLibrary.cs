namespace ULox
{
    public class DebugLibrary : IULoxLibrary
    {
        public string Name => nameof(DebugLibrary);

        public Table GetBindings()
        {
            var resTable = new Table();

            resTable.Add(nameof(GenerateStackDump), Value.New(GenerateStackDump));
            resTable.Add(nameof(GenerateGlobalsDump), Value.New(GenerateGlobalsDump));

            return resTable;
        }

        public Value GenerateStackDump(VMBase vm, int argCount)
        {
            return Value.New(vm.GenerateStackDump());
        }

        public Value GenerateGlobalsDump(VMBase vm, int argCount)
        {
            return Value.New(vm.GenerateGlobalsDump());
        }
    }
}
