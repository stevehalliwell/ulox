using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class NativeListInstance : InstanceInternal, INativeCollection
    {
        private readonly List<Value> _list = new List<Value>();

        public NativeListInstance(UserTypeInternal fromClass)
            : base(fromClass)
        {
        }

        public List<Value> List => _list;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(Value ind, Value val)
        {
            if (IsReadOnly)
                throw new UloxException($"Attempted to Set index '{ind}' to '{val}', but list is read only.");

            _list[(int)ind.val.asDouble] = val;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value Get(Value ind)
        {
            return _list[(int)ind.val.asDouble];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value Count()
        {
            return Value.New(_list.Count);
        }
    }
}
