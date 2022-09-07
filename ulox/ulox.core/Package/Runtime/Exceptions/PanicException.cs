namespace ULox
{
    public class PanicException : RuntimeUloxException
    {
        public PanicException(string message, int currentInstruction, string locationName)
            : base(message, currentInstruction, locationName)
        {
        }
    }
}
