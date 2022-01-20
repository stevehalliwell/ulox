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

        public NativeCallResult GenerateStackDump(Vm vm, int argCount)
        {
            vm.PushReturn(Value.New(vm.GenerateStackDump()));
            return NativeCallResult.SuccessfulExpression;
        }

        public NativeCallResult GenerateGlobalsDump(Vm vm, int argCount)
        {
            vm.PushReturn(Value.New(vm.GenerateGlobalsDump()));
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
