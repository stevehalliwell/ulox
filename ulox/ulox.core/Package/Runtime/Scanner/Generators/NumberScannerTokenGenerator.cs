﻿using System.Text;

namespace ULox
{
    public sealed class NumberScannerTokenGenerator : IScannerTokenGenerator
    {
        private readonly StringBuilder workingSpaceStringBuilder = new StringBuilder();

        public static bool IsDigit(int ch) => ch >= '0' && ch <= '9';

        public bool DoesMatchChar(char ch) => IsDigit(ch);

        public void Consume(IScanner scanner)
        {
            workingSpaceStringBuilder.Clear();

            workingSpaceStringBuilder.Append(scanner.CurrentChar);

            while (IsDigit(scanner.Peek()))
            {
                scanner.Advance();
                workingSpaceStringBuilder.Append(scanner.CurrentChar);
            }

            if (scanner.Peek() == '.')
            {
                scanner.Advance();
                workingSpaceStringBuilder.Append(scanner.CurrentChar);
                while (IsDigit(scanner.Peek()))
                {
                    scanner.Advance();
                    workingSpaceStringBuilder.Append(scanner.CurrentChar);
                }
            }

            var numStr = workingSpaceStringBuilder.ToString();

            scanner.AddToken(
                TokenType.NUMBER,
                numStr,
                double.Parse(numStr));
        }
    }
}
