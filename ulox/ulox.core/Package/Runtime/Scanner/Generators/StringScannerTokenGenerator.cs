﻿using System.Text;

namespace ULox
{
    public sealed class StringScannerTokenGenerator : IScannerTokenGenerator
    {
        private readonly StringBuilder workingSpaceStringBuilder = new StringBuilder();

        public bool DoesMatchChar(char ch) => ch == '"';

        public void Consume(IScanner scanner)
        {
            workingSpaceStringBuilder.Clear();
            var prevChar = scanner.CurrentChar;
            scanner.Advance();//skip leading "
            while (!scanner.IsAtEnd())
            {
                if (scanner.CurrentChar == '"'
                    && prevChar != '\\')
                {
                    End(scanner);
                    return;
                }

                workingSpaceStringBuilder.Append(scanner.CurrentChar);

                prevChar = scanner.CurrentChar;
                scanner.Advance();
            }

            //we don't want this but when doing expression only mode the last char and the close of quote can be the same
            if (scanner.CurrentChar != '"')
                scanner.ThrowScannerException("Unterminated String");

            End(scanner);
        }

        private void End(IScanner scanner)
        {
            var str = System.Text.RegularExpressions.Regex.Unescape(workingSpaceStringBuilder.ToString());
            scanner.AddToken(TokenType.STRING, str, str);
        }
    }
}
