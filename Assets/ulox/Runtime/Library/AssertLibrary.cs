namespace ULox
{
    public class AssertLibrary : ILoxByteCodeLibrary
    {
        public Table GetBindings()
        {
            var resTable = new Table();
            var assertInst = new InstanceInternal();
            resTable.Add("Assert", Value.New(assertInst));

            assertInst.fields[nameof(AreEqual)] = Value.New(AreEqual);
            assertInst.fields[nameof(AreNotEqual)] = Value.New(AreNotEqual);
            assertInst.fields[nameof(AreApproxEqual)] = Value.New(AreApproxEqual);

            return resTable;
        }

        private static Value AreApproxEqual(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if(lhs.type != ValueType.Double || rhs.type != ValueType.Double)
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
            if (!VM.VMValueCompare(ref lhs, ref rhs))
                throw new AssertException($"'{lhs}' does not equal '{rhs}'.");

            return Value.Null();
        }

        private static Value AreNotEqual(VMBase vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (VM.VMValueCompare(ref lhs, ref rhs))
                throw new AssertException($"'{lhs}' does not NOT equal '{rhs}'.");

            return Value.Null();
        }
    }
}
