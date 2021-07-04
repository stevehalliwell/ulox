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

        DEFINE_GLOBAL,
        FETCH_GLOBAL_UNCACHED,
        //FETCH_GLOBAL_CACHED,
        ASSIGN_GLOBAL_UNCACHED,
        //ASSIGN_GLOBAL_CACHED,
        GET_LOCAL,
        SET_LOCAL,
        GET_UPVALUE,
        SET_UPVALUE,
        CLOSE_UPVALUE,

        GET_PROPERTY_UNCACHED,
        //GET_PROPERTY_CACHED,
        SET_PROPERTY_UNCACHED,
        //SET_PROPERTY_CACHED,

        JUMP_IF_FALSE,
        JUMP,
        LOOP,   //this is just jump but negative

        NOT,
        EQUAL,
        GREATER,
        LESS,

        CALL,
        CLOSURE,

        RETURN,
        YIELD,

        CLASS,
        METHOD,
        INVOKE,
        INHERIT,
        GET_SUPER,
        SUPER_INVOKE,
        INIT_CHAIN_START,
        TEST_CHAIN_START,

        NEGATE,
        ADD,
        SUBTRACT,
        MULTIPLY,
        DIVIDE,

        TEST_START,
        TEST_END,

        THROW,
    }
}