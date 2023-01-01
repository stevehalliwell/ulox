namespace ULox
{
    public sealed class SlashScannerTokenGenerator : IScannerTokenGenerator
    {
        public Token Consume(Scanner scanner)
        {
            if (scanner.Match('/'))
            {
                scanner.ReadLine();
                return Scanner.SharedNoToken;
            }
            else if (scanner.Match('*'))
            {
                return ConsumeBlockComment(scanner);
            }
            else
            {
                return scanner.EmitTokenSingle(scanner.Match('=') ? TokenType.SLASH_EQUAL : TokenType.SLASH);
            }
        }

        private static Token ConsumeBlockComment(Scanner scanner)
        {
            while (!scanner.IsAtEnd())
            {
                if (scanner.Match('*') && scanner.Match('/'))
                {
                    break;
                }
                else
                {
                    scanner.Advance();
                }
            }
            return Scanner.SharedNoToken;
        }
        
        public bool DoesMatchChar(char ch)
        {
            return ch == '/';
        }
    }
}
