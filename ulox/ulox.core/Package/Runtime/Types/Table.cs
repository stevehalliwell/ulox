using System.Collections;
using System.Collections.Generic;

namespace ULox
{
    public sealed class Table : TableDict { }

    public class TableDict : IEnumerable<KeyValuePair<HashedString, Value>>
    {
        private readonly Dictionary<HashedString, Value> _values = new(TableKeyComparer);
        private static readonly HashedStringComparer TableKeyComparer = new();

        public IEnumerator<KeyValuePair<HashedString, Value>> GetEnumerator()
        {
            foreach (var pair in _values)
            {
                yield return pair;
            }
        }

        public bool Get(HashedString key, out Value ourContractMatchingMeth) 
            => _values.TryGetValue(key, out ourContractMatchingMeth);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void AddOrSet(HashedString hashedString, Value val)
        {
            _values[hashedString] = val;
        }

        public void Set(HashedString key, Value val)
        {
            if (!_values.ContainsKey(key))
                throw new UloxException($"Attempted to create a new entry '{key}' via Set.");
            _values[key] = val;
        }

        public bool Remove(HashedString key)
        {
            return _values.Remove(key);
        }

        public void CopyFrom(TableDict fields)
        {
            foreach (var pair in fields)
            {
                AddOrSet(pair.Key, pair.Value);
            }
        }

        public bool Contains(HashedString paramName)
        {
            return _values.ContainsKey(paramName);
        }

        public int Count => _values.Count;
    }
}
