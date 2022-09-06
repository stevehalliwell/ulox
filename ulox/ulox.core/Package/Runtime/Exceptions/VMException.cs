namespace ULox
{
    //TODO rename uloxruntimeexception
    public class VMException : UloxException
    {
        public VMException(string msg) : base(msg)
        {
        }
    }
}
