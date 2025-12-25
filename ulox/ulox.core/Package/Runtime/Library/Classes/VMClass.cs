namespace ULox
{
    public sealed class VMClass : UserTypeInternal
    {
        private static readonly HashedString VMFieldName = new("vm");

        public VMClass() : base(new HashedString("VM"), UserType.Native)
        {
            this.AddMethodsToClass(
                (ClassTypeCompilette.InitMethodName.String, Value.New(InitInstance, 1, 0)),
                (nameof(AddGlobal), Value.New(AddGlobal, 1, 2)),
                (nameof(GetGlobal), Value.New(GetGlobal, 1, 1)),
                (nameof(Start), Value.New(Start, 1, 1)),
                (nameof(InheritFromEnclosing), Value.New(InheritFromEnclosing, 1, 0)),
                (nameof(CopyBackToEnclosing), Value.New(CopyBackToEnclosing, 1, 0)),
                (nameof(Resume), Value.New(Resume, 1, 0)),
                (nameof(GenerateStackDump), Value.New(GenerateStackDump, 1, 0)),
                (nameof(GenerateGlobalsDump), Value.New(GenerateGlobalsDump, 1, 0))
                );
            AddFieldName(VMFieldName);
        }

        private NativeCallResult InitInstance(Vm vm)
        {
            var inst = vm.GetArg(0);
            var newVm = new Vm();
            newVm.Engine = vm.Engine;
            inst.val.asInstance.SetField(VMFieldName, Value.Object(newVm));
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

        public static NativeCallResult GenerateStackDump(Vm vm)
        {
            vm.SetNativeReturn(0, Value.New(VmUtil.GenerateValueStackDump(vm)));
            return NativeCallResult.SuccessfulExpression;
        }

        public static NativeCallResult GenerateGlobalsDump(Vm vm)
        {
            vm.SetNativeReturn(0, Value.New(VmUtil.GenerateGlobalsDump(vm)));
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
