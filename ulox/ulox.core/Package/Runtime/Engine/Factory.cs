using System.Collections.Generic;

namespace ULox
{
    public class Factory
    {
        private readonly Dictionary<Value, Value> _lines = new Dictionary<Value, Value>();

        public void SetLine(Value key, Value creator)
        {
            _lines[key] = creator;
        }

        public Value GetLine(IVm vm, Value key)
        {
            if(_lines.TryGetValue(key, out var value))
            {
                return value;
            }

            vm.ThrowRuntimeException($"Factory contains no line of key '{key}'");
            return default;
        }

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
