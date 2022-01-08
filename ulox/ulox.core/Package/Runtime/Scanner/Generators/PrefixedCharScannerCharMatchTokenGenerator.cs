namespace ULox
{
    public abstract class PrefixedCharScannerCharMatchTokenGenerator : IScannerTokenGenerator
    {
        protected PrefixedCharScannerCharMatchTokenGenerator(char matchingChar)
        {
            _matchingChar = matchingChar;
        }

        private readonly char _matchingChar;

        public abstract void Consume(IScanner scanner);

        public bool DoesMatchChar(char ch)
        {
            return ch == _matchingChar;
        }
    }
}
