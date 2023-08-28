using System.Collections;
using System.Collections.Generic;

namespace ULox
{
    public sealed class FastList<T> : IEnumerable<T>
    {
        public const int StartingSize = 32;
        public const int GrowFactor = 2;
        private T[] _array = new T[StartingSize];
        private int _back = -1;

        public FastList()
        { }

        public int Count => _back + 1;

        public int Capacity => _array.Length;


        public T this[int index]
        {
            get => _array[index];
            set => _array[index] = value;
        }

        public void Add(T val)
        {
            if (_back >= _array.Length - 2)
                System.Array.Resize(ref _array, _array.Length * GrowFactor);
            _array[++_back] = val;
        }

        public void RemoveAt(int index)
        {
            var count = Count;
            for (int i = index; i < count; i++)
                _array[i] = _array[i + 1];
            _back--;
        }

        public void Clear()
        {
            _back = -1;
        }

        public bool Remove(T item)
        {
            var count = Count;
            for (int i = 0; i < count; i++)
            {
                if (_array[i].Equals(item))
                {
                    RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _back+1; i++)
                yield return _array[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < _back+1; i++)
                yield return _array[i];
        }

        public void EnsureCapacity(int capacity)
        {
            if (_array.Length < capacity)
                System.Array.Resize(ref _array, capacity);
        }
    }
}