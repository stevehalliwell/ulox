namespace ULox
{
    public class ScannerException : UloxException
    {
        public ScannerException(TokenType tokenType, int line, int character, string msg)
            : base($"{tokenType}|{line}:{character} {msg}") { }
    }
}
