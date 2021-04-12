using System.Runtime.CompilerServices;

namespace ULox
{
    public class FastStack<T>
    {
        private const int StartingSize = 16;
        private T[] _array = new T[StartingSize];
        private int _back = -1;

        public int Count => _back + 1;
        public T this[int index] { get => _array[index]; set => _array[index] = value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T val)
        {
            if (_back >= _array.Length-1)
                System.Array.Resize(ref _array, _array.Length * 2);

            _array[++_back] = val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            return _array[_back--];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DiscardPop(int amount = 1)
        {
            _back -= amount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek(int down = 0)
        {
            return _array[_back-down];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAt(int index, T t)
        {
            _array[index] = t;
        }
    }
}