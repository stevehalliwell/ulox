using System.Collections.Generic;

namespace ULox
{
    public class IndexableStack<T> : List<T>
    {
        public IndexableStack() { }

        public void Push(T t) => Add(t);
        public T Pop() { var res = this[Count - 1]; RemoveAt(Count - 1); return res; }
        public T Peek() => Peek(0);
        public T Peek(int down)
        {
            if (Count == 0) return default;

            return this[Count - 1 - down];
        }
    }
}