using System;

namespace ULox
{
    public partial class VmLibrary : IULoxLibrary
    {
        public string Name => nameof(VmLibrary);
        public VmLibrary(Func<VMBase> createVM)
        {
            CreateVM = createVM;
        }

        public Func<VMBase> CreateVM { get; private set; }

        public Table GetBindings()
        {
            var resTable = new Table();
            resTable.Add("VM", Value.New(new VMClass(CreateVM)));

            return resTable;
        }
    }
}
