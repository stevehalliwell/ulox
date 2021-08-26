using System;

namespace ULox
{
    public class VMLibrary : ILoxByteCodeLibrary
    {
        public VMLibrary(Func<VMBase> createVM)
        {
            CreateVM = createVM;
        }

        public Func<VMBase> CreateVM { get; private set; }
        public class VMClass : ClassInternal
        {
            private readonly string VMFieldName = "vm";

            public Func<VMBase> CreateVM { get; private set; }

            public VMClass(Func<VMBase> createVM)
            {
                CreateVM = createVM;
                this.name = "VM";
                this.AddMethod(ClassCompilette.InitMethodName, Value.New(InitInstance));
                this.AddMethod(nameof(AddGlobal), Value.New(AddGlobal));
                this.AddMethod(nameof(GetGlobal), Value.New(GetGlobal));
                this.AddMethod(nameof(Start), Value.New(Start));
                this.AddMethod(nameof(InheritFromEnclosing), Value.New(InheritFromEnclosing));
                this.AddMethod(nameof(CopyBackToEnclosing), Value.New(CopyBackToEnclosing));
                this.AddMethod(nameof(Resume), Value.New(Resume));
            }

            private Value InitInstance(VMBase vm, int argCount)
            {
                var inst = vm.GetArg(0);
                inst.val.asInstance.fields.Add(VMFieldName, Value.Object(CreateVM()));
                return inst;
            }

            private Value AddGlobal(VMBase vm, int argCount)
            {
                var inst = vm.GetArg(0);
                var name = vm.GetArg(1).val.asString;
                var val = vm.GetArg(2);
                var ourVM = inst.val.asInstance.fields[VMFieldName].val.asObject as VM;
                ourVM.SetGlobal(name, val);
                return inst;
            }

            private Value GetGlobal(VMBase vm, int argCount)
            {
                var inst = vm.GetArg(0);
                var name = vm.GetArg(1).val.asString;
                var ourVM = inst.val.asInstance.fields[VMFieldName].val.asObject as VM;
                return ourVM.GetGlobal(name);
            }

            private Value InheritFromEnclosing(VMBase vm, int argCount)
            {
                var inst = vm.GetArg(0);
                var ourVM = inst.val.asInstance.fields[VMFieldName].val.asObject as VM;
                ourVM.CopyFrom(vm);
                return Value.Null();
            }

            private Value CopyBackToEnclosing(VMBase vm, int argCount)
            {
                var inst = vm.GetArg(0);
                var ourVM = inst.val.asInstance.fields[VMFieldName].val.asObject as VM;
                vm.CopyFrom(ourVM);
                return Value.Null();
            }

            private Value Start(VMBase vm, int argCount)
            {
                var inst = vm.GetArg(0);
                var ourVM = inst.val.asInstance.fields[VMFieldName].val.asObject as VM;
                var chunk = vm.GetArg(1).val.asClosure.chunk;
                ourVM.Interpret(chunk);
                return ourVM.StackTop;
            }

            private Value Resume(VMBase vm, int argCount)
            {
                var inst = vm.GetArg(0);
                var ourVM = inst.val.asInstance.fields[VMFieldName].val.asObject as VM;
                ourVM.Run();
                return ourVM.StackTop;
            }
        }

        public Table GetBindings()
        {
            var resTable = new Table();
            resTable.Add("VM", Value.New(new VMClass(CreateVM)));

            return resTable;
        }
    }
}
