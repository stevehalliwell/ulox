namespace ULox
{
    public interface IULoxLibrary
    {
        public string Name { get; }

        Table GetBindings();
    }
}
