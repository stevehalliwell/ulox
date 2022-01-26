using System.Runtime.CompilerServices;

namespace ULox
{
    internal interface INativeCollection
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Value Get(Value ind);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Set(Value ind, Value val);
    }
}
