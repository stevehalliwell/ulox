namespace ULox
{
    public sealed class TokenIterator
    {
        public Token CurrentToken { get; private set; }
        public Token PreviousToken { get; private set; }
        public string SourceName => _script.Name;
        public Scanner Scanner => _scanner;

        private readonly Scanner _scanner;
        private readonly Script _script;
        
        public TokenIterator(Scanner scanner, Script script)
        {
            _scanner = scanner;
            _script = script;
        }

        public string GetSourceSection(int start, int end)
        {
            return _scanner.GetSourceSection(start, end);
        }

        public void Advance()
        {
            PreviousToken = CurrentToken;
            CurrentToken = _scanner.Next();
        }

        public void Consume(TokenType tokenType, string msg)
        {
            if (CurrentToken.TokenType == tokenType)
                Advance();
            else
                throw new CompilerException(msg, PreviousToken, $"source '{_script.Name}'");
        }

        public bool Check(TokenType type)
            => CurrentToken.TokenType == type;

        public bool Match(TokenType type)
        {
            if (!Check(type))
                return false;
            Advance();
            return true;
        }

        public bool MatchAny(params TokenType[] type)
        {
            for (int i = 0; i < type.Length; i++)
            {
                if (!Check(type[i])) continue;

                Advance();
                return true;
            }
            return false;
        }
    }
}
