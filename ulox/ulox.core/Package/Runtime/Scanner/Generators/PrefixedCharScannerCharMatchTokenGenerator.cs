namespace ULox
{
    public abstract class PrefixedCharScannerCharMatchTokenGenerator : IScannerCharMatchTokenGenerator
    {
        protected PrefixedCharScannerCharMatchTokenGenerator(char matchingChar)
        {
            MatchingChar = matchingChar;
        }

        public char MatchingChar { get; private set; }

        public abstract void Consume(ScannerBase scanner);
    }
}
