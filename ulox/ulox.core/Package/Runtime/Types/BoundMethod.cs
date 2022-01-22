namespace ULox
{
    public class BoundMethod
    {
        public BoundMethod(
            Value receiver,
            ClosureInternal method)
        {
            Receiver = receiver;
            Method = method;
        }

        public Value Receiver { get; private set; }
        public ClosureInternal Method { get; private set; }
    }
}
