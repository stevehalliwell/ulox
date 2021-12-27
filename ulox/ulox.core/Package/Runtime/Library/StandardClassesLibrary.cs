namespace ULox
{
    public class StandardClassesLibrary : IULoxLibrary
    {
        public string Name => nameof(StandardClassesLibrary);

        public Table GetBindings()
            => this.GenerateBindingTable(
                ("List", Value.New(new ListClass())),
                ("Map", Value.New(new MapClass())),
                ("Dynamic", Value.New(new DynamicClass()))
                                        );
    }
}
