using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public enum FactoryOpType : byte
    {
        Set,
        Get
    }

    public sealed class Factory
    {
        private readonly Dictionary<Value, Value> _lines = new Dictionary<Value, Value>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLine(IVm vm, Value key, Value creator)
        {
            if (creator == null
                || creator.IsNull())
                vm.ThrowRuntimeException($"Factory line of key '{key}' attempted to be set to null. Not allowed.");

            _lines[key] = creator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value GetLine(IVm vm, Value key)
        {
            if (_lines.TryGetValue(key, out var value))
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
