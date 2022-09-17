using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Factory
    {
        private readonly Dictionary<Value, Value> _lines = new Dictionary<Value, Value>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLine(Value key, Value creator)
        {
            _lines[key] = creator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value GetLine(IVm vm, Value key)
        {
            if(_lines.TryGetValue(key, out var value))
            {
                return value;
            }

            vm.ThrowRuntimeException($"Factory contains no line of key '{key}'");
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Factory ShallowCopy()
        {
            var ret = new Factory();
            foreach (var pair in _lines)
            {
                ret._lines.Add(pair.Key, pair.Value);
            }
            return ret;
        }
    }
}
