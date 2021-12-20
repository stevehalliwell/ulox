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
        CombinedClosures,
        Upvalue,
        Class,
        Instance,
        BoundMethod,
        Object,
    }
}
