namespace ULox
{
    public class DebugLibrary : ILoxByteCodeLibrary
    {
        public Table GetBindings()
        {
            var resTable = new Table();

            resTable.Add("GenerateStackDump", Value.New(
                (vm, args) =>
                {
                    return Value.New(vm.GenerateStackDump());
                }));
            resTable.Add("GenerateGlobalsDump", Value.New(
                (vm, args) =>
                {
                    return Value.New(vm.GenerateGlobalsDump());
                }));

            return resTable;
        }
    }
}
