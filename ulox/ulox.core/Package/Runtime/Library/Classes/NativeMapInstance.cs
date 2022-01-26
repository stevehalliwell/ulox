using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public class NativeMapInstance : InstanceInternal, INativeCollection
    {
        public NativeMapInstance()
            : base(NativeMapClass.SharedClassInstance)
        {
        }

        private Dictionary<Value, Value> _map = new Dictionary<Value, Value>();

        public Dictionary<Value, Value> Map => _map;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(Value ind, Value val)
        {
            _map[ind] = val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value Get(Value ind)
        {
            return _map[ind];
        }
    }
}
