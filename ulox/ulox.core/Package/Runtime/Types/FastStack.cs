//#define FASTSTACK_FORCE_CLEAR

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class FastStack<T> : IEnumerable<T>
    {
        public const int StartingSize = 32;
        public const int GrowFactor = 2;
        public const int StartingBack = -1;
        private T[] _array = new T[StartingSize];
        private int _back = StartingBack;

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _back + 1;
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _array[index] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T val)
        {
            if (_back >= _array.Length - 2)
                System.Array.Resize(ref _array, _array.Length * GrowFactor);

            _array[++_back] = val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop() => _array[_back--];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DiscardPop(int amount = 1)
        {
#if FASTSTACK_FORCE_CLEAR
            for (int i = 0; i < amount; i++)
                _array[_back - i] = default;
#endif//FASTSTACK_FORCE_CLEAR
            _back -= amount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => _back = StartingBack;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek(int down = 0) => _array[_back - down];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAt(int index, T t) => _array[index] = t;

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_array).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _array.GetEnumerator();
    }
}
