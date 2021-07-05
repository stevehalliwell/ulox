namespace ULox
{
    public class ConfiguredSingleCharScannerCharMatchTokenGenerator : PrefixedCharScannerCharMatchTokenGenerator
    {
        public ConfiguredSingleCharScannerCharMatchTokenGenerator(char matchingChar, TokenType tokenType)
            :base(matchingChar)
        {
            this.tokenType = tokenType;
        }

        private readonly TokenType tokenType;
        public override void Consume(ScannerBase scanner) => ProduceToken(scanner);

        private void ProduceToken(ScannerBase scanner) => scanner.AddTokenSingle(tokenType);
    }
}
