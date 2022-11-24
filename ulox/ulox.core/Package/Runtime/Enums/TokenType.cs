namespace ULox
{
    public enum TokenType
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

        QUESTION,
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

        THROW,
        
        DATA,

        CLASS,
        THIS,
        STATIC,
        INIT,

        FREEZE,

        MIXIN,

        TEST,
        TESTCASE,

        CONTEXT_NAME_CLASS,
        CONTEXT_NAME_FUNC,
        CONTEXT_NAME_TESTCASE,
        CONTEXT_NAME_TESTSET,

        BUILD,
        
        REGISTER,
        INJECT,

        TYPEOF,

        LOCAL,
        PURE,

        MEETS,
        SIGNS,

        COUNT_OF,

        EXPECT,

        MATCH,

        FACTORY,
        FACTORYLINE,
    }
}
