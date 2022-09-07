namespace ULox
{
    //TODO probably not needed, we only throw this type never expect to catch it
    public class AssertException : RuntimeUloxException
    {
        public AssertException(string msg) : base(msg)
        {
        }
    }
}
