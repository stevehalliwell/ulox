namespace ULox
{
    public sealed class WhiteSpaceScannerTokenGenerator : IScannerTokenGenerator
    {
        public Token Consume(Scanner scanner) => scanner.SharedNoToken;

        public bool DoesMatchChar(char ch)
        {
            switch (ch)
            {
                case ' ':
                case '\r':
                case '\t':
                case '\n':
                return true;
            }

            return false;
        }
    }
}
