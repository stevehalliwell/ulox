using System.Text;

namespace ULox
{
    public class StringScannerTokenGenerator : IScannerTokenGenerator
    {
        private StringBuilder workingSpaceStringBuilder = new StringBuilder();
        private char _prevChar;

        public bool DoesMatchChar(char ch) => ch == '"';

        public void Consume(IScanner scanner)
        {
            workingSpaceStringBuilder.Clear();
            _prevChar = scanner.CurrentChar;
            scanner.Advance();//skip leading "
            while (!scanner.IsAtEnd())
            {
                if (scanner.CurrentChar == '\n') { scanner.Line++; scanner.CharacterNumber = 0; }

                if (scanner.CurrentChar == '"'
                    && _prevChar != '\\')
                {
                    End(scanner);
                    return;
                }

                workingSpaceStringBuilder.Append(scanner.CurrentChar);

                _prevChar = scanner.CurrentChar;
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
