using System;

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
            => this.GenerateBindingTable(
                ("VM", Value.New(new VMClass())),
                ("Assert", Value.New(MakeAssertInstance())),
                ("Serialise", Value.New(SerialiseStdLibrary.MakeInstance())),
                ("Math", Value.New(MathStdLibrary.MakeInstance())),
                ("Platform", Value.New(PlatformStdLibrary.MakeInstance())),
                ("String", Value.New(StringStdLibrary.MakeInstance())),
                ("List", NativeListClass.SharedNativeListClassValue),
                ("Map", NativeMapClass.SharedNativeMapClassValue),
                ("Dynamic", DynamicClass.SharedDynamicClassValue),
                ("Object", Value.New(ObjectLibrary.MakeInstance())),
                (nameof(str), Value.New(str, 1, 1)),
                (nameof(print), Value.New(print, 1, 1)),
                (nameof(printh), Value.New(printh, 1, 1))
                                        );

        public NativeCallResult print(Vm vm)
        {
            vm.Engine.Context.Platform.Print(vm.GetArg(1).ToString());
            return NativeCallResult.SuccessfulExpression;
        }

        public NativeCallResult printh(Vm vm)
        {
            var val = vm.GetArg(1);
            var valWriter = new StringBuilderValueHierarchyWriter();
            var objWalker = new ValueHierarchyWalker(valWriter);
            objWalker.Walk(val);
            vm.Engine.Context.Platform.Print(valWriter.GetString());
            return NativeCallResult.SuccessfulExpression;
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

        private static NativeCallResult AreEqual(Vm vm)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (!Value.Compare(ref lhs, ref rhs))
                vm.ThrowRuntimeException($"'{lhs}' does not equal '{rhs}'");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult AreNotEqual(Vm vm)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (Value.Compare(ref lhs, ref rhs))
                vm.ThrowRuntimeException($"'{lhs}' does not NOT equal '{rhs}'");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult IsTrue(Vm vm)
        {
            var lhs = vm.GetArg(1);
            if (lhs.IsFalsey())
                vm.ThrowRuntimeException($"'{lhs}' is not truthy");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult IsFalse(Vm vm)
        {
            var lhs = vm.GetArg(1);
            if (!lhs.IsFalsey())
                vm.ThrowRuntimeException($"'{lhs}' is not falsy");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult IsNull(Vm vm)
        {
            var lhs = vm.GetArg(1);
            if (!lhs.IsNull())
                vm.ThrowRuntimeException($"'{lhs}' is not null");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult IsNotNull(Vm vm)
        {
            var lhs = vm.GetArg(1);
            if (lhs.IsNull())
                vm.ThrowRuntimeException($"'{lhs}' is null");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult DoesContain(Vm vm)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (!rhs.val.asString.String.Contains(lhs.val.asString.String))
                vm.ThrowRuntimeException($"'{rhs}' did not contain '{lhs}'");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult DoesNotContain(Vm vm)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (rhs.val.asString.String.Contains(lhs.val.asString.String))
                vm.ThrowRuntimeException($"'{rhs}' did contain '{lhs}', should not have");

            return NativeCallResult.SuccessfulExpression;
        }

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
                vm.ThrowRuntimeException($"'{toRun.ChunkName}' did not throw, but should have");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Pass(Vm vm)
        {
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Fail(Vm vm)
        {
            var msg = vm.GetArg(1);
            vm.ThrowRuntimeException($"Fail. '{msg}'");
            return NativeCallResult.SuccessfulExpression;
        }

        public static NativeCallResult str(Vm vm)
        {
            var v = vm.GetArg(1);
            vm.SetNativeReturn(0, Value.New(v.ToString()));
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
