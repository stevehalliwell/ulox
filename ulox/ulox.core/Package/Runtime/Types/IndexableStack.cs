using System.Collections.Generic;

namespace ULox
{
    //TODO: could prob be a fast list
    public sealed class IndexableStack<T> : List<T>
    {
        public void Push(T t) 
            => Add(t);

        public T Pop()
        {
            var res = this[Count - 1]; 
            RemoveAt(Count - 1); 
            return res;
        }

        public T Peek() => Peek(0);

        public T Peek(int down) => 
            (Count == 0) 
            ? default 
            : this[Count - 1 - down];
    }
}
