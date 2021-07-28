namespace ULox
{
    public enum OpCode : byte
    {
        NONE,
     
        CONSTANT,
        NULL,
        PUSH_BOOL,
        PUSH_BYTE,

        POP, 
        SWAP,

        DEFINE_GLOBAL,
        FETCH_GLOBAL,
        ASSIGN_GLOBAL,
        GET_LOCAL,
        SET_LOCAL,
        GET_UPVALUE,
        SET_UPVALUE,
        CLOSE_UPVALUE,

        GET_PROPERTY,
        SET_PROPERTY,

        JUMP_IF_FALSE,
        JUMP,
        LOOP,   //this is just jump but negative

        NOT,
        EQUAL,
        GREATER,
        LESS,

        NEGATE,
        ADD,
        SUBTRACT,
        MULTIPLY,
        DIVIDE, 
        MODULUS,

        CALL,
        CLOSURE,

        RETURN,
        YIELD,

        THROW,

        CLASS,
        METHOD,
        INVOKE,
        INHERIT,
        GET_SUPER,
        SUPER_INVOKE,

        TEST_START,
        TEST_END,
        TEST_CHAIN_START,
    }
}