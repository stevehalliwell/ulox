namespace ULox
{
    //TODO: Split and org in folders
    public class PanicException : System.Exception
    {
        public PanicException(string message = "") : base(message) { }
    }
 
    public class LoxException : System.Exception
    {
        public LoxException(string msg) : base(msg) { }

        public LoxException(TokenType tokenType, int line, int character, string msg)
            : base($"{tokenType}|{line}:{character} {msg}")
        { }
    }

    public class CompilerException : LoxException
    {
        public CompilerException(string msg) : base(msg) { }
    }

    public class VMException : LoxException
    {
        public VMException(string msg) : base(msg) { }
    }

    public class AssertException : LoxException
    {
        public AssertException(string msg) : base(msg) { }
    }

    public class ScannerException : LoxException
    {
        public ScannerException(TokenType tokenType, int line, int character, string msg)
            : base(tokenType, line, character, msg) { }
    }
}
