using System;

namespace ULox
{
    internal static class MathStdLibrary
    {
        private static readonly Random _random = new();

        internal static InstanceInternal MakeInstance()
        {
            var diLibInst = new InstanceInternal();
            diLibInst.AddFieldsToInstance(
                (nameof(Acos), Value.New(Acos, 1, 1)),
                (nameof(Abs), Value.New(Abs, 1, 1)),
                (nameof(AbsSign), Value.New(AbsSign, 2, 1)),
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
                (nameof(RandRange), Value.New(RandRange, 1, 2)),
                (nameof(RandUnitCircle), Value.New(RandUnitCircle, 2, 0)),
                (nameof(Round), Value.New(Round, 1, 1)),
                (nameof(Floor), Value.New(Floor, 1, 1)),
                (nameof(Ceil), Value.New(Ceil, 1, 1)),
                (nameof(Sin), Value.New(Sin, 1, 1)),
                (nameof(Sign), Value.New(Sign, 1, 1)),
                (nameof(Sqrt), Value.New(Sqrt, 1, 1)),
                (nameof(Tan), Value.New(Tan, 1, 1)),
                (nameof(Max), Value.New(Max, 1, 2)),
                (nameof(Min), Value.New(Min, 1, 2)),
                (nameof(Clamp), Value.New(Clamp, 1, 3)),
                (nameof(MoveTowards), Value.New(MoveTowards, 1, 3)),
                (nameof(Lerp), Value.New(Lerp, 1, 3)),
                (nameof(Dampen), Value.New(Dampen, 1, 4)),
                (nameof(CalcDampenHalflife), Value.New(CalcDampenHalflife, 1, 2)),
                (nameof(Remap), Value.New(Remap, 1, 5)),
                (nameof(RemapClamp), Value.New(RemapClamp, 1, 5)),
                (nameof(SinCos), Value.New(SinCos, 2, 1))
                );

            diLibInst.Freeze();
            return diLibInst;
        }

        private static NativeCallResult Rand(Vm vm)
        {
            var result = _random.NextDouble();
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult RandRange(Vm vm)
        {
            var result = _random.NextDouble();
            var min = vm.GetArg(1).val.asDouble;
            var max = vm.GetArg(2).val.asDouble;
            result = min + result * (max - min);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult RandUnitCircle(Vm vm)
        {
            var a = _random.NextDouble() * 2 * Math.PI;
            var r = Math.Sqrt(_random.NextDouble());
            var x = r * Math.Cos(a);
            var y = r * Math.Sin(a);
            vm.SetNativeReturn(0, Value.New(x));
            vm.SetNativeReturn(1, Value.New(y));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Round(Vm vm)
        {
            var arg = vm.GetArg(1);
            vm.SetNativeReturn(0, Value.New(Math.Round(arg.val.asDouble)));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Floor(Vm vm)
        {
            var arg = vm.GetArg(1);
            vm.SetNativeReturn(0, Value.New(Math.Floor(arg.val.asDouble)));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Ceil(Vm vm)
        {
            var arg = vm.GetArg(1);
            vm.SetNativeReturn(0, Value.New(Math.Ceiling(arg.val.asDouble)));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Sin(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = Math.Sin(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult SinCos(Vm vm)
        {
            var arg = vm.GetArg(1);
            var r = arg.val.asDouble;
            vm.SetNativeReturn(0, Value.New(Math.Sin(r)));
            vm.SetNativeReturn(1, Value.New(Math.Cos(r)));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Cos(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = Math.Cos(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Tan(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = Math.Tan(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Asin(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = Math.Asin(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Acos(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = Math.Acos(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Atan(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = Math.Atan(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Atan2(Vm vm)
        {
            var y = vm.GetArg(1);
            var x = vm.GetArg(2);
            var result = Math.Atan2(y.val.asDouble, x.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Rad2Deg(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = arg.val.asDouble * (180.0 / Math.PI);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Deg2Rad(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = arg.val.asDouble * (Math.PI / 180.0);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Pi(Vm vm)
        {
            var result = Math.PI;
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult E(Vm vm)
        {
            var result = Math.E;
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Sqrt(Vm vm)
        {
            var arg = vm.GetArg(1);
            var result = Math.Sqrt(arg.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Pow(Vm vm)
        {
            var arg1 = vm.GetArg(1);
            var arg2 = vm.GetArg(2);
            var result = Math.Pow(arg1.val.asDouble, arg2.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Exp(Vm vm)
        {
            var arg1 = vm.GetArg(1);
            var result = Math.Exp(arg1.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Ln(Vm vm)
        {
            var arg1 = vm.GetArg(1);
            var result = Math.Log(arg1.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Log(Vm vm)
        {
            var arg1 = vm.GetArg(1);
            var arg2 = vm.GetArg(2);
            var result = Math.Log(arg1.val.asDouble, arg2.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Abs(Vm vm)
        {
            var arg1 = vm.GetArg(1);
            var result = Math.Abs(arg1.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult AbsSign(Vm vm)
        {
            var arg1 = vm.GetArg(1);
            var sign = Math.Sign(arg1.val.asDouble);
            var result = Math.Abs(arg1.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            vm.SetNativeReturn(1, Value.New(sign));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Sign(Vm vm)
        {
            var arg1 = vm.GetArg(1);
            var result = Math.Sign(arg1.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Max(Vm vm)
        {
            var arg1 = vm.GetArg(1);
            var arg2 = vm.GetArg(2);
            var result = Math.Max(arg1.val.asDouble, arg2.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Min(Vm vm)
        {
            var arg1 = vm.GetArg(1);
            var arg2 = vm.GetArg(2);
            var result = Math.Min(arg1.val.asDouble, arg2.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Clamp(Vm vm)
        {
            var arg1 = vm.GetArg(1);
            var arg2 = vm.GetArg(2);
            var arg3 = vm.GetArg(3);
            var result = Math.Min(Math.Max(arg1.val.asDouble, arg2.val.asDouble), arg3.val.asDouble);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult MoveTowards(Vm vm)
        {
            var from = vm.GetArg(1).val.asDouble;
            var to = vm.GetArg(2).val.asDouble;
            var step = vm.GetArg(3).val.asDouble;
            var delta = to - from;
            var result = to;
            if (Math.Abs(delta) > step)
            {
                result = from + Math.Sign(delta) * step;
            }
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Lerp(Vm vm)
        {
            var a = vm.GetArg(1).val.asDouble;
            var b = vm.GetArg(2).val.asDouble;
            var p = vm.GetArg(3).val.asDouble;
            var result = a + (b - a) * p;
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        public static NativeCallResult Dampen(Vm vm)
        {
            var curr = vm.GetArg(1).val.asDouble;
            var target = vm.GetArg(2).val.asDouble;
            var h = vm.GetArg(3).val.asDouble;
            var dt = vm.GetArg(4).val.asDouble;
            var t = 1 - Math.Exp(-dt / h);

            var result = curr + (target - curr) * t;

            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        public static NativeCallResult CalcDampenHalflife(Vm vm)
        {
            var timeSpan = vm.GetArg(1).val.asDouble;
            var precision = vm.GetArg(2).val.asDouble;
            var result = -timeSpan / Math.Log(precision, 2);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        public static NativeCallResult Remap(Vm vm)
        {
            var value = vm.GetArg(1).val.asDouble;
            var from1 = vm.GetArg(2).val.asDouble;
            var to1 = vm.GetArg(3).val.asDouble;
            var from2 = vm.GetArg(4).val.asDouble;
            var to2 = vm.GetArg(5).val.asDouble;

            var result = from2 + (value - from1) * (to2 - from2) / (to1 - from1);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        public static NativeCallResult RemapClamp(Vm vm)
        {
            var value = vm.GetArg(1).val.asDouble;
            var from1 = vm.GetArg(2).val.asDouble;
            var to1 = vm.GetArg(3).val.asDouble;
            var from2 = vm.GetArg(4).val.asDouble;
            var to2 = vm.GetArg(5).val.asDouble;

            var result = from2 + (value - from1) * (to2 - from2) / (to1 - from1);
            result = Math.Min(Math.Max(result, from2), to2);
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }
    }
}