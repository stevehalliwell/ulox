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

        JUMP_IF_FALSE,
        JUMP,
        LOOP,   //this is just jump but negative

        NOT,
        EQUAL,
        LESS,
        GREATER,

        NEGATE,
        ADD,
        SUBTRACT,
        MULTIPLY,
        DIVIDE,
        MODULUS,

        CALL,
        CLOSURE,

        NATIVE_CALL,

        RETURN,
        YIELD,

        THROW,

        VALIDATE,

        CLASS,
        GET_PROPERTY,
        SET_PROPERTY,
        METHOD,
        FIELD,
        INVOKE,

        FREEZE,

        MIXIN,

        TEST,

        BUILD,
        
        REGISTER,
        INJECT,

        NATIVE_TYPE,
        GET_INDEX,
        SET_INDEX,

        TYPEOF,

        LOCAL,

        MEETS,
        SIGNS,
    }
}
