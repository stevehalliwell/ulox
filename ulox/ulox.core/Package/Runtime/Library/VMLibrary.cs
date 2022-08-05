using System;

namespace ULox
{
    public class VmLibrary : IULoxLibrary
    {
        public string Name => nameof(VmLibrary);

        public VmLibrary(Func<Vm> createVM) => CreateVM = createVM;
        
        public VmLibrary() => CreateVM = () => new Vm();

        public Func<Vm> CreateVM { get; private set; }

        public Table GetBindings()
            => this.GenerateBindingTable(
                ("VM", Value.New(new VMClass(CreateVM)))
                                        );
    }
}
