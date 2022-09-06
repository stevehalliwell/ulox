namespace ULox
{
    public class CompilerException : UloxException
    {
        //TODO add demand for prev token and next token
        public CompilerException(string msg)
            : base(msg)
        {
        }
    }
}
