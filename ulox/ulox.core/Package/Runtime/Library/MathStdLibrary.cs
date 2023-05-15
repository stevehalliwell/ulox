﻿using System;
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
                (nameof(Acos), Value.New(Acos,1,1)),
                (nameof(Abs), Value.New(Abs,1,1)),
                (nameof(Asin), Value.New(Asin, 1, 1)),
                (nameof(Atan), Value.New(Atan, 1, 1)),
                (nameof(Atan2), Value.New(Atan2, 1, 2)),
                (nameof(Cos), Value.New(Cos, 1, 1)),
                (nameof(Deg2Rad), Value.New(Deg2Rad, 1, 1)),
                (nameof(E), Value.New(E, 1, 0)),
                (nameof(Exp), Value.New(Exp, 1, 1)),
                (nameof(Ln), Value.New(Ln, 1, 1)),
                (nameof(Log), Value.New(Log, 1, 2)),
                (nameof(Pi), Value.New(Pi, 1, 0)),
                (nameof(Pow), Value.New(Pow, 1, 2)),
                (nameof(Rad2Deg), Value.New(Rad2Deg, 1, 1)),
                (nameof(Rand), Value.New(Rand, 1, 0)),
                (nameof(Round), Value.New(Round, 1, 1)),
                (nameof(Sin), Value.New(Sin, 1, 1)),
                (nameof(Sign), Value.New(Sign, 1, 1)),
                (nameof(Sqrt), Value.New(Sqrt, 1, 1)),
                (nameof(Tan), Value.New(Tan, 1, 1)));

            diLibInst.Freeze();
            return diLibInst;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Rand(Vm vm)
        {
            var result = _random.NextDouble();
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Round(Vm vm)
        {
            var arg = vm.GetArg(1);
            vm.SetNativeReturn(0, Value.New(Math.Round(arg.val.asDouble)));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Sin(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = Math.Sin(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Cos(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = Math.Cos(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Tan(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = Math.Tan(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Asin(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = Math.Asin(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Acos(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = Math.Acos(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Atan(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = Math.Atan(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Atan2(Vm vm)
        {
            var y = vm.GetArg(1);
            var x = vm.GetArg(2);
            var result = Math.Atan2(y.val.asDouble, x.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Rad2Deg(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = arg.val.asDouble * (180.0 / Math.PI);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Deg2Rad(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = arg.val.asDouble * (Math.PI / 180.0);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Pi(Vm vm)
        {
            var result = Math.PI;
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult E(Vm vm)
        {
            var result = Math.E;
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Sqrt(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = Math.Sqrt(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Pow(Vm vm)
        {
            var arg1 = vm.GetArg(1);
            var arg2 = vm.GetArg(2);
            var result = Math.Pow(arg1.val.asDouble, arg2.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Exp(Vm vm)
        {
            var arg1 = vm.GetArg(1);
            var result = Math.Exp(arg1.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Ln(Vm vm)
        {
            var arg1 = vm.GetArg(1);
            var result = Math.Log(arg1.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Log(Vm vm)
        {
            var arg1 = vm.GetArg(1);
            var arg2 = vm.GetArg(2);
            var result = Math.Log(arg1.val.asDouble, arg2.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Abs(Vm vm)
        {
            var arg1 = vm.GetArg(1);
            var result = Math.Abs(arg1.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Sign(Vm vm)
        {
            var arg1 = vm.GetArg(1);
            var result = Math.Sign(arg1.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }
    }
}