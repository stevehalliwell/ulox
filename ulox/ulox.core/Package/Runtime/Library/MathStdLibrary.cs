using System;
using System.Runtime.CompilerServices;

namespace ULox
{
    internal static class MathStdLibrary
    {
        private static readonly Random _random = new Random();

        internal static InstanceInternal MakeMathInstance()
        {
            var diLibInst = new InstanceInternal();
            diLibInst.AddFieldsToInstance(
                (nameof(Acos), Value.New(Acos)),
                (nameof(Asin), Value.New(Asin)),
                (nameof(Atan), Value.New(Atan)),
                (nameof(Atan2), Value.New(Atan2)),
                (nameof(Cos), Value.New(Cos)),
                (nameof(Deg2Rad), Value.New(Deg2Rad)),
                (nameof(E), Value.New(E)),
                (nameof(Exp), Value.New(Exp)),
                (nameof(Ln), Value.New(Ln)),
                (nameof(Log), Value.New(Log)),
                (nameof(Pi), Value.New(Pi)),
                (nameof(Pow), Value.New(Pow)),
                (nameof(Rad2Deg), Value.New(Rad2Deg)),
                (nameof(Rand), Value.New(Rand)),
                (nameof(Sin), Value.New(Sin)),
                (nameof(Sqrt), Value.New(Sqrt)),
                (nameof(Tan), Value.New(Tan)));

            diLibInst.Freeze();
            return diLibInst;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Rand(Vm vm, int argCount)
        {
            var result = _random.NextDouble();
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Sin(Vm vm, int argCount)
        {
            var arg = vm.GetArg(1);
            var result = Math.Sin(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Cos(Vm vm, int argCount)
        {
            var arg = vm.GetArg(1);
            var result = Math.Cos(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Tan(Vm vm, int argCount)
        {
            var arg = vm.GetArg(1);
            var result = Math.Tan(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Asin(Vm vm, int argCount)
        {
            var arg = vm.GetArg(1);
            var result = Math.Asin(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Acos(Vm vm, int argCount)
        {
            var arg = vm.GetArg(1);
            var result = Math.Acos(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Atan(Vm vm, int argCount)
        {
            var arg = vm.GetArg(1);
            var result = Math.Atan(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Atan2(Vm vm, int argCount)
        {
            var y = vm.GetArg(1);
            var x = vm.GetArg(2);
            var result = Math.Atan2(y.val.asDouble, x.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Rad2Deg(Vm vm, int argCount)
        {
            var arg = vm.GetArg(1);
            var result = arg.val.asDouble * (180.0 / Math.PI);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Deg2Rad(Vm vm, int argCount)
        {
            var arg = vm.GetArg(1);
            var result = arg.val.asDouble * (Math.PI / 180.0);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Pi(Vm vm, int argCount)
        {
            var result = Math.PI;
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult E(Vm vm, int argCount)
        {
            var result = Math.E;
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Sqrt(Vm vm, int argCount)
        {
            var arg = vm.GetArg(1);
            var result = Math.Sqrt(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Pow(Vm vm, int argCount)
        {
            var arg1 = vm.GetArg(1);
            var arg2 = vm.GetArg(2);
            var result = Math.Pow(arg1.val.asDouble, arg2.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Exp(Vm vm, int argCount)
        {
            var arg1 = vm.GetArg(1);
            var result = Math.Exp(arg1.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Ln(Vm vm, int argCount)
        {
            var arg1 = vm.GetArg(1);
            var result = Math.Log(arg1.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Log(Vm vm, int argCount)
        {
            var arg1 = vm.GetArg(1);
            var arg2 = vm.GetArg(2);
            var result = Math.Log(arg1.val.asDouble, arg2.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
