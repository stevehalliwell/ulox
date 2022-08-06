using System;
using System.IO;

namespace ULox
{
    public class StdLibrary : IULoxLibrary
    {
        private const double SquareDividedTolerance = 0.01d;

        public string Name => nameof(StdLibrary);

        public StdLibrary()
        {
            CreateVM = () => new Vm();
        }

        public Func<Vm> CreateVM { get; private set; }

        public Table GetBindings()
        {
            var assertInst = new InstanceInternal();
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
                (nameof(Fail), Value.New(Fail)));
            assertInst.Freeze();

            var serialiseInst = new InstanceInternal();
            serialiseInst.AddFieldsToInstance(
                (nameof(ToJson), Value.New(ToJson)),
                (nameof(FromJson), Value.New(FromJson)));
            serialiseInst.Freeze();

            var diLibInst = new InstanceInternal();
            diLibInst.AddFieldsToInstance(
                (nameof(Count), Value.New(Count)),
                (nameof(GenerateDump), Value.New(GenerateDump)),
                (nameof(Freeze), Value.New(Freeze)));
            diLibInst.Freeze();

            return this.GenerateBindingTable(
                ("VM", Value.New(new VMClass(CreateVM))),
                ("Assert", Value.New(assertInst)),
                ("Serialise", Value.New(serialiseInst)),
                ("DI", Value.New(diLibInst)),
                (nameof(Duplicate), Value.New(Duplicate)),
                (nameof(str), Value.New(str)),
                (nameof(IsFrozen), Value.New(IsFrozen)),
                (nameof(Unfreeze), Value.New(Unfreeze)),
                (nameof(GenerateStackDump), Value.New(GenerateStackDump)),
                (nameof(GenerateGlobalsDump), Value.New(GenerateGlobalsDump)),
                (nameof(GenerateReturnDump), Value.New(GenerateReturnDump))
                                            );
        }

        public NativeCallResult GenerateStackDump(Vm vm, int argCount)
        {
            vm.PushReturn(Value.New(vm.GenerateStackDump()));
            return NativeCallResult.SuccessfulExpression;
        }

        public NativeCallResult GenerateGlobalsDump(Vm vm, int argCount)
        {
            vm.PushReturn(Value.New(vm.GenerateGlobalsDump()));
            return NativeCallResult.SuccessfulExpression;
        }

        public NativeCallResult GenerateReturnDump(Vm vm, int argCount)
        {
            vm.PushReturn(Value.New(vm.GenerateReturnDump()));
            return NativeCallResult.SuccessfulExpression;
        }

        public NativeCallResult IsFrozen(Vm vm, int argCount)
        {
            var target = vm.GetArg(1);
            if (target.type == ValueType.Instance)
                vm.PushReturn(Value.New(target.val.asInstance.IsFrozen));
            else if (target.type == ValueType.Class)
                vm.PushReturn(Value.New(target.val.asClass.IsFrozen));

            return NativeCallResult.SuccessfulExpression;
        }

        public NativeCallResult Unfreeze(Vm vm, int argCount)
        {
            var target = vm.GetArg(1);
            if (target.type == ValueType.Instance)
                target.val.asInstance.Unfreeze();
            if (target.type == ValueType.Class)
                target.val.asClass.Unfreeze();

            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Count(Vm vm, int argCount)
        {
            var di = FromVm(vm);
            vm.PushReturn(Value.New(di.Count));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult GenerateDump(Vm vm, int argCount)
        {
            var di = FromVm(vm);
            vm.PushReturn(Value.New(di.GenerateDump()));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Freeze(Vm vm, int argCount)
        {
            var di = FromVm(vm);
            di.Freeze();
            return NativeCallResult.SuccessfulExpression;
        }

        private DiContainer FromVm(Vm vMBase)
            => vMBase.DiContainer;

        private static NativeCallResult ToJson(Vm vm, int argCount)
        {
            var obj = vm.GetArg(1);
            var jsonWriter = new JsonValueHeirarchyWriter();
            var walker = new ValueHeirarchyWalker(jsonWriter);
            walker.Walk(obj);
            var result = jsonWriter.GetString();
            vm.PushReturn(Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult FromJson(Vm vm, int argCount)
        {
            var jsonString = vm.GetArg(1);
            var reader = new StringReader(jsonString.val.asString.String);
            var creator = new JsonDocValueHeirarchyTraverser(new ValueObjectBuilder(ValueObjectBuilder.ObjectType.Object), reader);
            creator.Process();
            var obj = creator.Finish();
            vm.PushReturn(obj);
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult AreApproxEqual(Vm vm, int argCount)
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

        private static NativeCallResult AreEqual(Vm vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (!lhs.Compare(ref lhs, ref rhs))
                throw new AssertException($"'{lhs}' does not equal '{rhs}'.");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult AreNotEqual(Vm vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (lhs.Compare(ref lhs, ref rhs))
                throw new AssertException($"'{lhs}' does not NOT equal '{rhs}'.");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult IsTrue(Vm vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            if (lhs.IsFalsey)
                throw new AssertException($"'{lhs}' is not truthy.");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult IsFalse(Vm vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            if (!lhs.IsFalsey)
                throw new AssertException($"'{lhs}' is not falsy.");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult IsNull(Vm vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            if (!lhs.IsNull)
                throw new AssertException($"'{lhs}' is not null.");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult IsNotNull(Vm vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            if (lhs.IsNull)
                throw new AssertException($"'{lhs}' is null.");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult DoesContain(Vm vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (!rhs.val.asString.String.Contains(lhs.val.asString.String))
                throw new AssertException($"'{rhs}' did not contain '{lhs}'.");

            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult DoesNotContain(Vm vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (rhs.val.asString.String.Contains(lhs.val.asString.String))
                throw new AssertException($"'{rhs}' did contain '{lhs}', should not have.");

            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Throws(Vm vm, int argCount)
        {
            var toRun = vm.GetArg(1).val.asClosure.chunk;
            if (toRun == null)
                throw new AssertException($"Requires 1 closure param to execute, but was not given one.");
            var ourVM = CreateVM();
            ourVM.CopyFrom(vm);
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

        private static NativeCallResult Pass(Vm vm, int argCount)
        {
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Fail(Vm vm, int argCount)
        {
            var msg = vm.GetArg(1);
            throw new AssertException($"Fail. '{msg}'");
            return NativeCallResult.SuccessfulExpression;
        }

        public NativeCallResult str(Vm vm, int argCount)
        {
            var v = vm.GetArg(1);
            vm.PushReturn(Value.New(v.str()));
            return NativeCallResult.SuccessfulExpression;
        }

        public NativeCallResult Duplicate(Vm vm, int argCount)
        {
            vm.PushReturn(Value.Copy(vm.GetArg(1)));
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
