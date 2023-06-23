﻿namespace ULox
{
    public enum OpCode : byte
    {
        NONE,

        PUSH_CONSTANT,
        PUSH_VALUE,

        POP,
        SWAP,
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

        TYPE,
        GET_PROPERTY,
        SET_PROPERTY,
        METHOD,
        FIELD,
        INVOKE,

        FREEZE,

        MIXIN,

        TEST,

        BUILD,

        NATIVE_TYPE,
        GET_INDEX,
        SET_INDEX,
        EXPAND_COPY_TO_STACK,

        TYPEOF,

        MEETS,
        SIGNS,

        COUNT_OF,

        EXPECT,

        GOTO,
        GOTO_IF_FALSE,
        LABEL,

        ENUM_VALUE,
        READ_ONLY,

        UPDATE,
    }
}
