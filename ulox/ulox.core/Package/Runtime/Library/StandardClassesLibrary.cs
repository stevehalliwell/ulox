namespace ULox
{
    public class StandardClassesLibrary : IULoxLibrary
    {
        public string Name => nameof(StandardClassesLibrary);

        public Table GetBindings()
            => this.GenerateBindingTable(
                ("Dynamic", Value.New(new DynamicClass()))
                                        );
    }
}
