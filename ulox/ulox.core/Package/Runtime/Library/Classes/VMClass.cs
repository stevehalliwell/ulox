using System;

namespace ULox
{
    public sealed class VMClass : UserTypeInternal
    {
        private static readonly HashedString VMFieldName = new HashedString("vm");

        public Func<Vm> CreateVM { get; }

        public VMClass(Func<Vm> createVM) : base(new HashedString("VM"), UserType.Native)
        {
            CreateVM = createVM;
            this.AddMethod(TypeCompilette.InitMethodName, Value.New(InitInstance), null);
            this.AddMethodsToClass(
                (nameof(AddGlobal), Value.New(AddGlobal)),
                (nameof(GetGlobal), Value.New(GetGlobal)),
                (nameof(Start), Value.New(Start)),
                (nameof(InheritFromEnclosing), Value.New(InheritFromEnclosing)),
                (nameof(CopyBackToEnclosing), Value.New(CopyBackToEnclosing)),
                (nameof(Resume), Value.New(Resume))
                                  );
        }

        private NativeCallResult InitInstance(Vm vm, int argCount)
        {
            var inst = vm.GetArg(0);
            inst.val.asInstance.SetField(VMFieldName, Value.Object(CreateVM()));
            vm.SetNativeReturn(0, inst);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult AddGlobal(Vm vm, int argCount)
        {
            Vm ourVM = GetArg0Vm(vm);
            var name = vm.GetArg(1).val.asString;
            var val = vm.GetArg(2);
            ourVM.SetGlobal(name, val);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult GetGlobal(Vm vm, int argCount)
        {
            Vm ourVM = GetArg0Vm(vm);
            var name = vm.GetArg(1).val.asString;
            vm.SetNativeReturn(0, ourVM.GetGlobal(name));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult InheritFromEnclosing(Vm vm, int argCount)
        {
            Vm ourVM = GetArg0Vm(vm);
            ourVM.CopyFrom(vm);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult CopyBackToEnclosing(Vm vm, int argCount)
        {
            Vm ourVM = GetArg0Vm(vm);
            vm.CopyFrom(ourVM);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Start(Vm vm, int argCount)
        {
            Vm ourVM = GetArg0Vm(vm);
            var chunk = vm.GetArg(1).val.asClosure.chunk;
            ourVM.Interpret(chunk);
            vm.SetNativeReturn(0, ourVM.StackTop);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Resume(Vm vm, int argCount)
        {
            Vm ourVM = GetArg0Vm(vm);
            ourVM.Run();
            vm.SetNativeReturn(0, ourVM.StackTop);
            return NativeCallResult.SuccessfulExpression;
        }

        private static Vm GetArg0Vm(Vm vm)
        {
            var instVal = vm.GetArg(0);
            var inst = instVal.val.asInstance;

            return inst.Fields[VMFieldName].val.asObject as Vm;
        }
    }
}
