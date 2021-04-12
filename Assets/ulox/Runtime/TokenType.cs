namespace ULox
{
    public enum TokenType
    {
        OPEN_PAREN,
        CLOSE_PAREN,
        OPEN_BRACE,
        CLOSE_BRACE,
        COMMA,
        DOT,
        END_STATEMENT,
        
        INCREMENT,
        DECREMENT,

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
        INT,
        FLOAT,

        FUNCTION,
        CLASS,
        THIS,
        SUPER,
        STATIC,

        AND,
        OR,
        IF,
        ELSE,
        WHILE,
        FOR,

        LOOP,
        RETURN,

        BREAK,
        CONTINUE,
        TRUE,
        FALSE,
        NULL,

        THROW,

        TEST,
        TESTCASE,

        EOF,

        NONE,
    }
}
