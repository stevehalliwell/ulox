namespace ULox
{
    public enum ValueType
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
