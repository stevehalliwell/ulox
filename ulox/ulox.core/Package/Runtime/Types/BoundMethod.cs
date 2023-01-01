namespace ULox
{
    public sealed class BoundMethod
    {
        public BoundMethod(
            Value receiver,
            ClosureInternal method)
        {
            Receiver = receiver;
            Method = method;
        }

        public Value Receiver { get; }
        public ClosureInternal Method { get; }
    }
}
