using System.Collections.Generic;

namespace ULox
{
    //todo add tests
    public class TokenIterator
    {
        public Token CurrentToken { get; private set; }
        public Token PreviousToken { get; private set; }
        private readonly List<Token> _tokens;

        public string SourceName { get; }

        private int tokenIndex;

        public TokenIterator(List<Token> tokens, string sourceName)
        {
            _tokens = tokens;
            SourceName = sourceName;
        }

        public void Advance()
        {
            PreviousToken = CurrentToken;
            CurrentToken = _tokens[tokenIndex];
            tokenIndex++;
        }

        public void Consume(TokenType tokenType, string msg)
        {
            if (CurrentToken.TokenType == tokenType)
                Advance();
            else
                throw new CompilerException(msg, PreviousToken, $"source '{SourceName}'");
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
