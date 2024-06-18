namespace ULox
{
    public enum ValueType : byte
    {
        Null,
        Double,
        Bool,
        String,
        Chunk,
        NativeFunction,
        Closure,
        Upvalue,
        UserType,
        Instance,
        BoundMethod,
        Object,
    }
}
