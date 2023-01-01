using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class NativeMapInstance : InstanceInternal, INativeCollection
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeMapInstance(UserTypeInternal fromClass)
            : base(fromClass)
        {
        }

        public Dictionary<Value, Value> Map { get; } = new Dictionary<Value, Value>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(Value ind, Value val)
        {
            if (IsReadOnly)
                throw new UloxException($"Attempted to Set index '{ind}' to '{val}', but map is read only.");

            Map[ind] = val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value Get(Value ind)
        {
            return Map.TryGetValue(ind, out var value)
                ? value
                : throw new UloxException($"Map contains no key of '{ind}'.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value Count()
        {
            return Value.New(Map.Count);
        }
    }
}
