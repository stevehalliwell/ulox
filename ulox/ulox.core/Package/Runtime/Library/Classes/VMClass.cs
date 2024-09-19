using System;

namespace ULox
{
    public sealed class VMClass : UserTypeInternal
    {
        private static readonly HashedString VMFieldName = new("vm");

        public Func<Vm> CreateVM { get; }

        public VMClass(Func<Vm> createVM) : base(new HashedString("VM"), UserType.Native)
        {
            CreateVM = createVM;
            this.AddMethodsToClass(
                (ClassTypeCompilette.InitMethodName.String, Value.New(InitInstance, 1, 0)),
                (nameof(AddGlobal), Value.New(AddGlobal, 1, 2)),
                (nameof(GetGlobal), Value.New(GetGlobal, 1, 1)),
                (nameof(Start), Value.New(Start, 1, 1)),
                (nameof(InheritFromEnclosing), Value.New(InheritFromEnclosing, 1, 0)),
                (nameof(CopyBackToEnclosing), Value.New(CopyBackToEnclosing, 1, 0)),
                (nameof(Resume), Value.New(Resume, 1, 0))
                                  );
            AddFieldName(VMFieldName);
        }

        private NativeCallResult InitInstance(Vm vm)
        {
            var inst = vm.GetArg(0);
            inst.val.asInstance.SetField(VMFieldName, Value.Object(CreateVM()));
            vm.SetNativeReturn(0, inst);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult AddGlobal(Vm vm)
        {
            Vm ourVM = GetArg0Vm(vm);
            var name = vm.GetArg(1).val.asString;
            var val = vm.GetArg(2);
            ourVM.Globals.AddOrSet(name, val);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult GetGlobal(Vm vm)
        {
            Vm ourVM = GetArg0Vm(vm);
            var name = vm.GetArg(1).val.asString;
            ourVM.Globals.Get(name, out var found);
            vm.SetNativeReturn(0, found);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult InheritFromEnclosing(Vm vm)
        {
            Vm ourVM = GetArg0Vm(vm);
            ourVM.CopyFrom(vm);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult CopyBackToEnclosing(Vm vm)
        {
            Vm ourVM = GetArg0Vm(vm);
            vm.CopyFrom(ourVM);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Start(Vm vm)
        {
            Vm ourVM = GetArg0Vm(vm);
            var chunk = vm.GetArg(1).val.asClosure.chunk;
            ourVM.Interpret(chunk);
            vm.SetNativeReturn(0, ourVM.ValueStack.Peek());
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Resume(Vm vm)
        {
            Vm ourVM = GetArg0Vm(vm);
            ourVM.Run();
            vm.SetNativeReturn(0, ourVM.ValueStack.Peek());
            return NativeCallResult.SuccessfulExpression;
        }

        private static Vm GetArg0Vm(Vm vm)
        {
            var instVal = vm.GetArg(0);
            var inst = instVal.val.asInstance;

            inst.Fields.Get(VMFieldName, out var found);
            return found.val.asObject as Vm;
        }
    }
}
