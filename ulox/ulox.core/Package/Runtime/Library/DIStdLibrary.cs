using System.Runtime.CompilerServices;

namespace ULox
{
    internal static class DIStdLibrary
    {
        internal static InstanceInternal MakeDIInstance()
        {
            var diLibInst = new InstanceInternal();
            diLibInst.AddFieldsToInstance(
            (nameof(Count), Value.New(Count)),
                (nameof(GenerateDump), Value.New(GenerateDump)),
                (nameof(Freeze), Value.New(Freeze)));
            diLibInst.Freeze();
            return diLibInst;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Count(Vm vm, int argCount)
        {
            var di = FromVm(vm);
            vm.SetNativeReturn(0, Value.New(di.Count));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult GenerateDump(Vm vm, int argCount)
        {
            var di = FromVm(vm);
            vm.SetNativeReturn(0, Value.New(di.GenerateDump()));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Freeze(Vm vm, int argCount)
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
