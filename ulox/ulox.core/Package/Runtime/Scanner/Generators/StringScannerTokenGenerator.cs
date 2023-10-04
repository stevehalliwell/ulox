using System.Text;

namespace ULox
{
    public sealed class StringScannerTokenGenerator : IScannerTokenGenerator
    {
        private readonly StringBuilder workingSpaceStringBuilder = new StringBuilder();
        private bool _isInMiddleOfInterp;

        public bool DoesMatchChar(char ch)
            => ch == '"' || (ch == '}' && _isInMiddleOfInterp);

        public void Consume(Scanner scanner)
        {
            if (_isInMiddleOfInterp)
            {
                scanner.EmitTokenSingle(TokenType.CLOSE_PAREN);
                scanner.EmitTokenSingle(TokenType.PLUS);
            }

            _isInMiddleOfInterp = false;
            workingSpaceStringBuilder.Clear();
            var prevChar = scanner.CurrentChar;
            scanner.Advance();//skip leading " or {
            while (!scanner.IsAtEnd())
            {
                if (scanner.CurrentChar == '"'
                    && prevChar != '\\')
                {
                    EndString(scanner);
                    return;
                }
                else if (scanner.CurrentChar == '{'
                    && prevChar != '\\')
                {
                    EndString(scanner);
                    scanner.EmitTokenSingle(TokenType.PLUS);
                    scanner.EmitTokenSingle(TokenType.OPEN_PAREN);
                    _isInMiddleOfInterp = true;
                    return;
                }

                workingSpaceStringBuilder.Append(scanner.CurrentChar);

                prevChar = scanner.CurrentChar;
                scanner.Advance();
            }

            //we don't want this but when doing expression only mode the last char and the close of quote can be the same
            if (scanner.CurrentChar != '"')
                scanner.ThrowScannerException("Unterminated String");

            EndString(scanner);
        }

        private void EndString(Scanner scanner)
        {
            var str = System.Text.RegularExpressions.Regex.Unescape(workingSpaceStringBuilder.ToString());
            scanner.EmitToken(TokenType.STRING, str, str);
        }
    }
}
