using System.Runtime.CompilerServices;

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
                (nameof(print), Value.New(print))
                                        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeCallResult print(Vm vm, int argCount)
        {
            _printer.Invoke(vm.GetArg(1).ToString());
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
