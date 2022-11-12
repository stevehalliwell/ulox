namespace ULox
{
    public sealed class SlashScannerTokenGenerator : PrefixedCharScannerCharMatchTokenGenerator
    {
        public SlashScannerTokenGenerator() : base('/')
        {
        }

        public override void Consume(IScanner scanner)
        {
            if (scanner.Match('/'))
            {
                scanner.ReadLine();
                scanner.Line++;
                scanner.CharacterNumber = 1;
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
    }
}
