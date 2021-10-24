namespace ULox
{
    public enum ValueType : byte
    {
        Null,
        Void,
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
