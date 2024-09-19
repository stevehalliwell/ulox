using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class NumberScannerTokenGenerator : IScannerTokenGenerator
    {
        private int _startingIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDigit(int ch) => ch >= '0' && ch <= '9';

        public bool DoesMatchChar(char ch) => IsDigit(ch);

        public void Consume(Scanner scanner)
        {
            _startingIndex = scanner.CurrentIndex;

            while (IsDigit(scanner.Peek()))
            {
                scanner.Advance();
            }

            if (scanner.Peek() == '.')
            {
                scanner.Advance();
                while (IsDigit(scanner.Peek()))
                {
                    scanner.Advance();
                }
            }

            var numStr = scanner.SubStrFrom(_startingIndex);

            scanner.EmitToken(
                TokenType.NUMBER,
                double.Parse(numStr));
        }
    }
}
