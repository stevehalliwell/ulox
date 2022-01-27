using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public class NativeListInstance : InstanceInternal, INativeCollection
    {
        private readonly List<Value> _list = new List<Value>();

        public NativeListInstance(ClassInternal fromClass)
            : base(fromClass)
        {
        }

        public List<Value> List => _list;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(Value ind, Value val)
        {
            _list[(int)ind.val.asDouble] = val;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value Get(Value ind)
        {
            return _list[(int)ind.val.asDouble];
        }
    }
}
