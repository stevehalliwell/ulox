using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class NativeListInstance : InstanceInternal, INativeCollection
    {

        public NativeListInstance(UserTypeInternal fromClass)
            : base(fromClass)
        {
        }

        public FastList<Value> List { get; } = new FastList<Value>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(Value ind, Value val)
        {
            if (IsReadOnly)
                throw new UloxException($"Attempted to Set index '{ind}' to '{val}', but list is read only.");

            List[(int)ind.val.asDouble] = val;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value Get(Value ind)
        {
            return List[(int)ind.val.asDouble];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value Count()
        {
            return Value.New(List.Count);
        }
    }
}
