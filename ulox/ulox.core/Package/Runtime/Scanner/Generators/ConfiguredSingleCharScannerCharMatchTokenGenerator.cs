namespace ULox
{
    public sealed class ConfiguredSingleCharScannerCharMatchTokenGenerator : PrefixedCharScannerCharMatchTokenGenerator
    {
        public ConfiguredSingleCharScannerCharMatchTokenGenerator(char matchingChar, TokenType tokenType)
            : base(matchingChar)
        {
            this.tokenType = tokenType;
        }

        private readonly TokenType tokenType;

        public override void Consume(IScanner scanner) 
            => scanner.AddTokenSingle(tokenType);
    }
}
