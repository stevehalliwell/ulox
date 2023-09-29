using System.Collections;
using System.Collections.Generic;

namespace ULox
{
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
}
