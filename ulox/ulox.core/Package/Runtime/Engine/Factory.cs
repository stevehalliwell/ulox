using System.Collections.Generic;

namespace ULox
{
    public class Factory
    {
        private Dictionary<Value, Value> _lines = new Dictionary<Value, Value>();

        public void SetLine(Value key, Value creator)
        {
            _lines[key] = creator;
        }

        public Value GetLine(Value key)
        {
            return _lines.TryGetValue(key, out var value)
                ? value
                : throw new VMException($"Factory contains no line of key '{key}'.");
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
