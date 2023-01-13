using System.Runtime.CompilerServices;

namespace ULox
{
    public interface INativeCollection
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Value Get(Value ind);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Set(Value ind, Value val);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Value Count();
    }
}
