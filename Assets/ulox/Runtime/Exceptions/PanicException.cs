namespace ULox
{
    public class PanicException : System.Exception
    {
        public PanicException(string message = "") : base(message) { }
    }
}
