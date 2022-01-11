using System;

namespace ULox
{
    public class AssertLibrary : IULoxLibrary
    {
        private const double SquareDividedTolerance = 0.01d;

        public string Name => nameof(AssertLibrary);

        public AssertLibrary(Func<VMBase> createVM)
        {
            CreateVM = createVM;
        }

        public Func<VMBase> CreateVM { get; private set; }

        public Table GetBindings()
        {
            var resTable = new Table();
            var assertInst = new InstanceInternal();
            resTable.Add(new HashedString("Assert"), Value.New(assertInst));

            assertInst.AddFieldsToInstance(
                (nameof(AreEqual), Value.New(AreEqual)),
                (nameof(AreNotEqual), Value.New(AreNotEqual)),
                (nameof(AreApproxEqual), Value.New(AreApproxEqual)),
                (nameof(IsTrue), Value.New(IsTrue)),
                (nameof(IsFalse), Value.New(IsFalse)),
                (nameof(IsNull), Value.New(IsNull)),
                (nameof(IsNotNull), Value.New(IsNotNull)),
                (nameof(DoesContain), Value.New(DoesContain)),
                (nameof(DoesNotContain), Value.New(DoesNotContain)),
                (nameof(Throws), Value.New(Throws)),
                (nameof(Pass), Value.New(Pass)),
                (nameof(Fail), Value.New(Fail))
                                          );

            return resTable;
        }

        private static NativeCallResult AreApproxEqual(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (lhs.type != ValueType.Double || rhs.type != ValueType.Double)
                throw new AssertException($"Cannot perform AreApproxEqual on non-double types, '{lhs}', '{rhs}'.");

            var lhsd = lhs.val.asDouble;
            var rhsd = rhs.val.asDouble;
            var dif = lhsd - rhsd;
            var squareDif = dif * dif;
            var largerSquare = Math.Max(lhsd * lhsd, rhsd * rhsd);
            var difsqOverLargersq = squareDif / largerSquare;
            if (difsqOverLargersq > SquareDividedTolerance)
                throw new AssertException($"'{lhs}' and '{rhs}' are '{dif}' apart. " +
                    $"Expect diff of squres to be less than '{SquareDividedTolerance}' " +
                    $"but '{squareDif}' and '{largerSquare}' are greater '{difsqOverLargersq}'.");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult AreEqual(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (!lhs.Compare(ref lhs, ref rhs))
                throw new AssertException($"'{lhs}' does not equal '{rhs}'.");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult AreNotEqual(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (lhs.Compare(ref lhs, ref rhs))
                throw new AssertException($"'{lhs}' does not NOT equal '{rhs}'.");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult IsTrue(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            if (lhs.IsFalsey)
                throw new AssertException($"'{lhs}' is not truthy.");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult IsFalse(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            if (!lhs.IsFalsey)
                throw new AssertException($"'{lhs}' is not falsy.");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult IsNull(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            if (!lhs.IsNull)
                throw new AssertException($"'{lhs}' is not null.");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult IsNotNull(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            if (lhs.IsNull)
                throw new AssertException($"'{lhs}' is null.");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult DoesContain(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (!rhs.val.asString.String.Contains(lhs.val.asString.String))
                throw new AssertException($"'{rhs}' did not contain '{lhs}'.");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult DoesNotContain(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (rhs.val.asString.String.Contains(lhs.val.asString.String))
                throw new AssertException($"'{rhs}' did contain '{lhs}', should not have.");

            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Throws(VMBase vm, int argCount)
        {
            var toRun = vm.GetArg(1).val.asClosure.chunk;
            if (toRun == null)
                throw new AssertException($"Requires 1 closure param to execute, but was not given one.");
            var ourVM = CreateVM();
            bool didThrow = false;
            try
            {
                ourVM.Interpret(toRun);
            }
            catch (Exception e)
            {
                didThrow = true;
            }

            if (!didThrow)
                throw new AssertException($"'{toRun.Name}' did not throw, but should have.");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Pass(VMBase vm, int argCount)
        {
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Fail(VMBase vm, int argCount)
        {
            var msg = vm.GetArg(1);
            throw new AssertException($"Fail. '{msg}'");
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
