using System;
using System.IO;

namespace ULox
{
    public sealed class StringIterator
    {
        private readonly StringReader _stringReader;

        public StringIterator(string source)
        {
            _stringReader = new StringReader(source);
        }

        public int Line { get; set; }
        public int CharacterNumber { get; set; }
        public char CurrentChar { get; private set; }

        internal int Peek() => _stringReader.Peek();

        internal char Read() => (char)_stringReader.Read();

        internal void ReadLine()
        {
            _stringReader.ReadLine();
            Line++;
            CharacterNumber = 1;
        }

        internal void Advance()
        {
            CurrentChar = (Char)Read();
            if (CurrentChar == '\n')
            {
                Line++;
                CharacterNumber = 0;
            }
            CharacterNumber++;
        }
    }
}
