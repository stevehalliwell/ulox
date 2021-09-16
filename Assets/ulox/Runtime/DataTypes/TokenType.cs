﻿namespace ULox
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

        //INCREMENT,
        //DECREMENT,

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

        CLASS,
        THIS,
        SUPER,
        STATIC,
        INIT,

        TEST,
        TESTCASE,

        CONTEXT_NAME_CLASS,
        CONTEXT_NAME_FUNC,
        CONTEXT_NAME_TEST,
        CONTEXT_NAME_TESTCASE,

        EOF,

        NONE,

        BUILD,
    }
}
