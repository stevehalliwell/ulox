namespace ULox
{
    public class SlashScannerTokenGenerator : PrefixedCharScannerCharMatchTokenGenerator
    {
        public SlashScannerTokenGenerator() : base('/')
        {
        }

        public override void Consume(ScannerBase scanner)
        {
            if (scanner.Match('/'))
            {
                scanner.ReadLine();
                scanner.Line++;
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

        private void ConsumeBlockComment(ScannerBase scanner)
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
