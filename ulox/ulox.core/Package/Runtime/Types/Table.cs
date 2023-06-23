using System.Collections;
using System.Collections.Generic;

namespace ULox
{
    public sealed class Table : TableDict { }

    public class TableArray : IEnumerable<KeyValuePair<HashedString, Value>>
    {
        private readonly List<(HashedString hs, Value val)> _values = new List<(HashedString, Value)>();
        
        public IEnumerator<KeyValuePair<HashedString, Value>> GetEnumerator()
        {
            foreach (var pair in _values)
            {
                yield return new KeyValuePair<HashedString, Value>(pair.hs, pair.val);
            }
        }

        public bool Get(HashedString key, out Value ourContractMatchingMeth)
        {
            ourContractMatchingMeth = Value.Null();
            for (int i = 0; i < _values.Count; i++)
            {
                if (_values[i].hs.Hash == key.Hash)
                {
                    ourContractMatchingMeth = _values[i].val;
                    return true;
                }
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void AddOrSet(HashedString hashedString, Value val)
        {
            var index = FindIndex(hashedString);

            if(index == -1)
            {
                _values.Add((hashedString, val));
            }
            else
            {
                _values[index] = (hashedString, val);
            }
        }

        public void Set(HashedString keyHash, Value val)
        {
            var index = FindIndex(keyHash);

            if (index == -1)
                throw new UloxException($"Attempted to Create a new field with hash'{keyHash}' via SetField on a frozen object.");

            _values[index] = (_values[index].hs, val);
        }

        public bool Remove(HashedString key)
        {
            var index = FindIndex(key);
            if (index == -1)
                return false;

            _values.RemoveAt(index);
            return true;
        }

        public void CopyFrom(TableArray fields)
        {
            for (int i = 0; i < fields._values.Count; i++)
            {
                AddOrSet(fields._values[i].hs, fields._values[i].val);
            }
        }

        public bool Contains(HashedString paramName)
        {
            return FindIndex(paramName) != -1;
        }

        public int Count => _values.Count;

        private int FindIndex(HashedString key)
        {
            for (int i = 0; i < _values.Count; i++)
            {
                if (_values[i].hs.Hash == key.Hash)
                    return i;
            }
            return -1;
        }
    }

    public class TableDict : IEnumerable<KeyValuePair<HashedString, Value>>
    {
        private readonly Dictionary<HashedString, Value> _values = new Dictionary<HashedString, Value>(TableKeyComparer);
        private static readonly HashedStringComparer TableKeyComparer = new HashedStringComparer();

        public IEnumerator<KeyValuePair<HashedString, Value>> GetEnumerator()
        {
            foreach (var pair in _values)
            {
                yield return pair;
            }
        }

        public bool Get(HashedString key, out Value ourContractMatchingMeth) => _values.TryGetValue(key, out ourContractMatchingMeth);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void AddOrSet(HashedString hashedString, Value val)
        {
            _values[hashedString] = val;
        }

        public void Set(HashedString key, Value val)
        {
            if (!_values.ContainsKey(key))
                throw new UloxException($"Attempted to Create a new field '{key}' via SetField on a frozen object.");
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
