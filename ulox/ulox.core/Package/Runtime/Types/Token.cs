namespace ULox
{
    public enum TokenType : byte
    {
        EOF,

        NONE,
        
        OPEN_PAREN,
        CLOSE_PAREN,
        OPEN_BRACE, //{
        CLOSE_BRACE, //}
        OPEN_BRACKET, //[
        CLOSE_BRACKET, //]
        COMMA,
        DOT,
        END_STATEMENT,

        MINUS,
        PLUS,
        SLASH,
        STAR,
        PERCENT,
        MINUS_EQUAL,
        PLUS_EQUAL,
        SLASH_EQUAL,
        STAR_EQUAL,
        PERCENT_EQUAL,

        ASSIGN,
        BANG,
        BANG_EQUAL,
        EQUALITY,
        GREATER,
        GREATER_EQUAL,
        LESS,
        LESS_EQUAL,

        COLON,

        IDENTIFIER,
        VAR,
        STRING,
        NUMBER,

        FUNCTION,

        AND,
        OR,
        IF,
        ELSE,
        WHILE,
        FOR,

        LOOP,
        RETURN,
        YIELD,

        BREAK,
        CONTINUE,
        TRUE,
        FALSE,
        NULL,

        PANIC,

        CLASS,
        THIS,
        STATIC,
        INIT,

        MIXIN,

        TEST_SET,
        TESTCASE,

        CONTEXT_NAME_CLASS,
        CONTEXT_NAME_FUNC,
        CONTEXT_NAME_TEST,
        CONTEXT_NAME_TESTSET,

        BUILD,

        TYPEOF,

        MEETS,
        SIGNS,

        COUNT_OF,

        EXPECT,

        MATCH,

        LABEL,
        GOTO,

        ENUM,

        READ_ONLY,

        SOA,
    }
    
    public readonly struct Token
    {
        public readonly string Literal;
        public readonly int StringSourceIndex;
        public readonly TokenType TokenType;

        public Token(
            TokenType tokenType,
            string literal,
            int stringSourceIndex)
        {
            TokenType = tokenType;
            Literal = literal;
            StringSourceIndex = stringSourceIndex;
        }

        public Token MutateType(TokenType newType)
        {
            return new Token(
                newType,
                Literal,
                StringSourceIndex);
        }

        public Token Mutate(TokenType tokenType, string literal)
        {
            return new Token(
                tokenType,
                literal,
                StringSourceIndex);
        }
    }
}
