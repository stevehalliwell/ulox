namespace ULox
{
    public class PanicException : RuntimeUloxException
    {
        public PanicException(string message = "") : base(message)
        {
        }
    }
}
