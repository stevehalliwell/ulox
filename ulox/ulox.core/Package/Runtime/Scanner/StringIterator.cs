using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class StringIterator
    {
        private readonly string _source;
        private int _index;

        public StringIterator(string source)
        {
            _source = source;
            _index = -1;
            Line = 1;
            CharacterNumber = 0;
        }

        public int Line { get; set; }
        public int CharacterNumber { get; set; }
        public char CurrentChar { get; private set; }
        public int CurrentIndex => _index;
        public List<int> LineLengths { get; } = new();

        public void FinishLineLengths()
        {
            LineLengths.Add(CharacterNumber);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int Peek() => SafeRead(_index + 1);
                
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int SafeRead(int index)
        {
            return index < _source.Length ? _source[index] : -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ReadLine()
        {
            while (Peek() != -1 && CurrentChar != '\n')
                Advance();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Advance()
        {
            _index++; 
            CurrentChar = (char)SafeRead(_index);
            if (CurrentChar == '\n' || CurrentChar == '\0')
            {
                LineLengths.Add(CharacterNumber);
                Line++;
                CharacterNumber = 0;
            }
            CharacterNumber++;
        }
    }
}
