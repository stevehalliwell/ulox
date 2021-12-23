using System;

namespace ULox
{
    public class LoxException : Exception
    {
        public LoxException(string msg) : base(msg)
        {
        }

        public LoxException(TokenType tokenType, int line, int character, string msg)
            : base($"{tokenType}|{line}:{character} {msg}")
        { }
    }
}
