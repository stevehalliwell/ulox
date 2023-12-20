namespace ULox
{
    public readonly struct Token
    {
        public readonly TokenType TokenType;
        public readonly string Lexeme;
        public readonly object Literal;
        public readonly int Line;
        public readonly int Character;
        public readonly int StringSourceIndex;

        public Token(
            TokenType tokenType,
            string lexeme,
            object literal,
            int line,
            int character,
            int stringSourceIndex)
        {
            TokenType = tokenType;
            Lexeme = lexeme;
            Literal = literal;
            Line = line;
            Character = character;
            StringSourceIndex = stringSourceIndex;
        }

        public Token MutateType(TokenType newType)
        {
            return new Token(
                newType,
                Lexeme,
                Literal,
                Line,
                Character,
                StringSourceIndex);
        }

        public Token Mutate(TokenType tokenType, string lexeme, object literal)
        {
            return new Token(
                tokenType,
                lexeme,
                literal,
                Line,
                Character,
                StringSourceIndex);
        }
    }
}
