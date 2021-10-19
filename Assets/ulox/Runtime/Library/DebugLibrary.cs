namespace ULox
{
    public class DebugLibrary : IULoxLibrary
    {
        public string Name => nameof(DebugLibrary);

        public Table GetBindings()
            => this.GenerateBindingTable(
                (nameof(GenerateStackDump), Value.New(GenerateStackDump)),
                (nameof(GenerateGlobalsDump), Value.New(GenerateGlobalsDump))
                );

        public Value GenerateStackDump(VMBase vm, int argCount) => Value.New(vm.GenerateStackDump());

        public Value GenerateGlobalsDump(VMBase vm, int argCount) => Value.New(vm.GenerateGlobalsDump());
    }
}
