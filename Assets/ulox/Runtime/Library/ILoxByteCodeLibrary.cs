namespace ULox
{
    public interface ILoxByteCodeLibrary
    {
        //TODO: Rather than get binginds, give it info so it can do the binding itself
        Table GetBindings();
    }
}
