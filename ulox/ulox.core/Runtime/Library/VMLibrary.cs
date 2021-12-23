using System;

namespace ULox
{
    public class VmLibrary : IULoxLibrary
    {
        public string Name => nameof(VmLibrary);

        public VmLibrary(Func<VMBase> createVM) => CreateVM = createVM;

        public Func<VMBase> CreateVM { get; private set; }

        public Table GetBindings()
            => this.GenerateBindingTable(
                ("VM", Value.New(new VMClass(CreateVM)))
                                        );
    }
}
