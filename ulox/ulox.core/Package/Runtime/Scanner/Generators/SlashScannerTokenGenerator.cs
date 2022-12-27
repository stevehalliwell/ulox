namespace ULox
{
    public sealed class SlashScannerTokenGenerator : IScannerTokenGenerator
    {
        public Token Consume(Scanner scanner)
        {
            if (scanner.Match('/'))
            {
                scanner.ReadLine();
                return scanner.SharedNoToken;
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

        private Token ConsumeBlockComment(Scanner scanner)
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
            return scanner.SharedNoToken;
        }
        
        public bool DoesMatchChar(char ch)
        {
            return ch == '/';
        }
    }
}
