namespace ULox
{
    public class PanicException : RuntimeUloxException
    {
        public PanicException(string message, int currentInstruction, string locationName, string valueStack, string callStack)
            : base(message, currentInstruction, locationName, valueStack, callStack)
        {
        }
    }
}
