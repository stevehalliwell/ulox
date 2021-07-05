namespace ULox
{
    public class CompoundCharScannerCharMatchTokenGenerator : PrefixedCharScannerCharMatchTokenGenerator
    {
        private TokenType regularType;
        private TokenType compoundType;

        public CompoundCharScannerCharMatchTokenGenerator(
            char matchingChar,
            TokenType regularType,
            TokenType compoundType) 
            : base(matchingChar)
        {
            this.regularType = regularType;
            this.compoundType = compoundType;
        }

        public override void Consume(ScannerBase scanner)
        {
            scanner.AddTokenSingle(scanner.Match('=') ? compoundType : regularType);
        }
    }
}
