using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class StringIterator
    {
        private readonly string _source;
        private int _index;
        private int _previousRunningCount;

        public StringIterator(string source)
        {
            _source = source;
            _index = -1;
        }

        public char CurrentChar { get; private set; }
        public int CurrentIndex => _index;
        public List<int> LineLengths { get; } = new();

        public void FinishLineLengths()
        {
            LineLengths.Add(_index - _previousRunningCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int Peek() => SafeRead(_index + 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int SafeRead(int index)
        {
            return index < _source.Length ? _source[index] : -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Advance()
        {
            _index++;
            CurrentChar = (char)SafeRead(_index);
            if (CurrentChar == '\n' || CurrentChar == '\0')
            {
                LineLengths.Add(_index - _previousRunningCount);
                _previousRunningCount = _index;
            }
        }

        public (int line, int characterNumber) GetLineAndCharacterNumber()
        {
            return (LineLengths.Count + 1, _index - _previousRunningCount + 1);
        }
    }
}
