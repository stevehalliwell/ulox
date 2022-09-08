namespace ULox
{
    public class PanicException : RuntimeUloxException
    {
        public PanicException(string message, int currentInstruction, string locationName, int line, string valueStack, string callStack)
            : base(message, currentInstruction, locationName, line, valueStack, callStack)
        {
        }
    }
}
    