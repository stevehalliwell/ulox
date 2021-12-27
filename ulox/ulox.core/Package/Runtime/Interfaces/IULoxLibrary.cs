namespace ULox
{
    public interface IULoxLibrary
    {
        string Name { get; }

        Table GetBindings();
    }
}
