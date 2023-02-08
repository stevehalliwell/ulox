using System.Runtime.CompilerServices;

namespace ULox
{
    internal static class DIStdLibrary
    {
        internal static InstanceInternal MakeDIInstance()
        {
            var diLibInst = new InstanceInternal();
            diLibInst.AddFieldsToInstance(
                (nameof(Count), Value.New(Count, 1, 0)),
                (nameof(GenerateDump), Value.New(GenerateDump, 1, 0)),
                (nameof(Freeze), Value.New(Freeze, 1, 0)));
            diLibInst.Freeze();
            return diLibInst;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Count(Vm vm)
        {
            var di = FromVm(vm);
            vm.SetNativeReturn(0, Value.New(di.Count));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult GenerateDump(Vm vm)
        {
            var di = FromVm(vm);
            vm.SetNativeReturn(0, Value.New(di.GenerateDump()));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Freeze(Vm vm)
        {
            var di = FromVm(vm);
            di.Freeze();
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DiContainer FromVm(Vm vMBase)
            => vMBase.DiContainer;
    }
}
