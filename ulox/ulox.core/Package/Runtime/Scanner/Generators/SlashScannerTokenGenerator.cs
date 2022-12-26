namespace ULox
{
    public sealed class SlashScannerTokenGenerator : IScannerTokenGenerator
    {
        public void Consume(IScanner scanner)
        {
            if (scanner.Match('/'))
            {
                scanner.ReadLine();
            }
            else if (scanner.Match('*'))
            {
                ConsumeBlockComment(scanner);
            }
            else
            {
                scanner.AddTokenSingle(scanner.Match('=') ? TokenType.SLASH_EQUAL : TokenType.SLASH);
            }
        }

        private void ConsumeBlockComment(IScanner scanner)
        {
            while (!scanner.IsAtEnd())
            {
                if (scanner.Match('*') && scanner.Match('/'))
                {
                    return;
                }
                else
                {
                    scanner.Advance();
                }
            }
        }
        
        public bool DoesMatchChar(char ch)
        {
            return ch == '/';
        }
    }
}
