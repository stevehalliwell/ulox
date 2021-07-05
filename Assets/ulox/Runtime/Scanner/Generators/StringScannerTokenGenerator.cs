using System.Text;

namespace ULox
{
    public class StringScannerTokenGenerator : IScannerTokenGenerator
    {
        private StringBuilder workingSpaceStringBuilder = new StringBuilder();

        public bool DoesMatchChar(ScannerBase scanner) => scanner.CurrentChar == '"';

        public void Consume(ScannerBase scanner)
        {
            workingSpaceStringBuilder.Clear();
            scanner.Advance();//skip leading "
            while (!scanner.IsAtEnd())
            {
                if (scanner.CurrentChar == '\n') { scanner.Line++; scanner.CharacterNumber = 0; }

                if (scanner.CurrentChar == '"')
                {
                    var str = System.Text.RegularExpressions.Regex.Unescape(workingSpaceStringBuilder.ToString());
                    scanner.AddToken(TokenType.STRING, str, str);
                    return;
                }

                workingSpaceStringBuilder.Append(scanner.CurrentChar);

                scanner.Advance();
            }

            //we don't want this but when doing expression only mode the last char and the close of quote can be the same
            if (scanner.CurrentChar == '"')
            {
                var str = System.Text.RegularExpressions.Regex.Unescape(workingSpaceStringBuilder.ToString());
                scanner.AddToken(TokenType.STRING, str, str);
                return;
            }

            throw new ScannerException(TokenType.IDENTIFIER, scanner.Line, scanner.CharacterNumber, "Unterminated String");
        }
    }
}
