namespace ULox
{
    public enum OpCode : byte
    {
        NONE,

        PUSH_CONSTANT,
        PUSH_VALUE,

        POP,
        DUPLICATE,

        DEFINE_GLOBAL,
        FETCH_GLOBAL,
        ASSIGN_GLOBAL,
        GET_LOCAL,
        SET_LOCAL,
        GET_UPVALUE,
        SET_UPVALUE,
        CLOSE_UPVALUE,

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

        MULTI_VAR,

        THROW,

        VALIDATE,

        GET_PROPERTY,
        SET_PROPERTY,
        GET_FIELD,
        SET_FIELD,
        INVOKE,

        TEST,

        BUILD,

        NATIVE_TYPE,
        GET_INDEX,
        SET_INDEX,
        EXPAND_COPY_TO_STACK,

        TYPEOF,

        COUNT_OF,

        GOTO,
        GOTO_IF_FALSE,
        LABEL,

        ENUM_VALUE,
        READ_ONLY, //could collapse with FREEZE

        UPDATE,
    }

    public static class OpCodeUtil
    {
        public const int NumberOfOpCodes = (int)OpCode.UPDATE + 1;
    }
}
