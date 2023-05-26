using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public sealed class Table : IEnumerable<KeyValuePair<int, Value>>//Dictionary<HashedString, Value>
    {
        private readonly Dictionary<int, HashedString> _strings = new Dictionary<int, HashedString>();
        private readonly Dictionary<int, Value> _values = new Dictionary<int, Value>();
        //public static readonly HashedStringComparer TableKeyComparer = new HashedStringComparer();

        //public Table() : base(TableKeyComparer)
        //{
        //}

        //public IReadOnlyDictionary<HashedString, Value> AsReadOnly => this;
        public IEnumerator<KeyValuePair<int, Value>> GetEnumerator()
        {
            foreach (var pair in _values)
            {
                yield return pair;
            }
        }

        public bool Get(int key, out Value ourContractMatchingMeth) => _values.TryGetValue(key, out ourContractMatchingMeth);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void AddOrSet(HashedString hashedString, Value val)
        {
            _strings[hashedString.Hash] = hashedString;
            _values[hashedString.Hash] = val;
        }

        public void Set(int keyHash, Value val)
        {
            //temp
            if (!_strings.ContainsKey(keyHash)) 
                throw new UloxException($"Attempted to Create a new field with hash'{keyHash}' via SetField on a frozen object.");
            _values[keyHash] = val;
        }

        public bool Remove(int key)
        {
            _strings.Remove(key);
            return _values.Remove(key);
        }

        public void CopyFrom(Table fields)
        {
            foreach (var pair in fields)
            {
                _strings[pair.Key] = pair.Value.val.asString;
                _values[pair.Key] = pair.Value;
            }
        }

        public bool Contains(HashedString paramName)
        {
            return _strings.ContainsKey(paramName.Hash);
        }

        public HashedString GetStringFromKey(int hash) => _strings[hash];

        public int Count => _values.Count;

        public int[] Keys() { return _values.Keys.ToArray(); }
    }
}
