namespace ULox
{
    public class ScannerException : LoxException
    {
        public ScannerException(TokenType tokenType, int line, int character, string msg)
            : base(tokenType, line, character, msg) { }
    }
}
