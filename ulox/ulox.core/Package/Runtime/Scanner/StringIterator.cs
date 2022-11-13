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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int Peek() => SafeRead(_index + 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal char Read()
        {
            Advance();
            return CurrentChar;
        }
        
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
            
            Line++;
            CharacterNumber = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Advance()
        {
            _index++; 
            CurrentChar = (char)SafeRead(_index);
            if (CurrentChar == '\n')
            {
                Line++;
                CharacterNumber = 0;
            }
            CharacterNumber++;
        }
    }
}
