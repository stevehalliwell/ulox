namespace ULox
{
    public sealed class CompilerMessage
    {
        public CompilerMessage(string message)
        {
            Message = message;
        }

        public string Message { get; }

        public override string ToString()
        {
            return Message;
        }
    }
}
