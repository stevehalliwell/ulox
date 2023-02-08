using System;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class StdLibrary : IULoxLibrary
    {
        private const double SquareDividedTolerance = 0.01d;
        
        public Func<Vm> CreateVM { get; }

        public StdLibrary()
        {
            CreateVM = () => new Vm();
        }

        public Table GetBindings()
        {
            return this.GenerateBindingTable(
                ("VM", Value.New(new VMClass(CreateVM))),
                ("Assert", Value.New(MakeAssertInstance())),
                ("Serialise", Value.New(SerialiseStdLibrary.MakeSerialiseInstance())),
                ("DI", Value.New(DIStdLibrary.MakeDIInstance())),
                ("Math", Value.New(MathStdLibrary.MakeMathInstance())),
                (nameof(Duplicate), Value.New(Duplicate, 1, 1)),
                (nameof(str), Value.New(str, 1, 1)),
                (nameof(IsFrozen), Value.New(IsFrozen, 1, 1)),
                (nameof(Unfreeze), Value.New(Unfreeze, 1, 1)),
                (nameof(GenerateStackDump), Value.New(GenerateStackDump, 1, 0)),
                (nameof(GenerateGlobalsDump), Value.New(GenerateGlobalsDump, 1, 0)),
                (nameof(GenerateReturnDump), Value.New(GenerateReturnDump, 1, 0))
                                            );
        }

        internal InstanceInternal MakeAssertInstance()
        {
            var assertInst = new InstanceInternal();
            assertInst.AddFieldsToInstance(
                (nameof(AreEqual), Value.New(AreEqual,1,2)),
                (nameof(AreNotEqual), Value.New(AreNotEqual, 1, 2)),
                (nameof(AreApproxEqual), Value.New(AreApproxEqual, 1, 2)),
                (nameof(IsTrue), Value.New(IsTrue, 1, 1)),
                (nameof(IsFalse), Value.New(IsFalse, 1, 1)),
                (nameof(IsNull), Value.New(IsNull, 1, 1)),
                (nameof(IsNotNull), Value.New(IsNotNull, 1, 1)),
                (nameof(DoesContain), Value.New(DoesContain, 1, 2)),
                (nameof(DoesNotContain), Value.New(DoesNotContain, 1, 2)),
                (nameof(Throws), Value.New(Throws, 1, 1)),
                (nameof(Pass), Value.New(Pass, 1, 0)),
                (nameof(Fail), Value.New(Fail, 1, 0)));
            assertInst.Freeze();
            return assertInst;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult AreApproxEqual(Vm vm)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (lhs.type != ValueType.Double || rhs.type != ValueType.Double)
                vm.ThrowRuntimeException($"Cannot perform AreApproxEqual on non-double types, '{lhs}', '{rhs}'");

            var lhsd = lhs.val.asDouble;
            var rhsd = rhs.val.asDouble;
            var dif = lhsd - rhsd;
            var squareDif = dif * dif;
            var largerSquare = Math.Max(lhsd * lhsd, rhsd * rhsd);
            var difsqOverLargersq = squareDif / largerSquare;
            if (difsqOverLargersq > SquareDividedTolerance)
                vm.ThrowRuntimeException($"'{lhs}' and '{rhs}' are '{dif}' apart. " +
                    $"Expect diff of squres to be less than '{SquareDividedTolerance}' " +
                    $"but '{squareDif}' and '{largerSquare}' are greater '{difsqOverLargersq}'");

            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult AreEqual(Vm vm)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (!Value.Compare(ref lhs, ref rhs))
                vm.ThrowRuntimeException($"'{lhs}' does not equal '{rhs}'");

            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult AreNotEqual(Vm vm)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (Value.Compare(ref lhs, ref rhs))
                vm.ThrowRuntimeException($"'{lhs}' does not NOT equal '{rhs}'");

            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult IsTrue(Vm vm)
        {
            var lhs = vm.GetArg(1);
            if (lhs.IsFalsey())
                vm.ThrowRuntimeException($"'{lhs}' is not truthy");

            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult IsFalse(Vm vm)
        {
            var lhs = vm.GetArg(1);
            if (!lhs.IsFalsey())
                vm.ThrowRuntimeException($"'{lhs}' is not falsy");

            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult IsNull(Vm vm)
        {
            var lhs = vm.GetArg(1);
            if (!lhs.IsNull())
                vm.ThrowRuntimeException($"'{lhs}' is not null");

            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult IsNotNull(Vm vm)
        {
            var lhs = vm.GetArg(1);
            if (lhs.IsNull())
                vm.ThrowRuntimeException($"'{lhs}' is null");

            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult DoesContain(Vm vm)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (!rhs.val.asString.String.Contains(lhs.val.asString.String))
                vm.ThrowRuntimeException($"'{rhs}' did not contain '{lhs}'");

            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult DoesNotContain(Vm vm)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (rhs.val.asString.String.Contains(lhs.val.asString.String))
                vm.ThrowRuntimeException($"'{rhs}' did contain '{lhs}', should not have");

            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private NativeCallResult Throws(Vm vm)
        {
            var toRun = vm.GetArg(1).val.asClosure.chunk;
            if (toRun == null)
                vm.ThrowRuntimeException($"Requires 1 closure param to execute, but was not given one");
            var ourVM = CreateVM();
            ourVM.CopyFrom(vm);
            bool didThrow = false;
            try
            {
                ourVM.Interpret(toRun);
            }
            catch (Exception)
            {
                didThrow = true;
            }

            if (!didThrow)
                vm.ThrowRuntimeException($"'{toRun.Name}' did not throw, but should have");

            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Pass(Vm vm)
        {
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeCallResult Fail(Vm vm)
        {
            var msg = vm.GetArg(1);
            vm.ThrowRuntimeException($"Fail. '{msg}'");
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeCallResult GenerateStackDump(Vm vm)
        {
            vm.SetNativeReturn(0, Value.New(VmUtil.GenerateValueStackDump(vm)));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeCallResult GenerateGlobalsDump(Vm vm)
        {
            vm.SetNativeReturn(0, Value.New(VmUtil.GenerateGlobalsDump(vm)));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeCallResult GenerateReturnDump(Vm vm)
        {
            vm.SetNativeReturn(0, Value.New(VmUtil.GenerateReturnDump(vm)));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeCallResult IsFrozen(Vm vm)
        {
            var target = vm.GetArg(1);
            if (target.type == ValueType.Instance)
                vm.SetNativeReturn(0, Value.New(target.val.asInstance.IsFrozen));
            else if (target.type == ValueType.UserType)
                vm.SetNativeReturn(0, Value.New(target.val.asClass.IsFrozen));

            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeCallResult Unfreeze(Vm vm)
        {
            var target = vm.GetArg(1);
            if (target.type == ValueType.Instance)
                target.val.asInstance.Unfreeze();
            if (target.type == ValueType.UserType)
                target.val.asClass.Unfreeze();

            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeCallResult str(Vm vm)
        {
            var v = vm.GetArg(1);
            vm.SetNativeReturn(0, Value.New(v.str()));
            return NativeCallResult.SuccessfulExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeCallResult Duplicate(Vm vm)
        {
            vm.SetNativeReturn(0, Value.Copy(vm.GetArg(1)));
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
