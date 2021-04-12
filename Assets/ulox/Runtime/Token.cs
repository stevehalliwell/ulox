namespace ULox
{
    public readonly struct Token
    {
        public readonly TokenType TokenType;
        public readonly string Lexeme;
        public readonly object Literal;
        public readonly int Line;
        public readonly int Character;

        public Token(TokenType tokenType,
                     string lexeme,
                     object literal,
                     int line,
                     int character)
        {
            this.TokenType = tokenType;
            this.Lexeme = lexeme;
            this.Literal = literal;
            this.Line = line;
            this.Character = character;
        }
    }
}
