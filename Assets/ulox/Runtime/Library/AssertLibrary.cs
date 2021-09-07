using System;

namespace ULox
{
    public class AssertLibrary : IULoxLibrary
    {
        public AssertLibrary(Func<VMBase> createVM)
        {
            CreateVM = createVM;
        }

        public Func<VMBase> CreateVM { get; private set; }

        public Table GetBindings()
        {
            var resTable = new Table();
            var assertInst = new InstanceInternal();
            resTable.Add("Assert", Value.New(assertInst));

            assertInst.fields[nameof(AreEqual)] = Value.New(AreEqual);
            assertInst.fields[nameof(AreNotEqual)] = Value.New(AreNotEqual);
            assertInst.fields[nameof(AreApproxEqual)] = Value.New(AreApproxEqual);
            assertInst.fields[nameof(IsTrue)] = Value.New(IsTrue);
            assertInst.fields[nameof(IsFalse)] = Value.New(IsFalse);
            assertInst.fields[nameof(IsNull)] = Value.New(IsNull);
            assertInst.fields[nameof(IsNotNull)] = Value.New(IsNotNull);
            assertInst.fields[nameof(DoesContain)] = Value.New(DoesContain);
            assertInst.fields[nameof(DoesNotContain)] = Value.New(DoesNotContain);
            assertInst.fields[nameof(Throws)] = Value.New(Throws);

            return resTable;
        }

        private static Value AreApproxEqual(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (lhs.type != ValueType.Double || rhs.type != ValueType.Double)
                throw new AssertException($"Cannot perform AreApproxEqual on non-double types, '{lhs}', '{rhs}'.");

            var dif = lhs.val.asDouble - rhs.val.asDouble;
            var squareDif = dif * dif;
            if (squareDif > 1e-16)
                throw new AssertException($"'{lhs}' and '{rhs}' are '{dif}' apart.");

            return Value.Null();
        }

        private static Value AreEqual(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (!lhs.Compare(ref lhs, ref rhs))
                throw new AssertException($"'{lhs}' does not equal '{rhs}'.");

            return Value.Null();
        }

        private static Value AreNotEqual(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (lhs.Compare(ref lhs, ref rhs))
                throw new AssertException($"'{lhs}' does not NOT equal '{rhs}'.");

            return Value.Null();
        }

        private static Value IsTrue(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            if (lhs.IsFalsey)
                throw new AssertException($"'{lhs}' is not truthy.");

            return Value.Null();
        }

        private static Value IsFalse(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            if (!lhs.IsFalsey)
                throw new AssertException($"'{lhs}' is not falsy.");

            return Value.Null();
        }

        private static Value IsNull(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            if (!lhs.IsNull)
                throw new AssertException($"'{lhs}' is not null.");

            return Value.Null();
        }

        private static Value IsNotNull(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            if (lhs.IsNull)
                throw new AssertException($"'{lhs}' is null.");

            return Value.Null();
        }

        private static Value DoesContain(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (!rhs.val.asString.Contains(lhs.val.asString))
                throw new AssertException($"'{rhs}' did not contain '{lhs}'.");

            return Value.Null();
        }

        private static Value DoesNotContain(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (rhs.val.asString.Contains(lhs.val.asString))
                throw new AssertException($"'{rhs}' did contain '{lhs}', should not have.");

            return Value.Null();
        }

        private Value Throws(VMBase vm, int argCount)
        {
            var toRun = vm.GetArg(1).val.asClosure.chunk;
            if(toRun == null)
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

            return Value.Null();
        }
    }
}
