namespace ULox
{
    public class StandardClassesLibrary : IULoxLibrary
    {
        public string Name => nameof(StandardClassesLibrary);

        public Table GetBindings()
            => this.GenerateBindingTable(
                ("Map", Value.New(new MapClass())),
                ("Dynamic", Value.New(new DynamicClass()))
                                        );
    }
}
