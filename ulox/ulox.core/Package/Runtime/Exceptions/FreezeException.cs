namespace ULox
{
    //TODO remove, no need for this freezing is core vm
    public class FreezeException : VMException
    {
        public FreezeException(string message) : base(message)
        {
        }
    }
}
