namespace ULox
{
    public class VMLibrary : ILoxByteCodeLibrary
    {
        public Table GetBindings()
        {
            var resTable = new Table();
            var vmLibInst = new InstanceInternal();
            resTable.Add("VM", Value.New(vmLibInst)); 

            vmLibInst.fields[nameof(CreateChildVMAndStart)] = Value.New(CreateChildVMAndStart);

            return resTable;
        }
        
        private static Value CreateChildVMAndStart(VM vm, int argCount)
        {
            var startFunc = vm.GetArg(1);

            var func = vm.FindFunctionWithArity(startFunc.val.asString,0);

            var newVM = new VM();
            vm.CopyGlobals(newVM);
            newVM.CallFunction(func, 0);

            return Value.Null();
        }
    }
}
