using System;
using System.Collections;
using System.Collections.Generic;

namespace ULox
{
    public class IndexedTable
    {
        private Dictionary<string, int> lookup = new Dictionary<string, int>();
        private List<Value> values = new List<Value>();

        public Value this[string str]
        {
            get => values[lookup[str]];
            set
            {
                if(!lookup.ContainsKey(str))
                {
                    Add(str, value);
                    return;
                }

                values[lookup[str]] = value;
            }
        }

        public void Add(string str, Value value)
        {
            lookup[str] = values.Count;
            values.Add(value);
            return;
        }

        public Value this[int index]
        {
            get => values[index];
            set => values[index] = value;
        }

        public int Count => lookup.Count;

        public int FindIndex(string str)
        {
            if (lookup.TryGetValue(str, out int i))
                return i;
            return -1;
        }

        public Dictionary<string, int>.Enumerator GetEnumerator()
        {
            return lookup.GetEnumerator();
        }
    }
}
