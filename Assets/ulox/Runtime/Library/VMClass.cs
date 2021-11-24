using System;

namespace ULox
{
    public class VMClass : ClassInternal
    {
        private static readonly HashedString VMFieldName = new HashedString("vm");

        public Func<VMBase> CreateVM { get; private set; }

        public VMClass(Func<VMBase> createVM) : base(new HashedString("VM"))
        {
            CreateVM = createVM;
            this.AddMethod(ClassCompilette.InitMethodName, Value.New(InitInstance));
            this.AddMethodsToClass(
                (nameof(AddGlobal), Value.New(AddGlobal)),
                (nameof(GetGlobal), Value.New(GetGlobal)),
                (nameof(Start), Value.New(Start)),
                (nameof(InheritFromEnclosing), Value.New(InheritFromEnclosing)),
                (nameof(CopyBackToEnclosing), Value.New(CopyBackToEnclosing)),
                (nameof(Resume), Value.New(Resume))
                );
        }

        private Value InitInstance(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            inst.val.asInstance.SetField(VMFieldName, Value.Object(CreateVM()));
            return inst;
        }

        private Value AddGlobal(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            Vm ourVM = GetArg0Vm(vm);
            var name = vm.GetArg(1).val.asString;
            var val = vm.GetArg(2);
            ourVM.SetGlobal(name, val);
            return inst;
        }

        private Value GetGlobal(VMBase vm, int argCount)
        {
            Vm ourVM = GetArg0Vm(vm);
            var name = vm.GetArg(1).val.asString;
            return ourVM.GetGlobal(name);
        }

        private Value InheritFromEnclosing(VMBase vm, int argCount)
        {
            Vm ourVM = GetArg0Vm(vm);
            ourVM.CopyFrom(vm);
            return Value.Null();
        }

        private Value CopyBackToEnclosing(VMBase vm, int argCount)
        {
            Vm ourVM = GetArg0Vm(vm);
            vm.CopyFrom(ourVM);
            return Value.Null();
        }

        private Value Start(VMBase vm, int argCount)
        {
            Vm ourVM = GetArg0Vm(vm);
            var chunk = vm.GetArg(1).val.asClosure.chunk;
            ourVM.Interpret(chunk);
            return ourVM.StackTop;
        }

        private Value Resume(VMBase vm, int argCount)
        {
            Vm ourVM = GetArg0Vm(vm);
            ourVM.Run();
            return ourVM.StackTop;
        }

        private Vm GetArg0Vm(VMBase vm)
        {
            var inst = vm.GetArg(0);
            var ourVM = inst.val.asInstance.GetField(VMFieldName).val.asObject as Vm;
            return ourVM;
        }
    }
}
